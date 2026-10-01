using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class LocalMatchController : MonoBehaviour
{
    public enum MatchState
    {
        Idle,
        Setup,
        Countdown,
        Playing,
        Paused,
        Result
    }

    public readonly struct PlayerEntry
    {
        public readonly FieldSide Side;
        public readonly CosmeticItem Look;

        public PlayerEntry(FieldSide side, CosmeticItem look)
        {
            Side = side;
            Look = look;
        }
    }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Solo;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private Ball MainBall;
    [SerializeField] private Paddle MainPaddle;
    [SerializeField] private RecordsService Records;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private SkinApplier Skins;

    [Header("Modes")]
    [SerializeField] private List<GameModeDefinition> Modes = new List<GameModeDefinition>();

    [Header("Players")]
    [SerializeField] private Sprite[] PlayerPaddles = new Sprite[4];
    [SerializeField] private float DuelZoneRatio = 0.5f;
    [SerializeField] private float PartyInset = 0.8f;

    [Header("Timing")]
    [SerializeField] private int CountdownFrom = 3;
    [SerializeField] private float CountdownStep = 0.6f;
    [SerializeField] private float ServeSpread = 35f;

    public event Action<MatchState> StateChanged;
    public event Action<int> CountdownTick;
    public event Action<HitResult> HitScored;
    public event Action<Participant> PointLost;
    public event Action<Participant> PlayerEliminated;

    public MatchState State { get; private set; } = MatchState.Idle;
    public GameModeDefinition Mode { get; private set; }
    public MatchRules Rules { get; private set; }
    public IReadOnlyList<Participant> Participants => participants;
    public IReadOnlyList<GameModeDefinition> ModeList => Modes;
    public IReadOnlyList<Ball> Balls => balls;
    public bool NewBestRally { get; private set; }
    public float MatchSeconds { get; private set; }
    public bool IsActive => State != MatchState.Idle;

    private readonly List<Participant> participants = new List<Participant>();
    private readonly List<PlayerEntry> entries = new List<PlayerEntry>();
    private readonly Dictionary<FieldSide, Paddle> extraPaddles = new Dictionary<FieldSide, Paddle>();
    private readonly List<Ball> balls = new List<Ball>();
    private readonly List<Action> unsubscribers = new List<Action>();
    private readonly List<Coroutine> running = new List<Coroutine>();
    private MatchRandom random = new MatchRandom(1);

    private void Awake()
    {
        if (Solo == null) Solo = FindFirstObjectByType<SoloGameManager>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (MainBall == null) MainBall = Solo != null ? Solo.Ball : FindFirstObjectByType<Ball>();
        if (MainPaddle == null) MainPaddle = FindFirstObjectByType<Paddle>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Skins == null) Skins = FindFirstObjectByType<SkinApplier>();
    }

    private void Update()
    {
        if (State != MatchState.Playing || Rules == null) return;

        MatchSeconds += Time.deltaTime;
        if (Rules.Tick(Time.deltaTime)) EndMatch();
    }

    public static FieldSide[] SidesFor(GameModeDefinition mode)
    {
        if (mode != null && mode.Topology == FieldTopology.FourSides)
            return new[] { FieldSide.Bottom, FieldSide.Top, FieldSide.Left, FieldSide.Right };
        return new[] { FieldSide.Bottom, FieldSide.Top };
    }

    public static bool FitsScreen(GameModeDefinition mode)
    {
        if (mode == null || !mode.TabletOnly) return true;

        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        float shortEdgeDp = Mathf.Min(Screen.width, Screen.height) / (dpi / 160f);
        return shortEdgeDp >= 600f;
    }

    public GameModeDefinition FindMode(string id)
    {
        for (int i = 0; i < Modes.Count; i++)
            if (Modes[i] != null && Modes[i].Id == id) return Modes[i];
        return null;
    }

    public Sprite PaddleSpriteFor(FieldSide side)
    {
        int index = (int)side;
        return PlayerPaddles != null && index < PlayerPaddles.Length ? PlayerPaddles[index] : null;
    }

    public void Open(GameModeDefinition mode)
    {
        if (mode == null) return;
        if (Solo != null && Solo.State != SoloGameManager.GameState.Menu) return;

        StopRunning();
        Time.timeScale = 1f;
        Mode = mode;
        entries.Clear();
        if (Solo != null) Solo.HideGameObjects();
        SetState(MatchState.Setup);
    }

    public void Cancel()
    {
        if (State != MatchState.Setup) return;

        Mode = null;
        entries.Clear();
        SetState(MatchState.Idle);
    }

    public void Begin(IReadOnlyList<PlayerEntry> players)
    {
        if (Mode == null || players == null || players.Count < Mathf.Max(1, Mode.MinPlayers)) return;

        entries.Clear();
        entries.AddRange(players);
        StartMatch();
    }

    public void Rematch()
    {
        if (Mode == null || entries.Count == 0) return;
        StartMatch();
    }

    public void SetPaused(bool paused)
    {
        if (paused && State != MatchState.Playing) return;
        if (!paused && State != MatchState.Paused) return;

        Time.timeScale = paused ? 0f : 1f;
        SetInputs(!paused);
        SetState(paused ? MatchState.Paused : MatchState.Playing);
    }

    public void Exit()
    {
        if (State == MatchState.Idle) return;

        StopRunning();
        Unsubscribe();
        Time.timeScale = 1f;

        foreach (var ball in balls)
            if (ball != null && ball != MainBall) Destroy(ball.gameObject);
        balls.Clear();

        foreach (var paddle in extraPaddles.Values)
            if (paddle != null) Destroy(paddle.gameObject);
        extraPaddles.Clear();

        if (MainPaddle != null)
        {
            MainPaddle.InputEnabled = false;
            MainPaddle.ResetToDefault();
        }

        if (MainBall != null)
        {
            MainBall.ClearServeOrigin();
            MainBall.SetServer(MainPaddle);
        }

        if (Layout != null) Layout.SetTopology(FieldTopology.Solo);
        if (Skins != null) Skins.Refresh();

        participants.Clear();
        Rules = null;
        Mode = null;
        SetState(MatchState.Idle);

        if (Solo != null) Solo.ReturnToMenu();
    }

    private void StartMatch()
    {
        StopRunning();
        Unsubscribe();
        Time.timeScale = 1f;

        Layout.SetTopology(Mode.Topology);
        BuildParticipants();

        Rules = MatchRules.Create(Mode);
        Rules.Begin(participants);
        random = new MatchRandom(unchecked((int)DateTime.UtcNow.Ticks));
        MatchSeconds = 0f;
        NewBestRally = false;

        PrepareBalls();
        SetInputs(true);

        SetState(MatchState.Countdown);
        Run(Countdown());
    }

    private void BuildParticipants()
    {
        participants.Clear();

        var keep = new HashSet<FieldSide>();
        foreach (var entry in entries) keep.Add(entry.Side);
        foreach (var side in new List<FieldSide>(extraPaddles.Keys))
        {
            if (keep.Contains(side)) continue;
            if (extraPaddles[side] != null) Destroy(extraPaddles[side].gameObject);
            extraPaddles.Remove(side);
        }

        bool party = Mode.Topology == FieldTopology.FourSides;
        float duelInset = MainPaddle.DefaultHome.y - Layout.Field.yMin;

        foreach (var entry in entries)
        {
            var paddle = entry.Side == FieldSide.Bottom ? MainPaddle : ExtraPaddle(entry.Side);
            Vector2 home = Layout.HomeFor(entry.Side, party ? PartyInset : duelInset);

            paddle.Configure(entry.Side, home, new TouchZoneController(entry.Side, ZoneFor(entry.Side), true));
            paddle.SetSprite(PaddleSpriteFor(entry.Side));
            paddle.SetVisible(true);
            paddle.InputEnabled = false;

            participants.Add(new Participant((int)entry.Side, entry.Side, paddle) { Look = entry.Look });
        }

        if (!keep.Contains(FieldSide.Bottom))
        {
            MainPaddle.InputEnabled = false;
            MainPaddle.SetVisible(false);
        }

        foreach (var side in SidesFor(Mode))
            if (!keep.Contains(side)) Layout.CloseSide(side);
    }

    private Paddle ExtraPaddle(FieldSide side)
    {
        if (extraPaddles.TryGetValue(side, out var existing) && existing != null) return existing;

        var clone = Instantiate(MainPaddle.gameObject, MainPaddle.transform.parent);
        clone.name = $"paddle_{side.ToString().ToLowerInvariant()}";
        var paddle = clone.GetComponent<Paddle>();
        extraPaddles[side] = paddle;
        return paddle;
    }

    private Func<Vector2, bool> ZoneFor(FieldSide side)
    {
        if (Mode.Topology != FieldTopology.FourSides)
            return p => TouchZoneController.InZone(side, DuelZoneRatio, new Vector2(Screen.width, Screen.height), p);

        return p => TouchZoneController.NearestEdge(CourtOnScreen(), p) == side;
    }

    private Rect CourtOnScreen()
    {
        var cam = Camera.main;
        if (cam == null) return new Rect(0f, 0f, Screen.width, Screen.height);

        Rect f = Layout.Field;
        float depth = -cam.transform.position.z;
        Vector3 min = cam.WorldToScreenPoint(new Vector3(f.xMin, f.yMin, depth));
        Vector3 max = cam.WorldToScreenPoint(new Vector3(f.xMax, f.yMax, depth));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void PrepareBalls()
    {
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            if (balls[i] != null && balls[i] != MainBall) Destroy(balls[i].gameObject);
        }
        balls.Clear();

        AddBall(MainBall);
        MainBall.StopRound();
    }

    private void AddBall(Ball ball)
    {
        balls.Add(ball);

        Action<Paddle, float> struck = (paddle, offset) => OnPaddleStruck(ball, paddle, offset);
        Action<Goal> goal = g => OnGoalEntered(ball, g);
        ball.PaddleStruck += struck;
        ball.GoalEntered += goal;
        unsubscribers.Add(() =>
        {
            if (ball == null) return;
            ball.PaddleStruck -= struck;
            ball.GoalEntered -= goal;
        });

        if (ball == MainBall) return;

        Action wall = () => AudioManager.PlayOne(Sfx.WallBounce);
        ball.WallHit += wall;
        unsubscribers.Add(() =>
        {
            if (ball != null) ball.WallHit -= wall;
        });
    }

    private IEnumerator Countdown()
    {
        for (int n = CountdownFrom; n >= 1; n--)
        {
            CountdownTick?.Invoke(n);
            yield return new WaitForSeconds(CountdownStep);
        }

        CountdownTick?.Invoke(0);
        SetState(MatchState.Playing);

        if (Mode.Topology == FieldTopology.FourSides) AutoServe(MainBall);
        else ServeFrom(MainBall, participants[(int)(random.NextUInt() % (uint)participants.Count)]);
    }

    private void ServeFrom(Ball ball, Participant server)
    {
        Wear(ball, server);
        ball.ServeFrom(server.Paddle);
        ball.StartRound();
        ball.EnableServe();
    }

    private void AutoServe(Ball ball)
    {
        var target = RandomAlive();
        if (target == null) return;

        Wear(ball, target);
        Vector2 toward = -target.Side.Inward();
        float spread = random.Range(-ServeSpread, ServeSpread);
        Vector2 direction = Quaternion.Euler(0f, 0f, spread) * toward;
        ball.ServeToward(Layout.Field.center, direction);
    }

    private Participant RandomAlive()
    {
        int alive = 0;
        foreach (var p in participants) if (!p.Eliminated) alive++;
        if (alive == 0) return null;

        int pick = (int)(random.NextUInt() % (uint)alive);
        foreach (var p in participants)
        {
            if (p.Eliminated) continue;
            if (pick-- == 0) return p;
        }
        return null;
    }

    private static void Wear(Ball ball, Participant participant)
    {
        var look = participant?.Look;
        if (look == null) return;

        var face = ball.GetComponent<BallFace>();
        if (face != null) face.SetExpressions(look.Idle, look.Happy, look.Sad);
    }

    private void OnPaddleStruck(Ball ball, Paddle paddle, float offset)
    {
        if (State != MatchState.Playing || Rules == null) return;

        var hitter = Find(paddle);
        if (hitter == null) return;

        var hit = Rules.ScoreHit(hitter, offset);
        hitter.RegisterHit();
        Wear(ball, hitter);

        if (Mode.Kind == GameModeKind.CoopRally && hit.Perfect) ball.ScaleSpeed(1f - Mode.PerfectSlowdown);

        HitScored?.Invoke(hit);

        if (Rules.BallsInPlay > balls.Count) SpawnExtraBall();
    }

    private void SpawnExtraBall()
    {
        var clone = Instantiate(MainBall.gameObject, MainBall.transform.parent);
        clone.name = "ball_extra";
        var ball = clone.GetComponent<Ball>();
        AddBall(ball);
        Run(ServeLater(ball, 0f));
    }

    private void OnGoalEntered(Ball ball, Goal goal)
    {
        if (State != MatchState.Playing || Rules == null) return;

        var conceded = Find(goal.Side);
        if (conceded == null) return;

        conceded.BreakStreak();
        bool over = Rules.OnMiss(conceded);
        PointLost?.Invoke(conceded);

        if (conceded.Eliminated)
        {
            Layout.CloseSide(conceded.Side);
            conceded.Paddle.InputEnabled = false;
            conceded.Paddle.SetVisible(false);
            PlayerEliminated?.Invoke(conceded);
        }

        if (over)
        {
            EndMatch();
            return;
        }

        if (Mode.Topology == FieldTopology.FourSides) Run(ServeLater(ball, Mode.RespawnDelaySeconds));
        else Run(ServeAfterPoint(ball, conceded));
    }

    private IEnumerator ServeLater(Ball ball, float delay)
    {
        yield return null;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (State != MatchState.Playing && State != MatchState.Paused) yield break;
        while (State == MatchState.Paused) yield return null;
        if (ball != null) AutoServe(ball);
    }

    private IEnumerator ServeAfterPoint(Ball ball, Participant server)
    {
        yield return new WaitForSeconds(Mode.RespawnDelaySeconds);
        if (State != MatchState.Playing && State != MatchState.Paused) yield break;
        if (ball != null) ServeFrom(ball, server);
    }

    private void EndMatch()
    {
        if (State == MatchState.Result) return;

        StopRunning();
        SetInputs(false);
        foreach (var ball in balls) if (ball != null) ball.StopRound();
        Time.timeScale = 0f;

        int rally = Mode.Kind == GameModeKind.CoopRally ? Rules.TeamScore : 0;
        if (Records != null) NewBestRally = Records.RecordMatch(rally, MatchSeconds) && rally > 0;
        if (Cosmetics != null) Cosmetics.EvaluateStreakRewards();

        SetState(MatchState.Result);
    }

    private Participant Find(Paddle paddle)
    {
        foreach (var p in participants) if (p.Paddle == paddle) return p;
        return null;
    }

    private Participant Find(FieldSide side)
    {
        foreach (var p in participants) if (p.Side == side) return p;
        return null;
    }

    private void SetInputs(bool enabled)
    {
        foreach (var p in participants)
            if (p.Paddle != null) p.Paddle.InputEnabled = enabled && !p.Eliminated;
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

    private void Unsubscribe()
    {
        foreach (var action in unsubscribers) action();
        unsubscribers.Clear();
    }

    private void SetState(MatchState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
