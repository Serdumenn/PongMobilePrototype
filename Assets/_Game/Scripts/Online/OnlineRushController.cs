using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class OnlineRushController : MonoBehaviour
{
    public enum RushState
    {
        Idle,
        Waiting,
        Countdown,
        Playing,
        Finished,
        Result
    }

    public readonly struct Entrant
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Look;

        public Entrant(string id, string name, string look)
        {
            Id = id;
            Name = name;
            Look = look;
        }
    }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Solo;
    [SerializeField] private Paddle MainPaddle;
    [SerializeField] private RecordsService Records;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private SkinApplier Skins;

    [Header("Timing")]
    [SerializeField] private int CountdownFrom = 3;
    [SerializeField] private float CountdownStep = 0.6f;
    [SerializeField] private float ScoreInterval = 0.25f;
    [SerializeField] private float FinalWait = 8f;

    [Header("Attacks")]
    [SerializeField] private float WarningSeconds = 1.5f;
    [SerializeField] private float AttackSeconds = 5f;
    [SerializeField] private float MiniPaddleScale = 0.6f;
    [SerializeField] private float FastBallBoost = 1.35f;

    public event Action<RushState> StateChanged;
    public event Action<int> CountdownTick;
    public event Action ScoresChanged;
    public event Action<RushAttack, string> AttackIncoming;
    public event Action<RushAttack, bool> AttackActive;
    public event Action<RushAttack, string> AttackSent;
    public event Action<string> AttackBlocked;
    public event Action<string> MyAttackBlocked;
    public event Action<string, byte> ReactionReceived;
    public event Action RematchChanged;

    public RushState State { get; private set; } = RushState.Idle;
    public GameModeDefinition Mode { get; private set; }
    public RushBattleRules Rules { get; private set; }
    public bool IsHost { get; private set; }
    public bool IsActive => State != RushState.Idle;
    public RushAttack? Incoming { get; private set; }
    public RushAttack? ActiveAttack { get; private set; }
    public bool WantsRematch => Rules?.Me != null && Rules.Me.WantsRematch;
    public int PingMs => link != null ? link.PingMs : -1;
    public float MatchSeconds { get; private set; }

    private readonly List<Coroutine> running = new List<Coroutine>();
    private readonly List<Entrant> entrants = new List<Entrant>();
    private IMatchLink link;
    private string myId;
    private string myLook;
    private MatchRandom random = new MatchRandom(1);
    private ushort nextSeq;
    private int lastIncomingSeq = -1;
    private float nextScoreAt;
    private bool blockedThisAttack;
    private byte incomingFrom;
    private bool recorded;

    private void Awake()
    {
        if (Solo == null) Solo = FindFirstObjectByType<SoloGameManager>();
        if (MainPaddle == null) MainPaddle = FindFirstObjectByType<Paddle>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Skins == null) Skins = FindFirstObjectByType<SkinApplier>();
    }

    private void OnEnable()
    {
        if (Solo == null) return;
        Solo.HitScored += OnHit;
        Solo.StateChanged += OnSoloState;
    }

    private void OnDisable()
    {
        if (Solo != null)
        {
            Solo.HitScored -= OnHit;
            Solo.StateChanged -= OnSoloState;
        }
        Detach();
    }

    private void Update()
    {
        if (State != RushState.Playing || Solo == null || Solo.State != SoloGameManager.GameState.Playing) return;

        MatchSeconds += Time.unscaledDeltaTime;
        if (Time.unscaledTime < nextScoreAt) return;
        nextScoreAt = Time.unscaledTime + ScoreInterval;

        int score = Solo.ScoreManager != null ? Solo.ScoreManager.Score : 0;
        int left = Solo.Rules != null ? Mathf.CeilToInt(Solo.Rules.TimeRemaining) : 0;
        Rules.SetScore(Rules.MySlot, score, left);
        Send(MatchMessage.RushScore(Rules.MySlot, score, (byte)Mathf.Clamp(left, 0, 255)));
        ScoresChanged?.Invoke();
    }

    public void Open(IMatchLink matchLink, GameModeDefinition mode, string playerId, string look, IReadOnlyList<Entrant> lobby)
    {
        if (matchLink == null || mode == null || Solo == null || Solo.State != SoloGameManager.GameState.Menu) return;

        Detach();
        link = matchLink;
        link.Received += OnReceived;

        Mode = mode;
        IsHost = link.IsHost;
        myId = playerId;
        myLook = look;
        entrants.Clear();
        if (lobby != null) entrants.AddRange(lobby);
        Rules = null;
        SetState(RushState.Waiting);
    }

    public void UpdateEntrants(IReadOnlyList<Entrant> lobby)
    {
        if (lobby == null) return;

        foreach (var entrant in lobby)
        {
            bool known = false;
            foreach (var e in entrants) if (e.Id == entrant.Id) known = true;
            if (!known) entrants.Add(entrant);
        }

        if (Rules == null || State == RushState.Waiting) return;

        bool changed = false;
        foreach (var player in Rules.Players)
        {
            if (player.Left || player.Id == myId) continue;
            bool present = false;
            foreach (var entrant in lobby) if (entrant.Id == player.Id) present = true;
            if (present) continue;

            Rules.MarkLeft(player.Slot);
            changed = true;
        }

        if (!changed) return;
        ScoresChanged?.Invoke();
        RematchChanged?.Invoke();
        TryFinish();
    }

    public void HostStart()
    {
        if (!IsHost || link == null || (State != RushState.Waiting && State != RushState.Result)) return;

        var ids = new List<string>();
        foreach (var entrant in entrants) ids.Add(entrant.Id);
        if (Rules != null && State == RushState.Result)
        {
            ids.Clear();
            foreach (var player in Rules.Players) if (!player.Left) ids.Add(player.Id);
        }

        string roster = RushBattleRules.BuildRoster(ids);
        if (RushBattleRules.ParseRoster(roster).Length < 2) return;

        int seed = unchecked((int)DateTime.UtcNow.Ticks);
        Send(MatchMessage.RushStart(seed, roster));
        Begin(seed, roster);
    }

    public void RequestRematch()
    {
        if (State != RushState.Result || Rules?.Me == null || Rules.Me.WantsRematch) return;

        Rules.Me.WantsRematch = true;
        Send(MatchMessage.RematchFrom(Rules.MySlot));
        RematchChanged?.Invoke();
        if (IsHost && Rules.EveryoneWantsRematch) HostStart();
    }

    public void SendReaction(byte reaction)
    {
        if (Rules == null) return;
        Send(MatchMessage.ReactionFrom(Rules.MySlot, reaction));
    }

    public void Exit()
    {
        if (State == RushState.Idle) return;

        StopRunning();
        ClearEffects();
        Detach();
        Time.timeScale = 1f;

        Rules = null;
        Mode = null;
        Incoming = null;
        SetState(RushState.Idle);

        if (Solo != null) Solo.ReturnToMenu();
        if (Skins != null) Skins.Refresh();
    }

    public string NameOf(byte slot)
    {
        var player = Rules?.Find(slot);
        return player != null ? player.Name : Loc.T("Someone");
    }

    private void Begin(int seed, string roster)
    {
        StopRunning();
        ClearEffects();

        var ids = RushBattleRules.ParseRoster(roster);
        var players = new List<RushBattlePlayer>();
        byte mySlot = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            var entrant = FindEntrant(ids[i]);
            players.Add(new RushBattlePlayer { Slot = (byte)i, Id = ids[i], Name = entrant.Name, Look = entrant.Look });
            if (ids[i] == myId) mySlot = (byte)i;
        }

        Rules = new RushBattleRules(players, mySlot);
        random = new MatchRandom(unchecked(seed + mySlot * 7919));
        nextSeq = 0;
        lastIncomingSeq = -1;
        Incoming = null;
        recorded = false;
        MatchSeconds = 0f;

        if (Solo.State != SoloGameManager.GameState.Menu) Solo.ReturnToMenu();
        Wear(myLook);

        ScoresChanged?.Invoke();
        RematchChanged?.Invoke();
        SetState(RushState.Countdown);
        Run(Countdown(seed));
    }

    private Entrant FindEntrant(string id)
    {
        foreach (var entrant in entrants)
            if (entrant.Id == id) return entrant;
        return new Entrant(id, PlayerNames.ForId(id), string.Empty);
    }

    private IEnumerator Countdown(int seed)
    {
        for (int n = CountdownFrom; n >= 1; n--)
        {
            CountdownTick?.Invoke(n);
            yield return new WaitForSecondsRealtime(CountdownStep);
        }

        CountdownTick?.Invoke(0);
        nextScoreAt = 0f;
        SetState(RushState.Playing);
        Solo.StartBattleRun(Mode, seed);
    }

    private void OnHit(HitResult hit)
    {
        if (State != RushState.Playing || Rules == null || !Solo.BattleRun) return;

        if (Incoming.HasValue && hit.Perfect && !blockedThisAttack)
        {
            blockedThisAttack = true;
            Send(MatchMessage.ShieldFrom(nextSeq++, Rules.MySlot, incomingFrom, (byte)Incoming.Value));
            AttackBlocked?.Invoke(NameOf(incomingFrom));
            AudioManager.PlayOne(Sfx.Shield);
        }

        if (!RushBattleRules.ShouldAttack(hit.PerfectStreak)) return;

        var target = Rules.PickTarget();
        if (target == null) return;

        var kind = RushBattleRules.KindFor(random.NextUInt());
        Send(MatchMessage.AttackOn(nextSeq++, Rules.MySlot, target.Slot, (byte)kind));
        AttackSent?.Invoke(kind, target.Name);
        AudioManager.PlayOne(Sfx.AttackSend);
    }

    private void OnSoloState(SoloGameManager.GameState state)
    {
        if (state != SoloGameManager.GameState.GameOver || State != RushState.Playing || !Solo.BattleRun) return;

        StopRunning();
        ClearEffects();
        Incoming = null;

        int score = Solo.ScoreManager != null ? Solo.ScoreManager.Score : 0;
        Rules.SetFinal(Rules.MySlot, score);
        Send(MatchMessage.RushFinal(Rules.MySlot, score));
        ScoresChanged?.Invoke();

        SetState(RushState.Finished);
        Run(WaitForOthers());
        TryFinish();
    }

    private IEnumerator WaitForOthers()
    {
        float deadline = Time.realtimeSinceStartup + FinalWait + (Mode != null ? Mode.DurationSeconds : 60f);
        while (State == RushState.Finished && Time.realtimeSinceStartup < deadline) yield return null;
        if (State != RushState.Finished) yield break;

        foreach (var player in Rules.Players)
            if (player.Active) Rules.MarkLeft(player.Slot);
        TryFinish();
    }

    private void TryFinish()
    {
        if (Rules == null) return;

        if (State == RushState.Playing && Rules.AloneLeft)
        {
            Solo.GameOver();
            return;
        }

        if (State != RushState.Finished || !Rules.AllDone) return;

        StopRunning();
        if (!recorded && Records != null) Records.RecordMatch(0, MatchSeconds);
        recorded = true;
        if (Rules.PlaceOf(Rules.MySlot) == 1) AudioManager.PlayOne(Sfx.MatchWin);
        SetState(RushState.Result);
    }

    private void OnReceived(MatchMessage message)
    {
        if (IsHost && Relays(message.Type)) link.Send(message);

        switch (message.Type)
        {
            case MatchMessageType.RushStart:
                if (!IsHost) Begin(message.Seed, message.Roster);
                break;

            case MatchMessageType.RushScore:
                if (Rules == null || message.Slot == Rules.MySlot) break;
                Rules.SetScore(message.Slot, message.Value, message.Kind);
                ScoresChanged?.Invoke();
                break;

            case MatchMessageType.RushFinal:
                if (Rules == null || message.Slot == Rules.MySlot) break;
                Rules.SetFinal(message.Slot, message.Value);
                ScoresChanged?.Invoke();
                TryFinish();
                break;

            case MatchMessageType.Attack:
                if (Rules == null || message.Target != Rules.MySlot || message.Slot == Rules.MySlot) break;
                if (State != RushState.Playing || Incoming.HasValue || ActiveAttack.HasValue) break;
                if (message.Seq == lastIncomingSeq && message.Slot == incomingFrom) break;
                lastIncomingSeq = message.Seq;
                Run(UnderAttack((RushAttack)Mathf.Clamp(message.Kind, 0, RushBattleRules.AttackKinds - 1), message.Slot));
                break;

            case MatchMessageType.Shield:
                if (Rules == null || message.Target != Rules.MySlot) break;
                MyAttackBlocked?.Invoke(NameOf(message.Slot));
                break;

            case MatchMessageType.Reaction:
                if (Rules == null || message.Slot == Rules.MySlot) break;
                ReactionReceived?.Invoke(NameOf(message.Slot), message.Reaction);
                break;

            case MatchMessageType.Rematch:
                var player = Rules?.Find(message.Slot);
                if (player == null || message.Slot == Rules.MySlot) break;
                player.WantsRematch = true;
                RematchChanged?.Invoke();
                if (IsHost && State == RushState.Result && Rules.EveryoneWantsRematch) HostStart();
                break;
        }
    }

    private static bool Relays(MatchMessageType type)
    {
        return type == MatchMessageType.RushScore || type == MatchMessageType.RushFinal || type == MatchMessageType.Attack
            || type == MatchMessageType.Shield || type == MatchMessageType.Reaction || type == MatchMessageType.Rematch;
    }

    private IEnumerator UnderAttack(RushAttack kind, byte from)
    {
        Incoming = kind;
        incomingFrom = from;
        blockedThisAttack = false;
        AttackIncoming?.Invoke(kind, NameOf(from));
        AudioManager.PlayOne(Sfx.AttackWarn);
        HapticManager.Medium();

        float until = Time.realtimeSinceStartup + WarningSeconds;
        while (Time.realtimeSinceStartup < until && !blockedThisAttack && State == RushState.Playing) yield return null;

        Incoming = null;
        if (blockedThisAttack || State != RushState.Playing) yield break;

        Apply(kind, true);
        until = Time.realtimeSinceStartup + AttackSeconds;
        while (Time.realtimeSinceStartup < until && State == RushState.Playing) yield return null;
        Apply(kind, false);
    }

    private void Apply(RushAttack kind, bool on)
    {
        ActiveAttack = on ? kind : (RushAttack?)null;
        switch (kind)
        {
            case RushAttack.MiniPaddle:
                if (MainPaddle != null) MainPaddle.SetLengthScale(on ? MiniPaddleScale : 1f);
                break;
            case RushAttack.FastBall:
                if (Solo.Ball != null) Solo.Ball.SetSpeedBoost(on ? FastBallBoost : 1f);
                break;
        }
        AttackActive?.Invoke(kind, on);
    }

    private void ClearEffects()
    {
        if (ActiveAttack.HasValue) Apply(ActiveAttack.Value, false);
        if (MainPaddle != null && !Mathf.Approximately(MainPaddle.LengthScale, 1f)) MainPaddle.SetLengthScale(1f);
        if (Solo != null && Solo.Ball != null && !Mathf.Approximately(Solo.Ball.SpeedBoost, 1f)) Solo.Ball.SetSpeedBoost(1f);
        ActiveAttack = null;
    }

    private void Wear(string lookId)
    {
        if (Cosmetics == null || Solo == null || Solo.Ball == null || string.IsNullOrEmpty(lookId)) return;

        var look = Cosmetics.CatalogAsset.Find(lookId);
        var face = look != null ? Solo.Ball.GetComponent<BallFace>() : null;
        if (face != null) face.SetExpressions(look.Idle, look.Happy, look.Sad);
    }

    private void Send(MatchMessage message)
    {
        link?.Send(message);
    }

    private void Detach()
    {
        if (link == null) return;
        link.Received -= OnReceived;
        link.Close();
        link = null;
    }

    private void Run(IEnumerator routine)
    {
        running.Add(StartCoroutine(routine));
    }

    private void StopRunning()
    {
        foreach (var routine in running) if (routine != null) StopCoroutine(routine);
        running.Clear();
    }

    private void SetState(RushState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
