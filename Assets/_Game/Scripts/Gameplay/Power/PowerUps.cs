using System;
using UnityEngine;

public sealed class PowerUps : MonoBehaviour
{
    private const int SeedSalt = 0x3C6EF372;
    private const float PopSeconds = 0.25f;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private GameFeel Feel;

    [Header("Art")]
    [SerializeField] private Sprite Token;
    [SerializeField] private Sprite[] Icons = new Sprite[PowerSchedule.KindCount];
    [SerializeField] private Color[] Colors =
    {
        new Color(0.18f, 0.77f, 0.71f, 1f),
        new Color(0.31f, 0.66f, 1f, 1f),
        new Color(1f, 0.77f, 0.2f, 1f),
        new Color(0.61f, 0.42f, 1f, 1f)
    };
    [SerializeField] private Sprite Bar;

    [Header("Timing")]
    [SerializeField] private float FirstAt = 8f;
    [SerializeField] private float GapMin = 10f;
    [SerializeField] private float GapMax = 14f;
    [SerializeField] private float TokenLife = 7f;
    [SerializeField] private float BlinkFrom = 1.5f;

    [Header("Effects")]
    [SerializeField] private float BigSeconds = 8f;
    [SerializeField] private float BigScale = 1.5f;
    [SerializeField] private float SlowSeconds = 6f;
    [SerializeField, Range(0.3f, 1f)] private float SlowBoost = 0.72f;
    [SerializeField] private float DoubleSeconds = 8f;
    [SerializeField] private float ShieldSeconds = 12f;

    [Header("Layout")]
    [SerializeField] private float TokenSize = 0.66f;
    [SerializeField] private float EdgeMargin = 0.7f;
    [SerializeField] private float TopMargin = 1.6f;
    [SerializeField] private float ShieldGap = 0.55f;
    [SerializeField] private float ShieldThickness = 0.14f;
    [SerializeField] private int Order = 4;

    public event Action<PowerKind> Collected;
    public event Action<PowerKind> Ended;

    private readonly float[] remaining = new float[PowerSchedule.KindCount];
    private PowerSchedule schedule;
    private Transform token;
    private SpriteRenderer tokenBack;
    private SpriteRenderer tokenIcon;
    private Transform shield;
    private SpriteRenderer shieldView;
    private BoxCollider2D shieldBody;
    private SoloGameManager.GameState lastState;
    private PowerKind tokenKind;
    private float tokenAge;
    private float runTime;
    private bool launched;
    private bool running;

    public bool Running => running;
    public bool TokenShown => token != null && token.gameObject.activeSelf;
    public PowerKind TokenKind => tokenKind;
    public Vector2 TokenPosition => token != null ? (Vector2)token.position : Vector2.zero;
    public bool ShieldUp => shield != null && shield.gameObject.activeSelf;
    public float ShieldLine => shield != null ? shield.position.y : 0f;

