using System;
using UnityEngine;

public sealed class RunMoments : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private GameFeel Feel;
    [SerializeField] private CameraPunch Punch;
    [SerializeField] private Camera WorldCamera;

    [Header("New best")]
    [SerializeField] private int ConfettiPerSide = 16;
    [SerializeField] private float BestSpeed = 8.5f;
    [SerializeField] private float BestSpread = 50f;
    [SerializeField, Range(0f, 1f)] private float BestHeight = 0.78f;
    [SerializeField] private float BestPunch = 0.1f;

    [Header("Milestones")]
    [SerializeField] private int[] Milestones = { 10, 25, 50, 100 };
    [SerializeField] private int MilestoneStep = 50;
    [SerializeField] private int ConfettiPerMilestone = 14;
    [SerializeField] private float MilestoneSpeed = 5.5f;
    [SerializeField] private float MilestoneSpread = 150f;
    [SerializeField, Range(0f, 1f)] private float MilestoneHeight = 0.8f;

    [Header("Near miss")]
    [SerializeField, Range(0.5f, 1f)] private float EdgeOffset = 0.88f;
    [SerializeField] private int NearMissAfterHits = 3;
    [SerializeField] private float NearMissCooldown = 8f;
    [SerializeField, Range(0.05f, 1f)] private float NearMissScale = 0.4f;
    [SerializeField] private float NearMissHold = 0.12f;
    [SerializeField] private float NearMissEase = 0.3f;

    [Header("Last miss")]
    [SerializeField, Range(0.05f, 1f)] private float FallScale = 0.25f;
    [SerializeField] private float FallHold = 0.6f;
    [SerializeField] private float FallEase = 0.2f;
    [SerializeField] private float FallDepth = 0.12f;
    [SerializeField] private float FallPunch = 0.05f;

    public event Action NewBestReached;
    public event Action<int> MilestoneReached;
    public event Action NearMissed;
    public event Action LastMissStarted;

    private Ball ball;
    private BallFace face;
    private bool bestCelebrated;
    private bool falling;
    private float lastNearMiss = float.NegativeInfinity;
    private SoloGameManager.GameState lastState;

    public bool Falling => falling;
    public bool BestCelebrated => bestCelebrated;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();
        if (Punch == null) Punch = FindFirstObjectByType<CameraPunch>();
        if (WorldCamera == null) WorldCamera = Camera.main;
        ball = Game != null ? Game.Ball : null;
        face = ball != null ? ball.GetComponent<BallFace>() : null;
    }

    private void OnEnable()
    {
        if (Game != null)
        {
            Game.StateChanged += OnStateChanged;
            Game.HitScored += OnHitScored;
        }

        if (ball != null)
        {
            ball.PaddleStruck += OnPaddleStruck;
            ball.Launched += OnLaunched;
            ball.WallHit += OnWallHit;
        }
    }

    private void OnDisable()
    {
        if (Game != null)
        {
            Game.StateChanged -= OnStateChanged;
            Game.HitScored -= OnHitScored;
        }

        if (ball != null)
        {
            ball.PaddleStruck -= OnPaddleStruck;
            ball.Launched -= OnLaunched;
            ball.WallHit -= OnWallHit;
        }
    }

    private void Update()
    {
        if (falling || !Playing || Game.Rules == null || ball == null || !ball.InPlay) return;

        var paddle = Game.Player != null ? Game.Player.Paddle : null;
        if (paddle == null || !Game.Rules.MissWouldEnd(Game.Player)) return;

        Vector2 inward = paddle.Inward;
        float depth = Vector2.Dot(ball.Position - (Vector2)paddle.transform.position, inward);
        if (depth > -FallDepth || Vector2.Dot(ball.LastVelocity, inward) >= 0f) return;

        StartFall(inward);
    }

    private bool Playing => Game != null && Game.State == SoloGameManager.GameState.Playing;

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused) ResetRun();
        lastState = state;
    }

    private void ResetRun()
    {
        bestCelebrated = false;
        falling = false;
        lastNearMiss = float.NegativeInfinity;
    }

    private void OnLaunched()
    {
        falling = false;
    }

    private void OnWallHit()
    {
        var paddle = Game != null && Game.Player != null ? Game.Player.Paddle : null;
        if (!falling || paddle == null || Vector2.Dot(ball.LastVelocity, paddle.Inward) <= 0f) return;

        falling = false;
        if (Feel != null) Feel.Release();
    }

    private void OnHitScored(HitResult hit)
    {
        if (!Playing) return;

        var score = Game.ScoreManager;
        bool ranked = Game.Kind == SoloGameManager.RunKind.Normal && score != null && !score.Unranked;
        if (!bestCelebrated && score != null && RunMarks.BeatsBest(score.Score, score.PreviousBest, ranked))
        {
            bestCelebrated = true;
            CelebrateBest();
            return;
        }

        int streak = Game.Player != null ? Game.Player.HitStreak : 0;
        if (RunMarks.IsMilestone(streak, Milestones, MilestoneStep)) CelebrateMilestone(streak);
    }

    private void OnPaddleStruck(Paddle paddle, float offset)
    {
        if (!Playing || Game.Player == null || paddle != Game.Player.Paddle) return;

        if (falling)
        {
            falling = false;
            if (Feel != null) Feel.Release();
            return;
        }

        if (Time.unscaledTime - lastNearMiss < NearMissCooldown) return;
        if (!RunMarks.NearMiss(offset, EdgeOffset, Game.Player.HitStreak, NearMissAfterHits)) return;

        lastNearMiss = Time.unscaledTime;
        if (Feel != null)
        {
            Feel.Warp(NearMissScale, NearMissHold, NearMissEase);
            Feel.Flash(ball.LastContact);
        }

        NearMissed?.Invoke();
    }

    private void CelebrateBest()
    {
        if (Feel != null)
        {
            Feel.Celebrate(ViewPoint(0f, BestHeight), new Vector2(0.55f, 0.85f).normalized, ConfettiPerSide, BestSpread, BestSpeed);
            Feel.Celebrate(ViewPoint(1f, BestHeight), new Vector2(-0.55f, 0.85f).normalized, ConfettiPerSide, BestSpread, BestSpeed);
        }

        if (Punch != null) Punch.Kick(Vector2.up, BestPunch);
        HapticManager.Medium();
        NewBestReached?.Invoke();
    }

    private void CelebrateMilestone(int streak)
    {
        if (Feel != null) Feel.Celebrate(ViewPoint(0.5f, MilestoneHeight), Vector2.up, ConfettiPerMilestone, MilestoneSpread, MilestoneSpeed);
        HapticManager.Light();
        MilestoneReached?.Invoke(streak);
    }

    private void StartFall(Vector2 inward)
    {
        falling = true;
        if (Feel != null) Feel.Warp(FallScale, FallHold, FallEase);
        if (face != null) face.ShowSad();
        if (Punch != null) Punch.Kick(-inward, FallPunch);
        LastMissStarted?.Invoke();
    }

    private Vector2 ViewPoint(float x, float y)
    {
        if (WorldCamera == null) return Vector2.zero;
        return WorldCamera.ViewportToWorldPoint(new Vector3(x, y, -WorldCamera.transform.position.z));
    }
}
