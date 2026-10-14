using UnityEngine;

public enum BubbleKind
{
    Note,
    Heart,
    Flame,
    Phew,
    Star
}

public sealed class PingiBubbles : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private RunMoments Moments;
    [SerializeField] private LocalMatchController Match;
    [SerializeField] private Camera WorldCamera;

    [Header("Art")]
    [SerializeField] private Sprite Bubble;
    [SerializeField] private Sprite Note;
    [SerializeField] private Sprite Heart;
    [SerializeField] private Sprite Flame;
    [SerializeField] private Sprite Drop;
    [SerializeField] private Sprite Star;
    [SerializeField] private Color NoteColor = new Color(0.61f, 0.42f, 1f, 1f);
    [SerializeField] private Color HeartColor = new Color(1f, 0.42f, 0.55f, 1f);
    [SerializeField] private Color FlameColor = new Color(1f, 0.54f, 0.24f, 1f);
    [SerializeField] private Color DropColor = new Color(0.31f, 0.66f, 1f, 1f);
    [SerializeField] private Color StarColor = new Color(1f, 0.77f, 0.2f, 1f);

    [Header("Timing")]
    [SerializeField] private float Hold = 0.9f;
    [SerializeField] private float PopIn = 0.2f;
    [SerializeField] private float PopOut = 0.14f;
    [SerializeField] private float Gap = 1.2f;
    [SerializeField] private float IdleEvery = 7f;
    [SerializeField] private int HeartEvery = 3;

    [Header("Layout")]
    [SerializeField] private float Width = 0.62f;
    [SerializeField] private Vector2 Offset = new Vector2(0.36f, 0.5f);
    [SerializeField] private float Margin = 0.12f;
    [SerializeField, Range(0.2f, 0.8f)] private float IconShare = 0.42f;
    [SerializeField] private int Order = 30;

    private Ball ball;
    private Transform holder;
    private SpriteRenderer back;
    private SpriteRenderer icon;
    private float age = -1f;
    private float quietUntil;
    private float idle;
    private bool flameShown;

    public bool Showing => age >= 0f;
    public BubbleKind Current { get; private set; }
    public Bounds ShownBounds => back != null ? back.bounds : new Bounds();

    private float Life => PopIn + Hold + PopOut;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Moments == null) Moments = FindFirstObjectByType<RunMoments>();
        if (Match == null) Match = FindFirstObjectByType<LocalMatchController>();
        if (WorldCamera == null) WorldCamera = Camera.main;
        ball = Game != null ? Game.Ball : null;

        holder = new GameObject("PingiBubble").transform;
        holder.SetParent(transform, false);
        back = holder.gameObject.AddComponent<SpriteRenderer>();
        back.sprite = Bubble;
        back.sortingOrder = Order;

        var iconHolder = new GameObject("Icon").transform;
        iconHolder.SetParent(holder, false);
        icon = iconHolder.gameObject.AddComponent<SpriteRenderer>();
        icon.sortingOrder = Order + 1;

        Hide();
    }

    private void OnEnable()
    {
        if (Game != null)
        {
            Game.HitScored += OnHitScored;
            Game.StateChanged += OnStateChanged;
        }

        if (Moments != null)
        {
            Moments.NewBestReached += OnNewBest;
            Moments.NearMissed += OnNearMissed;
        }

        if (ball != null) ball.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        if (Game != null)
        {
            Game.HitScored -= OnHitScored;
            Game.StateChanged -= OnStateChanged;
        }

        if (Moments != null)
        {
            Moments.NewBestReached -= OnNewBest;
            Moments.NearMissed -= OnNearMissed;
        }

        if (ball != null) ball.Launched -= OnLaunched;
        Hide();
    }

    public void Show(BubbleKind kind)
    {
        if (Showing && kind < Current) return;
        if (!Showing && kind == BubbleKind.Note && Time.unscaledTime < quietUntil) return;

        var sprite = IconFor(kind);
        if (sprite == null || Bubble == null) return;

        Current = kind;
        age = 0f;
        icon.sprite = sprite;
        icon.color = ColorFor(kind);
        back.enabled = true;
        icon.enabled = true;
        Place(0f);
    }

    private void Update()
    {
        float step = FeelMath.Step(Time.unscaledDeltaTime);
        WatchIdle(step);
        WatchHeat();

        if (!Showing) return;

        age += step;
        if (age >= Life)
        {
            Hide();
            quietUntil = Time.unscaledTime + Gap;
            return;
        }

        Place(age);
    }

    private void WatchIdle(float step)
    {
        bool resting = Game != null && Game.State == SoloGameManager.GameState.Menu && (Match == null || !Match.IsActive);
        if (!resting)
        {
            idle = 0f;
            return;
        }

        idle += step;
        if (idle < IdleEvery) return;

        idle = 0f;
        Show(BubbleKind.Note);
    }

    private void WatchHeat()
    {
        if (flameShown || ball == null || !ball.InPlay || Game == null || Game.State != SoloGameManager.GameState.Playing) return;
        if (ball.Heat < 0.999f) return;

        flameShown = true;
        Show(BubbleKind.Flame);
    }

    private void Place(float time)
    {
        if (ball == null || WorldCamera == null) return;

        float scale = time < PopIn ? FeelMath.Overshoot(time / PopIn) : time < PopIn + Hold ? 1f : 1f - Mathf.Clamp01((time - PopIn - Hold) / PopOut);
        float unit = Bubble.bounds.size.x > 0.0001f ? Width / Bubble.bounds.size.x : 1f;
        holder.localScale = new Vector3(unit * scale, unit * scale, 1f);

        Vector2 size = Bubble.bounds.size * unit;
        float halfHeight = WorldCamera.orthographicSize;
        float halfWidth = halfHeight * WorldCamera.aspect;
        Vector2 center = WorldCamera.transform.position;
        Vector2 at = ball.Position;

        bool left = at.x + Offset.x + size.x * 0.5f > center.x + halfWidth - Margin;
        bool below = at.y + Offset.y + size.y * 0.5f > center.y + halfHeight - Margin;
        Vector2 spot = at + new Vector2(left ? -Offset.x : Offset.x, below ? -Offset.y : Offset.y);
        spot.x = Mathf.Clamp(spot.x, center.x - halfWidth + Margin + size.x * 0.5f, center.x + halfWidth - Margin - size.x * 0.5f);
        spot.y = Mathf.Clamp(spot.y, center.y - halfHeight + Margin + size.y * 0.5f, center.y + halfHeight - Margin - size.y * 0.5f);
        spot.y += Mathf.Sin(time * 7f) * 0.02f;

        holder.position = new Vector3(spot.x, spot.y, 0f);
        back.flipX = left;
        back.flipY = below;

        var iconSprite = icon.sprite;
        float iconWidth = iconSprite != null && iconSprite.bounds.size.x > 0.0001f ? iconSprite.bounds.size.x : 1f;
        float iconScale = Bubble.bounds.size.x * IconShare / iconWidth;
        icon.transform.localScale = new Vector3(iconScale, iconScale, 1f);
        float lift = Bubble.bounds.size.y * 0.12f;
        icon.transform.localPosition = new Vector3(0f, below ? -lift : lift, 0f);
    }

    private void Hide()
    {
        age = -1f;
        if (back != null) back.enabled = false;
        if (icon != null) icon.enabled = false;
    }

    private Sprite IconFor(BubbleKind kind)
    {
        return kind switch
        {
            BubbleKind.Heart => Heart,
            BubbleKind.Flame => Flame,
            BubbleKind.Phew => Drop,
            BubbleKind.Star => Star,
            _ => Note
        };
    }

    private Color ColorFor(BubbleKind kind)
    {
        return kind switch
        {
            BubbleKind.Heart => HeartColor,
            BubbleKind.Flame => FlameColor,
            BubbleKind.Phew => DropColor,
            BubbleKind.Star => StarColor,
            _ => NoteColor
        };
    }

    private void OnHitScored(HitResult hit)
    {
        if (hit.Perfect && HeartEvery > 0 && hit.PerfectStreak > 0 && hit.PerfectStreak % HeartEvery == 0) Show(BubbleKind.Heart);
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        idle = 0f;
        if (state != SoloGameManager.GameState.Playing) flameShown = false;
        if (state == SoloGameManager.GameState.Menu && Showing && Current != BubbleKind.Note) Hide();
    }

    private void OnLaunched()
    {
        flameShown = false;
    }

    private void OnNewBest()
    {
        Show(BubbleKind.Star);
    }

    private void OnNearMissed()
    {
        Show(BubbleKind.Phew);
    }
}
