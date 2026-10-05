using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class OnlineMatchController : MonoBehaviour
{
    public enum MatchState
    {
        Idle,
        Waiting,
        Countdown,
        Playing,
        Result
    }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Solo;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private Ball MainBall;
    [SerializeField] private Paddle MainPaddle;
    [SerializeField] private RecordsService Records;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private SkinApplier Skins;
    [SerializeField] private PortalView Portal;
    [SerializeField] private OnlineScores Scores;
    [SerializeField] private LiveDuel Live;

    [Header("Timing")]
    [SerializeField] private int CountdownFrom = 3;
    [SerializeField] private float CountdownStep = 0.6f;
    [SerializeField] private float PortalDelay = 0.5f;
    [SerializeField] private float ServeDelay = 1f;
    [SerializeField] private float PeerWait = 10f;

    [Header("Portal")]
    [SerializeField] private float EntryInset = 0.6f;
    [SerializeField] private float EdgeMargin = 0.35f;

    public event Action<MatchState> StateChanged;
    public event Action<int> CountdownTick;
    public event Action<int> PaddleHit;
    public event Action ScoreChanged;
    public event Action<byte> ReactionReceived;
    public event Action<float> BallLeaving;
    public event Action<float> BallArriving;
    public event Action RematchChanged;
    public event Action<bool> PeerMissingChanged;

    public MatchState State { get; private set; } = MatchState.Idle;
    public GameModeDefinition Mode { get; private set; }
    public OnlineMatchRules Rules { get; private set; }
    public bool IsHost { get; private set; }
    public string OpponentName { get; private set; }
    public string OpponentLook { get; private set; }
    public string MyLook { get; private set; }
    public bool IsActive => State != MatchState.Idle;
    public bool Coop => Rules != null && Rules.Coop;
    public int MyScore => Rules != null ? Rules.ScoreFor(IsHost) : 0;
    public int OpponentScore => Rules != null ? Rules.ScoreFor(!IsHost) : 0;
    public bool Won => Rules != null && Rules.Ended && !Rules.Coop && Rules.HostWon == IsHost;
    public bool WantsRematch { get; private set; }
    public bool OpponentWantsRematch { get; private set; }
    public bool PeerMissing { get; private set; }
    public float PeerWaitRemaining => PeerMissing ? Mathf.Max(0f, peerDeadline - Time.realtimeSinceStartup) : 0f;
    public int PingMs => link != null ? link.PingMs : -1;
    public bool MyServe => Rules != null && Rules.HostServes == IsHost;
    public bool IsLive => Mode != null && Mode.Kind == GameModeKind.LiveDuel;
    public bool AwaitingMyServe => IsLive ? Live != null && Live.AwaitingMyServe : MainBall != null && MainBall.WaitingForServe;
    public float MatchSeconds { get; private set; }

    private readonly List<Coroutine> running = new List<Coroutine>();
    private IMatchLink link;
    private ushort nextSeq;
    private int lastHandoff = -1;
    private int lastMiss = -1;
    private ushort rally;
    private float peerDeadline;
    private bool subscribed;

    private void Awake()
    {
        if (Solo == null) Solo = FindFirstObjectByType<SoloGameManager>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (MainBall == null) MainBall = Solo != null ? Solo.Ball : FindFirstObjectByType<Ball>();
        if (MainPaddle == null) MainPaddle = FindFirstObjectByType<Paddle>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Skins == null) Skins = FindFirstObjectByType<SkinApplier>();
        if (Portal == null) Portal = FindFirstObjectByType<PortalView>();
        if (Scores == null) Scores = FindFirstObjectByType<OnlineScores>();
        if (Live == null) Live = FindFirstObjectByType<LiveDuel>();
        if (Live != null)
        {
            Live.Hit += OnLiveHit;
            Live.Goal += OnLiveGoal;
        }
    }

    private void Update()
    {
        if (State == MatchState.Playing) MatchSeconds += Time.unscaledDeltaTime;

        if (PeerMissing && Time.realtimeSinceStartup >= peerDeadline && State != MatchState.Result && Rules != null)
        {
            Rules.Forfeit(!IsHost);
            if (IsHost) Send(MatchMessage.Score(Rules));
            EndMatch();
        }
    }

    private void OnDestroy()
    {
        Detach();
        if (Live == null) return;
        Live.Hit -= OnLiveHit;
        Live.Goal -= OnLiveGoal;
    }

    public void Open(IMatchLink matchLink, GameModeDefinition mode, string myLook, string opponentName, string opponentLook)
    {
        if (matchLink == null || mode == null) return;
        if (Solo != null && Solo.State != SoloGameManager.GameState.Menu) return;

        Detach();
        link = matchLink;
        link.Received += OnReceived;
        link.PeerChanged += OnPeerChanged;
        subscribed = true;

        Mode = mode;
        IsHost = link.IsHost;
        MyLook = myLook;
        OpponentName = opponentName;
        OpponentLook = opponentLook;
        Rules = null;
        WantsRematch = false;
        OpponentWantsRematch = false;
        PeerMissing = false;

        PrepareWorld();
        SetState(MatchState.Waiting);
    }

    public void HostStart()
    {
        if (!IsHost || link == null || State == MatchState.Countdown || State == MatchState.Playing) return;

        int seed = unchecked((int)DateTime.UtcNow.Ticks);
        bool hostServes = (new MatchRandom(seed).NextUInt() & 1u) == 0u;
        Send(MatchMessage.Start(seed, hostServes));
        Begin(seed, hostServes);
    }

    public void RequestRematch()
    {
        if (State != MatchState.Result || WantsRematch) return;

        WantsRematch = true;
        Send(MatchMessage.RematchRequest());
        RematchChanged?.Invoke();
        if (IsHost && OpponentWantsRematch) HostStart();
    }

    public void OpponentLeft()
    {
        if (Rules == null || State == MatchState.Result || State == MatchState.Idle || State == MatchState.Waiting) return;

        Rules.Forfeit(!IsHost);
        if (IsHost) Send(MatchMessage.Score(Rules));
        EndMatch();
    }

    public void SendReaction(byte reaction)
    {
        if (State == MatchState.Idle) return;
        Send(MatchMessage.ReactionOf(reaction));
    }

    public void Exit()
    {
        if (State == MatchState.Idle) return;

        StopRunning();
        Detach();
        Time.timeScale = 1f;

        if (MainBall != null)
        {
            MainBall.StopRound();
            MainBall.ClearServeOrigin();
            MainBall.SetServer(MainPaddle);
        }

        if (MainPaddle != null)
        {
            MainPaddle.InputEnabled = false;
            MainPaddle.ResetToDefault();
        }

        if (Live != null) Live.End();
        if (Layout != null) Layout.SetTopology(FieldTopology.Solo);
        if (Skins != null) Skins.Refresh();
        if (Portal != null) Portal.Hide();

        Rules = null;
        Mode = null;
        PeerMissing = false;
        SetState(MatchState.Idle);

        if (Solo != null) Solo.ReturnToMenu();
    }

    public Vector2 PortalPoint(float normalizedX)
    {
        if (Layout == null) return Vector2.zero;
        Rect f = Layout.Field;
        float half = Mathf.Max(0.01f, f.width / 2f - EdgeMargin);
        return new Vector2(f.center.x + Mathf.Clamp(normalizedX, -1f, 1f) * half, f.yMax);
    }

    private void PrepareWorld()
    {
        StopRunning();
        Time.timeScale = 1f;

        if (IsLive && Live != null)
        {
            if (Portal != null) Portal.Hide();
            Live.Prepare();
            return;
        }

        Layout.SetTopology(FieldTopology.Portal);
        MainPaddle.ResetToDefault();
        MainPaddle.SetVisible(true);
        MainPaddle.InputEnabled = false;
        if (Skins != null) Skins.Refresh();

        MainBall.StopRound();
        MainBall.ClearServeOrigin();
        MainBall.SetServer(MainPaddle);
        MainBall.PaddleStruck -= OnPaddleStruck;
        MainBall.GoalEntered -= OnGoalEntered;
        MainBall.PaddleStruck += OnPaddleStruck;
        MainBall.GoalEntered += OnGoalEntered;
    }

    private void Begin(int seed, bool hostServes)
    {
        StopRunning();
        Rules = new OnlineMatchRules(Mode.Kind == GameModeKind.CoopRally, Mode.PointsToWin, Mode.Lives, hostServes);
        WantsRematch = false;
        OpponentWantsRematch = false;
        lastHandoff = -1;
        lastMiss = -1;
        rally = 0;
        MatchSeconds = 0f;

        PrepareWorld();
        if (IsLive && Live != null) Live.Begin(link, seed, hostServes, Mode.PointsToWin, MyLook, OpponentLook);
        ScoreChanged?.Invoke();
        RematchChanged?.Invoke();
        SetState(MatchState.Countdown);
        Run(Countdown());
    }

    private IEnumerator Countdown()
    {
        for (int n = CountdownFrom; n >= 1; n--)
        {
            CountdownTick?.Invoke(n);
            yield return new WaitForSecondsRealtime(CountdownStep);
        }

        CountdownTick?.Invoke(0);
        if (IsLive && Live != null)
        {
            SetState(MatchState.Playing);
            Live.Go();
            yield break;
        }

        MainPaddle.InputEnabled = true;
        SetState(MatchState.Playing);
        if (MyServe) ServeLocal();
    }

    private void ServeLocal()
    {
        rally = 0;
        Wear(MyLook);
        MainBall.ServeFrom(MainPaddle);
        MainBall.StartRound();
        MainBall.EnableServe();
    }

    private IEnumerator ServeLater()
    {
        yield return new WaitForSecondsRealtime(ServeDelay);
        if (State == MatchState.Playing && !PeerMissing && MyServe) ServeLocal();
    }

    private void OnPaddleStruck(Paddle paddle, float offset)
    {
        if (State != MatchState.Playing || paddle != MainPaddle) return;

        rally++;
        Wear(MyLook);
        PaddleHit?.Invoke(rally);
    }

    private void OnGoalEntered(Goal goal)
    {
        if (State != MatchState.Playing || Rules == null) return;

        if (goal.Portal)
        {
            SendHandoff();
            return;
        }

        if (IsHost) ApplyMiss(true);
        else Send(MatchMessage.Miss(nextSeq++));
    }

    private void SendHandoff()
    {
        Rect f = Layout.Field;
        float half = Mathf.Max(0.01f, f.width / 2f - EdgeMargin);
        float x = Mathf.Clamp((MainBall.ExitPoint.x - f.center.x) / half, -1f, 1f);
        Vector2 dir = MainBall.LastVelocity.sqrMagnitude > 0.0001f ? MainBall.LastVelocity.normalized : Vector2.up;

        Send(MatchMessage.Handoff(nextSeq++, x, dir.x, dir.y, MainBall.CurrentSpeed, rally, MyLook));
        Rules.Pass();
        if (Rules.Coop) ScoreChanged?.Invoke();
        if (Portal != null) Portal.Leave(PortalPoint(x));
        AudioManager.PlayOne(Sfx.Portal);
        BallLeaving?.Invoke(x);
    }

    private void OnHandoff(MatchMessage message)
    {
        if (State != MatchState.Playing && State != MatchState.Countdown) return;
        if (message.Seq == lastHandoff) return;
        lastHandoff = message.Seq;

        rally = message.Rally;
        Rules.Pass();
        if (Rules.Coop) ScoreChanged?.Invoke();

        float x = -message.X;
        if (Portal != null) Portal.Arrive(PortalPoint(x));
        AudioManager.PlayOne(Sfx.Portal);
        BallArriving?.Invoke(x);
        Run(Arrive(x, new Vector2(-message.DirX, -message.DirY), message.Speed, message.Look));
    }

    private IEnumerator Arrive(float x, Vector2 direction, float speed, string look)
    {
        yield return new WaitForSecondsRealtime(PortalDelay);
        while (State == MatchState.Countdown) yield return null;
        if (State != MatchState.Playing) yield break;

        Wear(look);
        Vector2 origin = PortalPoint(x) + Vector2.down * EntryInset;
        if (direction.y > -0.1f) direction = new Vector2(direction.x, -Mathf.Abs(direction.y) - 0.1f);
        MainBall.Enter(origin, direction, speed);
    }

    private void ApplyMiss(bool hostMissed)
    {
        Rules.Miss(hostMissed);
        Send(MatchMessage.Score(Rules));
        OnScore();
    }

    private void OnScore()
    {
        ScoreChanged?.Invoke();
        AudioManager.PlayOne(Sfx.Point);

        if (Rules.Ended)
        {
            EndMatch();
            return;
        }

        if (!IsLive && MyServe) Run(ServeLater());
    }

    private void OnLiveHit(int rallyCount, bool mine)
    {
        if (State != MatchState.Playing) return;
        rally = (ushort)Mathf.Min(rallyCount, ushort.MaxValue);
        PaddleHit?.Invoke(rally);
    }

    private void OnLiveGoal(bool hostMissed)
    {
        if (State != MatchState.Playing || Rules == null || !IsLive) return;
        Rules.Miss(hostMissed);
        OnScore();
    }

    private void EndMatch()
    {
        if (State == MatchState.Result) return;

        StopRunning();
        PeerMissing = false;
        MainPaddle.InputEnabled = false;
        MainBall.StopRound();
        if (IsLive && Live != null) Live.Freeze();

        int passes = Rules != null && Rules.Coop ? Rules.Passes : 0;
        if (Records != null) Records.RecordMatch(passes, MatchSeconds);
        if (Scores != null && passes > 0) _ = Scores.SubmitAsync(OnlineScores.CoopBoard, passes, OpponentName);
        if (Cosmetics != null) Cosmetics.EvaluateStreakRewards();
        if (Won || (Rules != null && Rules.Coop)) AudioManager.PlayOne(Sfx.MatchWin);

        SetState(MatchState.Result);
    }

    private void OnReceived(MatchMessage message)
    {
        switch (message.Type)
        {
            case MatchMessageType.Start:
                if (!IsHost) Begin(message.Seed, message.HostServes);
                break;

            case MatchMessageType.Handoff:
                OnHandoff(message);
                break;

            case MatchMessageType.Miss:
                if (!IsHost || State != MatchState.Playing || message.Seq == lastMiss) break;
                lastMiss = message.Seq;
                ApplyMiss(false);
                break;

            case MatchMessageType.Score:
                if (IsHost || Rules == null) break;
                bool wasEnded = Rules.Ended;
                Rules.Apply(message);
                if (!wasEnded) OnScore();
                break;

            case MatchMessageType.Reaction:
                ReactionReceived?.Invoke(message.Reaction);
                break;

            case MatchMessageType.Rematch:
                OpponentWantsRematch = true;
                RematchChanged?.Invoke();
                if (IsHost && WantsRematch) HostStart();
                break;
        }
    }

    private void OnPeerChanged(bool connected)
    {
        if (State == MatchState.Idle || State == MatchState.Result) return;

        if (!connected && !PeerMissing)
        {
            PeerMissing = true;
            peerDeadline = Time.realtimeSinceStartup + PeerWait;
            MainPaddle.InputEnabled = false;
            MainBall.StopRound();
            PeerMissingChanged?.Invoke(true);
            return;
        }

        if (connected && PeerMissing)
        {
            PeerMissing = false;
            PeerMissingChanged?.Invoke(false);
            if (State != MatchState.Playing || IsLive) return;

            MainPaddle.InputEnabled = true;
            if (IsHost) Send(MatchMessage.Score(Rules));
            if (MyServe) Run(ServeLater());
        }
    }

    private void Wear(string lookId)
    {
        if (Cosmetics == null || string.IsNullOrEmpty(lookId)) return;

        var look = Cosmetics.CatalogAsset.Find(lookId);
        if (look == null) return;

        var face = MainBall.GetComponent<BallFace>();
        if (face != null) face.SetExpressions(look.Idle, look.Happy, look.Sad);
    }

    private void Send(MatchMessage message)
    {
        link?.Send(message);
    }

    private void Detach()
    {
        if (MainBall != null)
        {
            MainBall.PaddleStruck -= OnPaddleStruck;
            MainBall.GoalEntered -= OnGoalEntered;
        }

        if (link == null || !subscribed) return;
        link.Received -= OnReceived;
        link.PeerChanged -= OnPeerChanged;
        link.Close();
        link = null;
        subscribed = false;
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

    private void SetState(MatchState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