    private Ball MainBall => Game != null ? Game.Ball : null;
    private Paddle PlayerPaddle => Game != null && Game.Player != null ? Game.Player.Paddle : null;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();
        BuildToken();
        BuildShield();
    }

    private void OnEnable()
    {
        if (Game != null) Game.StateChanged += OnStateChanged;
        if (MainBall != null) MainBall.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        if (Game != null) Game.StateChanged -= OnStateChanged;
        if (MainBall != null) MainBall.Launched -= OnLaunched;
        StopRun();
    }

    public bool IsActive(PowerKind kind)
    {
        return remaining[(int)kind] > 0f;
    }

    public float Remaining(PowerKind kind)
    {
        return remaining[(int)kind];
    }

    public float Fraction(PowerKind kind)
    {
        float duration = Duration(kind);
        return duration > 0f ? Mathf.Clamp01(remaining[(int)kind] / duration) : 0f;
    }

    public float Duration(PowerKind kind)
    {
        return kind switch
        {
            PowerKind.BigPaddle => BigSeconds,
            PowerKind.SlowBall => SlowSeconds,
            PowerKind.DoublePoints => DoubleSeconds,
            _ => ShieldSeconds
        };
    }

    public Color ColorOf(PowerKind kind)
    {
        int index = (int)kind;
        return Colors != null && index < Colors.Length ? Colors[index] : Color.white;
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused) StartRun();
        else if (state == SoloGameManager.GameState.Menu || state == SoloGameManager.GameState.GameOver) StopRun();
        lastState = state;
    }

    private void StartRun()
    {
        EndAll();
        var mode = Game.RunMode;
        running = mode != null && mode.Kind == GameModeKind.Rush && !Game.BattleRun;
        schedule = new PowerSchedule(unchecked(Game.RunSeed ^ SeedSalt), FirstAt, GapMin, GapMax);
        runTime = 0f;
        launched = false;
    }

    private void StopRun()
    {
        EndAll();
        running = false;
    }

    private void OnLaunched()
    {
        if (running) launched = true;
    }

    private void Update()
    {
        if (!running || Game == null || Game.State != SoloGameManager.GameState.Playing) return;

        float step = Time.deltaTime;
        if (launched) runTime += step;

        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] <= 0f) continue;
            remaining[i] -= step;
            if (remaining[i] <= 0f) End((PowerKind)i);
        }

        if (TokenShown) AnimateToken(step);
        else if (launched && schedule != null && runTime >= schedule.NextAt)
        {
            Spawn(schedule.TakeKind(), schedule.TakeSpot(Area()));
            schedule.Advance();
        }
    }

    public Rect Area()
    {
        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        float xMin = field.xMin + EdgeMargin;
        float xMax = Mathf.Max(xMin, field.xMax - EdgeMargin);
        float yMin = field.center.y - 0.3f;
        float yMax = Mathf.Max(yMin, field.yMax - TopMargin);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    public void Spawn(PowerKind kind, Vector2 at)
    {
        if (token == null) return;

        tokenKind = kind;
        tokenAge = 0f;
        token.position = new Vector3(at.x, at.y, 0f);
        int index = (int)kind;
        tokenIcon.sprite = Icons != null && index < Icons.Length ? Icons[index] : null;
        tokenBack.color = ColorOf(kind);
        tokenIcon.color = Color.white;
        token.gameObject.SetActive(true);
        AnimateToken(0f);
    }

    public void Touched(Collider2D other)
    {
        if (other == null || MainBall == null || other.GetComponentInParent<Ball>() != MainBall) return;
        Collect();
    }

    public void Collect()
    {
        if (!TokenShown) return;

        var kind = tokenKind;
        Vector2 at = token.position;
        HideToken();
        Activate(kind);

        if (Feel != null)
        {
            Feel.Flash(at);
            Feel.Impact(at, Vector2.up, ColorOf(kind), false);
        }

        Collected?.Invoke(kind);
    }

    public void Activate(PowerKind kind)
    {
        bool wasActive = IsActive(kind);
        remaining[(int)kind] = Duration(kind);
        if (!wasActive) Apply(kind, true);
    }

    public void ShieldHit()
    {
        if (IsActive(PowerKind.Shield)) End(PowerKind.Shield);
    }

    public void EndAll()
    {
        for (int i = 0; i < remaining.Length; i++)
            if (remaining[i] > 0f) End((PowerKind)i);
        HideToken();
    }

    private void End(PowerKind kind)
    {
        remaining[(int)kind] = 0f;
        Apply(kind, false);
        Ended?.Invoke(kind);
    }

    private void Apply(PowerKind kind, bool on)
    {
        switch (kind)
        {
            case PowerKind.BigPaddle:
                if (PlayerPaddle != null) PlayerPaddle.SetLengthScale(on ? BigScale : 1f);
                break;
            case PowerKind.SlowBall:
                if (MainBall != null) MainBall.SetSpeedBoost(on ? SlowBoost : 1f);
                break;
            case PowerKind.DoublePoints:
                if (Game != null) Game.PointMultiplier = on ? 2 : 1;
                break;
            default:
                if (on) PlaceShield();
                if (shield != null) shield.gameObject.SetActive(on);
                break;
        }
    }

    private void AnimateToken(float step)
    {
        tokenAge += step;
        if (tokenAge >= TokenLife)
        {
            HideToken();
            return;
        }

        float grow = FeelMath.Overshoot(tokenAge / PopSeconds);
        float pulse = 1f + 0.06f * Mathf.Sin(tokenAge * 5f);
        float size = TokenSize * Mathf.Max(0.05f, grow) * pulse;
        float unit = Token != null && Token.bounds.size.x > 0.0001f ? size / Token.bounds.size.x : size;
        tokenBack.transform.localScale = new Vector3(unit, unit, 1f);
        tokenBack.transform.localPosition = new Vector3(0f, Mathf.Sin(tokenAge * 3f) * 0.05f, 0f);

        var icon = tokenIcon.sprite;
        float iconUnit = icon != null && icon.bounds.size.x > 0.0001f ? size * 0.5f / icon.bounds.size.x : size;
        tokenIcon.transform.localScale = new Vector3(iconUnit, iconUnit, 1f);
        tokenIcon.transform.localPosition = tokenBack.transform.localPosition;

        bool blink = tokenAge > TokenLife - BlinkFrom && Mathf.Repeat(tokenAge * 6f, 1f) < 0.35f;
        float alpha = blink ? 0.35f : 1f;
        var back = tokenBack.color;
        back.a = alpha;
        tokenBack.color = back;
        tokenIcon.color = new Color(1f, 1f, 1f, alpha);
    }

    private void HideToken()
    {
        if (token != null) token.gameObject.SetActive(false);
    }

    private void PlaceShield()
    {
        if (shield == null) return;

        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        float y = PlayerPaddle != null ? PlayerPaddle.Home.y - ShieldGap : field.yMin + 0.4f;
        shield.position = new Vector3(field.center.x, y, 0f);
        shieldBody.size = new Vector2(field.width, ShieldThickness);

        if (Bar != null && Bar.bounds.size.x > 0.0001f)
            shieldView.transform.localScale = new Vector3(field.width / Bar.bounds.size.x, ShieldThickness * 2.2f / Bar.bounds.size.y, 1f);

        var color = ColorOf(PowerKind.Shield);
        color.a = 0.85f;
        shieldView.color = color;
    }

    private void BuildToken()
    {
        token = new GameObject("PowerToken").transform;
        token.SetParent(transform, false);
        var trigger = token.gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = TokenSize * 0.5f;
        token.gameObject.AddComponent<PowerToken>().Owner = this;

        tokenBack = new GameObject("Back").AddComponent<SpriteRenderer>();
        tokenBack.transform.SetParent(token, false);
        tokenBack.sprite = Token;
        tokenBack.sortingOrder = Order;

        tokenIcon = new GameObject("Icon").AddComponent<SpriteRenderer>();
        tokenIcon.transform.SetParent(token, false);
        tokenIcon.sortingOrder = Order + 1;

        token.gameObject.SetActive(false);
    }

    private void BuildShield()
    {
        shield = new GameObject("ShieldWall").transform;
        shield.SetParent(transform, false);
        shieldBody = shield.gameObject.AddComponent<BoxCollider2D>();
        shield.gameObject.AddComponent<ShieldWall>().Owner = this;

        shieldView = new GameObject("View").AddComponent<SpriteRenderer>();
        shieldView.transform.SetParent(shield, false);
        shieldView.sprite = Bar;
        shieldView.sortingOrder = Order;

        shield.gameObject.SetActive(false);
    }
}
