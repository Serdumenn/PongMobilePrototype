using System;
using System.IO;
using UnityEngine;

public sealed class GhostRecorder : MonoBehaviour
{
    public const string FileName = "ghost_last.bin";

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private FieldLayout Layout;

    public event Action<GhostRun> Recorded;

    public GhostRun Current => recording;
    public GhostRun Finished { get; private set; }
    public bool Started => started;
    public float Elapsed => started ? Time.time - startTime : 0f;

    public GhostRun LastRun
    {
        get
        {
            if (!loaded) Load();
            return lastRun;
        }
    }

    private GhostRun recording;
    private GhostRun lastRun;
    private bool loaded;
    private bool started;
    private float startTime;
    private Vector2 lastBall;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
    }

    private void Start()
    {
        if (Game == null) return;

        Game.StateChanged += OnState;
        Game.HitScored += OnHit;

        var ball = Game.Ball;
        if (ball == null) return;
        ball.Launched += OnLaunched;
        ball.RoundReset += OnAppear;
        ball.PaddleHit += OnPaddle;
        ball.WallHit += OnWall;
        ball.GoalEntered += OnGoal;
    }

    private void OnDestroy()
    {
        if (Game == null) return;

        Game.StateChanged -= OnState;
        Game.HitScored -= OnHit;

        var ball = Game.Ball;
        if (ball == null) return;
        ball.Launched -= OnLaunched;
        ball.RoundReset -= OnAppear;
        ball.PaddleHit -= OnPaddle;
        ball.WallHit -= OnWall;
        ball.GoalEntered -= OnGoal;
    }

    private void Update()
    {
        if (recording == null || !started || Game.State != SoloGameManager.GameState.Playing) return;

        var ball = Game.Ball;
        if (ball != null && ball.InPlay) lastBall = ball.Position;

        var paddle = Game.Player?.Paddle;
        float x = paddle != null ? GhostRun.ToField(Field, paddle.transform.position).x : 0f;
        float elapsed = Elapsed;
        while (recording.Paddle.Count * GhostRun.PaddleStep <= elapsed && recording.Paddle.Count < 1000) recording.AddPaddle(x);
    }

    private Rect Field => Layout != null ? Layout.Field : default;

    private bool ShouldRecord()
    {
        return Game.RunMode != null && Game.RunMode.Kind == GameModeKind.Rush && !Game.BattleRun;
    }

    private void OnState(SoloGameManager.GameState state)
    {
        switch (state)
        {
            case SoloGameManager.GameState.Playing:
                if (recording == null && ShouldRecord()) Begin();
                break;

            case SoloGameManager.GameState.GameOver:
                Finish();
                break;

            case SoloGameManager.GameState.Menu:
                recording = null;
                started = false;
                break;
        }
    }

    public GhostRun FinishNow()
    {
        if (recording != null) Finish();
        return Finished;
    }

    private void Begin()
    {
        Finished = null;
        recording = new GhostRun
        {
            Seed = Game.RunSeed,
            BallSkin = SkinId(CosmeticCategory.Ball),
            PaddleSkin = SkinId(CosmeticCategory.Paddle)
        };
        started = false;
    }

    private string SkinId(CosmeticCategory category)
    {
        var item = Cosmetics != null ? Cosmetics.Equipped(category) : null;
        return item != null ? item.Id : string.Empty;
    }

    private void Finish()
    {
        var run = recording;
        recording = null;
        Finished = null;
        if (run == null || !started) return;

        float elapsed = Elapsed;
        started = false;
        run.AddBall(elapsed, GhostRun.ToField(Field, lastBall), GhostRun.KeyKind.End);
        run.Finish(elapsed, Game.ScoreManager != null ? Game.ScoreManager.Score : 0);
        if (run.Score <= 0) return;

        var packed = GhostRun.FromBytes(run.ToBytes());
        if (packed == null || packed.Problem() != null)
        {
            Debug.Log($"Run not saved as a ghost, it failed the fairness check ({packed?.Problem() ?? "unreadable"}).");
            return;
        }

        lastRun = packed;
        Finished = packed;
        loaded = true;
        Save(packed);
        Recorded?.Invoke(packed);
    }

    private void Add(GhostRun.KeyKind kind, Vector2 world)
    {
        if (recording == null || !started) return;
        recording.AddBall(Elapsed, GhostRun.ToField(Field, world), kind);
    }

    private void OnLaunched()
    {
        if (recording == null || Game.State != SoloGameManager.GameState.Playing) return;

        if (!started)
        {
            started = true;
            startTime = Time.time;
        }
        lastBall = Game.Ball.Position;
        Add(GhostRun.KeyKind.Launch, lastBall);
    }

    private void OnAppear()
    {
        if (Game.State == SoloGameManager.GameState.Playing) Add(GhostRun.KeyKind.Appear, Game.Ball.Position);
    }

    private void OnPaddle()
    {
        lastBall = Game.Ball.Position;
        Add(GhostRun.KeyKind.Paddle, lastBall);
    }

    private void OnWall()
    {
        lastBall = Game.Ball.Position;
        Add(GhostRun.KeyKind.Wall, lastBall);
    }

    private void OnGoal(Goal goal)
    {
        Add(GhostRun.KeyKind.Out, Game.Ball.ExitPoint);
    }

    private void OnHit(HitResult hit)
    {
        if (recording == null || !started || Game.ScoreManager == null) return;
        recording.AddScore(Elapsed, Game.ScoreManager.Score);
    }

    private void Save(GhostRun run)
    {
        try
        {
            File.WriteAllBytes(SaveLocation.PathFor(FileName), run.ToBytes());
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Saving the last run failed: {e.Message}");
        }
    }

    private void Load()
    {
        loaded = true;
        try
        {
            string path = SaveLocation.PathFor(FileName);
            if (!File.Exists(path)) return;

            var run = GhostRun.FromBytes(File.ReadAllBytes(path));
            lastRun = run != null && run.Problem() == null ? run : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Loading the last run failed: {e.Message}");
        }
    }

    public void Forget()
    {
        lastRun = null;
        loaded = false;
    }
}
