using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ArcadeEvents : MonoBehaviour
{
    private const int SeedSalt = 0x7F4A7C15;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private GoldCoins Coins;
    [SerializeField] private RunPurse Purse;
    [SerializeField] private PowerUps Powers;
    [SerializeField] private TargetBoard Board;
    [SerializeField] private GameFeel Feel;
    [SerializeField] private Camera WorldCamera;

    [Header("Timing")]
    [SerializeField] private float FirstAt = 20f;
    [SerializeField] private float GapMin = 30f;
    [SerializeField] private float GapMax = 40f;

    [Header("Ball storm")]
    [SerializeField] private int StormExtraBalls = 4;
    [SerializeField] private float StormSeconds = 10f;
    [SerializeField] private int StormKeep = 3;
    [SerializeField] private int StormPointsPerBall = 3;
    [SerializeField] private int StormGold = 25;
    [SerializeField] private float StormSpeed = 1.15f;
    [SerializeField] private float StormSpread = 120f;

    [Header("Gold rain")]
    [SerializeField] private float RainSeconds = 9f;
    [SerializeField] private float RainEvery = 0.55f;
    [SerializeField] private int RainGoal = 6;
    [SerializeField] private int RainGold = 20;

    [Header("Targets")]
    [SerializeField] private int TargetCount = 5;
    [SerializeField] private float TargetSeconds = 12f;
    [SerializeField] private int TargetPoints = 3;
    [SerializeField] private int TargetGold = 25;

    [Header("Giant")]
    [SerializeField] private float GiantSeconds = 10f;
    [SerializeField] private int GiantGoal = 8;
    [SerializeField] private float GiantScale = 1.8f;
    [SerializeField] private float GiantBoost = 1.2f;
    [SerializeField] private int GiantGold = 20;

    [Header("Celebration")]
    [SerializeField] private int Confetti = 14;

    public event Action<EventKind> Started;
    public event Action Changed;
    public event Action<EventKind, bool, int> Finished;

    private readonly List<Ball> storm = new List<Ball>();
    private readonly List<Action> unhooks = new List<Action>();
    private EventSchedule schedule;
    private SoloGameManager.GameState lastState;
    private Vector3 giantRest = Vector3.one;
    private float runTime;
    private float endsAt;
    private bool launched;
    private bool running;
    private bool mainLost;

    public bool Running => running;
    public bool Active { get; private set; }
    public EventKind Current { get; private set; }
    public int Progress { get; private set; }
    public int Target { get; private set; }
    public int Total { get; private set; }
    public float Duration { get; private set; }
    public float TimeLeft => Active ? Mathf.Max(0f, endsAt - runTime) : 0f;
    public int CompletedThisRun { get; private set; }
    public int StormBalls => storm.Count;
    public float NextAt => schedule != null ? schedule.NextAt : 0f;

    private Ball MainBall => Game != null ? Game.Ball : null;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Coins == null) Coins = FindFirstObjectByType<GoldCoins>();
        if (Purse == null) Purse = FindFirstObjectByType<RunPurse>();
        if (Powers == null) Powers = FindFirstObjectByType<PowerUps>();
        if (Board == null) Board = FindFirstObjectByType<TargetBoard>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();
        if (WorldCamera == null) WorldCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (Game != null)
        {
            Game.StateChanged += OnStateChanged;
            Game.HitScored += OnHitScored;
            Game.MissSheltered += OnMainLost;
        }

        if (MainBall != null) MainBall.Launched += OnLaunched;
        if (Coins != null) Coins.Collected += OnCoin;
        if (Board != null) Board.Hit += OnTarget;
    }

    private void OnDisable()
    {
        if (Game != null)
        {
            Game.StateChanged -= OnStateChanged;
            Game.HitScored -= OnHitScored;
            Game.MissSheltered -= OnMainLost;
        }

        if (MainBall != null) MainBall.Launched -= OnLaunched;
        if (Coins != null) Coins.Collected -= OnCoin;
        if (Board != null) Board.Hit -= OnTarget;
        StopRun();
    }

    public float DurationOf(EventKind kind)
    {
        return kind switch
        {
            EventKind.Storm => StormSeconds,
            EventKind.GoldRain => RainSeconds,
            EventKind.Targets => TargetSeconds,
            _ => GiantSeconds
        };
    }

    public int GoldOf(EventKind kind)
    {
        return kind switch
        {
            EventKind.Storm => StormGold,
            EventKind.GoldRain => RainGold,
            EventKind.Targets => TargetGold,
            _ => GiantGold
        };
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused) StartRun();
        else if (state == SoloGameManager.GameState.Menu || state == SoloGameManager.GameState.GameOver) StopRun();
        lastState = state;
    }

    private void StartRun()
    {
        StopRun();
        var mode = Game.RunMode;
        running = mode != null && mode.Kind == GameModeKind.Arcade;
        schedule = new EventSchedule(unchecked(Game.RunSeed ^ SeedSalt), FirstAt, GapMin, GapMax);
        runTime = 0f;
        launched = false;
        CompletedThisRun = 0;
    }

    private void StopRun()
    {
        if (Active) Close(false, false);
        running = false;
    }

    private void OnLaunched()
    {
        if (running) launched = true;
    }

    private void Update()
    {
        if (!running || Game == null || Game.State != SoloGameManager.GameState.Playing) return;

        if (launched) runTime += Time.deltaTime;

        if (Active)
        {
            if (runTime >= endsAt) Close(true, true);
            return;
        }

        if (launched && MainBall != null && MainBall.InPlay && schedule != null && runTime >= schedule.NextAt)
            Begin(schedule.TakeKind());
    }

    public void Begin(EventKind kind)
    {
        if (Active) Close(false, false);
        if (Game == null || MainBall == null) return;

        Active = true;
        Current = kind;
        Progress = 0;
        Total = 0;
        mainLost = false;
        Duration = DurationOf(kind);
        endsAt = runTime + Duration;
        if (Powers != null) Powers.Paused = true;

        switch (kind)
        {
            case EventKind.Storm:
                Game.Sheltered = true;
                SpawnStorm();
                Total = 1 + storm.Count;
                Progress = Total;
                Target = Mathf.Min(StormKeep, Total);
                break;
            case EventKind.GoldRain:
                Target = RainGoal;
                if (Coins != null) Coins.StartRain(RainSeconds, RainEvery);
                break;
            case EventKind.Targets:
                Target = TargetCount;
                if (Board != null) Board.Show(TargetCount);
                break;
            default:
                Target = GiantGoal;
                if (Powers != null) Powers.Cancel(PowerKind.SlowBall);
                giantRest = MainBall.transform.localScale;
                MainBall.transform.localScale = giantRest * GiantScale;
                MainBall.SetSpeedBoost(GiantBoost);
                break;
        }

        Started?.Invoke(kind);
        Changed?.Invoke();
    }

    public void End()
    {
        if (Active) Close(true, true);
    }

    private void Close(bool reward, bool advance)
    {
        var kind = Current;
        bool success = reward && EventSchedule.Succeeded(kind, Progress, Target);
        Active = false;

        switch (kind)
        {
            case EventKind.Storm:
                int survivors = Progress;
                if (reward && survivors > 0) AddPoints(survivors * StormPointsPerBall);
                EndStorm();
                break;
            case EventKind.GoldRain:
                if (Coins != null) Coins.StopRain();
                break;
            case EventKind.Targets:
                if (Board != null) Board.Hide();
                break;
            default:
                if (MainBall != null)
                {
                    MainBall.transform.localScale = giantRest;
                    MainBall.SetSpeedBoost(1f);
                }
                break;
        }

        if (Powers != null) Powers.Paused = false;
        if (advance && schedule != null) schedule.Advance(runTime);

        int gold = success ? GoldOf(kind) : 0;
        if (gold > 0 && Purse != null) Purse.Add(gold);
        if (success)
        {
            CompletedThisRun++;
            Celebrate();
        }

        if (reward) Finished?.Invoke(kind, success, gold);
        Changed?.Invoke();
    }

    private void AddPoints(int points)
    {
        if (points > 0 && Game.ScoreManager != null) Game.ScoreManager.AddPoints(points);
    }

    private void OnHitScored(HitResult hit)
    {
        if (!Active || Current != EventKind.Giant) return;

        AddPoints(1);
        Step();
    }

    private void OnCoin(int value)
    {
        if (Active && Current == EventKind.GoldRain) Step();
    }

    private void OnTarget(int index)
    {
        if (!Active || Current != EventKind.Targets) return;

        AddPoints(TargetPoints);
        Step();
    }

    private void Step()
    {
        Progress++;
        Changed?.Invoke();
        if (Progress >= Target) Close(true, true);
    }

    private void OnMainLost()
    {
        if (!Active || Current != EventKind.Storm) return;

        mainLost = true;
        LoseBall();
    }

    private void LoseBall()
    {
        Progress = Mathf.Max(0, Progress - 1);
        Changed?.Invoke();
        if (Progress <= 0) Close(true, true);
    }

    private void SpawnStorm()
    {
        var main = MainBall;
        Vector2 origin = main.Position;
        float speed = Mathf.Max(0.1f, main.CurrentSpeed * StormSpeed);
        var mainBody = main.GetComponent<Collider2D>();

        for (int i = 0; i < StormExtraBalls; i++)
        {
            var copy = Instantiate(main.gameObject, main.transform.parent);
            copy.name = "ball_storm";
            var ball = copy.GetComponent<Ball>();
            var body = copy.GetComponent<Collider2D>();
            if (mainBody != null && body != null) Physics2D.IgnoreCollision(mainBody, body, true);
            foreach (var other in storm)
            {
                var otherBody = other != null ? other.GetComponent<Collider2D>() : null;
                if (otherBody != null && body != null) Physics2D.IgnoreCollision(otherBody, body, true);
            }

            Hook(ball);
            storm.Add(ball);
            ball.Enter(origin, FeelMath.Fan(Vector2.up, i, StormExtraBalls, StormSpread), speed);
        }
    }

    private void Hook(Ball ball)
    {
        Action<Paddle, float> struck = (paddle, offset) =>
        {
            if (Game != null && Game.Player != null && paddle == Game.Player.Paddle) Game.RegisterPaddleHit(offset);
        };
        Action<Goal> goal = _ => OnStormBallLost(ball);
        Action wall = () => AudioManager.PlayOne(Sfx.WallBounce);
        ball.PaddleStruck += struck;
        ball.GoalEntered += goal;
        ball.WallHit += wall;
        unhooks.Add(() =>
        {
            if (ball == null) return;
            ball.PaddleStruck -= struck;
            ball.GoalEntered -= goal;
            ball.WallHit -= wall;
        });
    }

    private void OnStormBallLost(Ball ball)
    {
        if (!storm.Remove(ball)) return;

        Destroy(ball.gameObject);
        if (Active && Current == EventKind.Storm) LoseBall();
    }

    private void EndStorm()
    {
        foreach (var unhook in unhooks) unhook();
        unhooks.Clear();

        Ball heir = null;
        foreach (var ball in storm)
            if (ball != null && ball.InPlay) heir = ball;

        if (mainLost && Game != null && Game.State == SoloGameManager.GameState.Playing && MainBall != null)
        {
            if (heir != null) MainBall.Enter(heir.Position, heir.LastVelocity.sqrMagnitude > 0.0001f ? heir.LastVelocity.normalized : Vector2.up, heir.CurrentSpeed);
            else Game.RespawnMainBall();
        }

        foreach (var ball in storm)
        {
            if (ball == null) continue;
            if (Feel != null && ball.InPlay) Feel.Flash(ball.Position);
            Destroy(ball.gameObject);
        }

        storm.Clear();
        if (Game != null) Game.Sheltered = false;
        mainLost = false;
    }

    private void Celebrate()
    {
        if (Feel == null || WorldCamera == null) return;

        Vector2 origin = WorldCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.78f, -WorldCamera.transform.position.z));
        Feel.Celebrate(origin, Vector2.up, Confetti, 150f, 5.5f);
    }
}
