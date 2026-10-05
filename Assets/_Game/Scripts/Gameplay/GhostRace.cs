using System;
using UnityEngine;

public sealed class GhostRace : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private FieldLayout Layout;

    [Header("Look")]
    [SerializeField, Range(0f, 1f)] private float BallAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float PaddleAlpha = 0.32f;

    public event Action<int> GhostScoreChanged;

    public GhostRun Ghost { get; private set; }
    public string GhostName { get; private set; }
    public bool Active => Ghost != null;
    public bool Racing => Active && Game != null && Game.GhostRun;
    public int GhostScore { get; private set; }
    public float Elapsed => started ? Time.time - startTime : 0f;
    public bool BallShown => ballView != null && ballView.enabled;
    public Vector3 BallViewPosition => ballView != null ? ballView.transform.position : Vector3.zero;
    public Vector3 PaddleViewPosition => paddleView != null ? paddleView.transform.position : Vector3.zero;

    private SpriteRenderer ballView;
    private SpriteRenderer paddleView;
    private bool started;
    private float startTime;

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
        if (Game.Ball != null) Game.Ball.Launched += OnLaunched;
    }

    private void OnDestroy()
    {
        if (Game == null) return;
        Game.StateChanged -= OnState;
        if (Game.Ball != null) Game.Ball.Launched -= OnLaunched;
    }

    public bool Begin(GhostRun ghost, string ghostName)
    {
        var rush = Game != null ? Game.FindMode("rush") : null;
        if (ghost == null || rush == null) return false;

        Ghost = ghost;
        GhostName = string.IsNullOrEmpty(ghostName) ? Loc.T("Ghost") : ghostName;
        Reset();
        EnsureViews();
        ApplySkins();
        Game.StartGhostRun(rush, ghost.Seed);
        return true;
    }

    public bool Retry()
    {
        return Ghost != null && Begin(Ghost, GhostName);
    }

    public void End()
    {
        Ghost = null;
        Reset();
        HideViews();
    }

    private void Reset()
    {
        started = false;
        SetGhostScore(0);
    }

    private void OnState(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && !started) SetGhostScore(0);
        if (state == SoloGameManager.GameState.GameOver || state == SoloGameManager.GameState.Menu) HideViews();
    }

    private void OnLaunched()
    {
        if (!Racing || started || Game.State != SoloGameManager.GameState.Playing) return;
        started = true;
        startTime = Time.time;
    }

    private void Update()
    {
        if (!Racing)
        {
            HideViews();
            return;
        }

        var state = Game.State;
        if (state != SoloGameManager.GameState.Playing && state != SoloGameManager.GameState.Paused) return;

        float t = Elapsed;
        Rect field = Layout != null ? Layout.Field : default;

        if (started && Ghost.BallAt(t, out var ball))
        {
            ballView.transform.position = GhostRun.FromField(field, ball);
            ballView.enabled = true;
        }
        else ballView.enabled = false;

        var paddle = Game.Player?.Paddle;
        if (paddle != null)
        {
            float x = GhostRun.FromField(field, new Vector2(Ghost.PaddleAt(t), 0f)).x;
            if (field.width > paddle.HalfWidth * 2f) x = Mathf.Clamp(x, field.xMin + paddle.HalfWidth, field.xMax - paddle.HalfWidth);
            paddleView.transform.position = new Vector3(x, paddle.Home.y, 0f);
            paddleView.enabled = true;
        }

        SetGhostScore(started ? Ghost.ScoreAt(t) : 0);
    }

    private void SetGhostScore(int value)
    {
        if (value == GhostScore) return;
        GhostScore = value;
        GhostScoreChanged?.Invoke(value);
    }

    private void EnsureViews()
    {
        if (ballView != null) return;

        var ball = Game.Ball;
        var paddle = Game.Player?.Paddle;
        var parent = ball != null ? ball.transform.parent : transform;
        ballView = CreateView("ghost_ball", parent, ball != null ? ball.GetComponentInChildren<SpriteRenderer>() : null);
        paddleView = CreateView("ghost_paddle", parent, paddle != null ? paddle.Visual : null);
    }

    private static SpriteRenderer CreateView(string name, Transform parent, SpriteRenderer source)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var view = go.AddComponent<SpriteRenderer>();
        if (source != null)
        {
            view.sprite = source.sprite;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = source.sortingOrder - 1;
            var scale = source.transform.lossyScale;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(
                scale.x / Mathf.Max(0.0001f, parentScale.x),
                scale.y / Mathf.Max(0.0001f, parentScale.y),
                1f);
        }
        view.enabled = false;
        return view;
    }

    private void ApplySkins()
    {
        var catalog = Cosmetics != null ? Cosmetics.CatalogAsset : null;
        var ballSkin = catalog != null ? catalog.Find(Ghost.BallSkin) : null;
        var paddleSkin = catalog != null ? catalog.Find(Ghost.PaddleSkin) : null;
        if (ballSkin == null && catalog != null) ballSkin = catalog.DefaultFor(CosmeticCategory.Ball);
        if (paddleSkin == null && catalog != null) paddleSkin = catalog.DefaultFor(CosmeticCategory.Paddle);

        if (ballSkin != null && ballSkin.Idle != null) ballView.sprite = ballSkin.Idle;
        if (paddleSkin != null && paddleSkin.PaddleSprite != null) paddleView.sprite = paddleSkin.PaddleSprite;

        ballView.color = new Color(1f, 1f, 1f, BallAlpha);
        paddleView.color = new Color(1f, 1f, 1f, PaddleAlpha);
    }

    public CosmeticItem GhostBall()
    {
        var catalog = Cosmetics != null ? Cosmetics.CatalogAsset : null;
        if (catalog == null || Ghost == null) return null;
        return catalog.Find(Ghost.BallSkin) ?? catalog.DefaultFor(CosmeticCategory.Ball);
    }

    private void HideViews()
    {
        if (ballView != null) ballView.enabled = false;
        if (paddleView != null) paddleView.enabled = false;
    }
}
