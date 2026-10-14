using UnityEngine;

[RequireComponent(typeof(Ball))]
public sealed class BallFeel : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform Visual;

    [Header("Squash")]
    [SerializeField, Range(0f, 0.5f)] private float PaddleSquash = 0.3f;
    [SerializeField, Range(0f, 0.5f)] private float WallSquash = 0.2f;
    [SerializeField] private float Frequency = 3.2f;
    [SerializeField] private float Damping = 14f;
    [SerializeField, Range(0f, 0.2f)] private float FlightStretch = 0.06f;

    [Header("Trail")]
    [SerializeField] private float TrailSeconds = 0.16f;
    [SerializeField, Range(0f, 1f)] private float TrailWidth = 0.75f;
    [SerializeField, Range(0f, 1f)] private float TrailAlpha = 0.45f;
    [SerializeField] private int TrailOrder = -5;

    [Header("Color")]
    [SerializeField] private Color Accent = new Color(1f, 0.42f, 0.34f, 1f);

    [Header("Heat")]
    [SerializeField] private Sprite Glow;
    [SerializeField] private Color HotColor = new Color(1f, 0.86f, 0.45f, 1f);
    [SerializeField, Range(0f, 1f)] private float HotShare = 0.6f;
    [SerializeField, Range(0f, 1f)] private float GlowAlpha = 0.8f;
    [SerializeField] private float GlowSize = 2.5f;
    [SerializeField] private float GlowGrowth = 0.6f;
    [SerializeField] private int GlowOrder = -6;
    [SerializeField, Range(0f, 2f)] private float TrailHeatBoost = 0.6f;
    [SerializeField] private float HeatFade = 1.5f;

    private const string GlowName = "Glow";
    private const float HeatStep = 0.02f;

    private static Material trailMaterial;

    private readonly Gradient paint = new Gradient();
    private readonly GradientColorKey[] colorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

    private Ball ball;
    private TrailRenderer trail;
    private SpriteRenderer glow;
    private float glowUnit = 1f;
    private float heat;
    private float paintedHeat = -1f;
    [SerializeField, HideInInspector] private Vector3 restScale = Vector3.one;
    [SerializeField, HideInInspector] private bool restKnown;

    private Vector2 squashed = Vector2.one;
    private float elapsed = -1f;

    public Color AccentColor => Accent;
    public bool TrailEmitting => trail != null && trail.emitting;
    public bool Animating => elapsed >= 0f;
    public float Heat => heat;
    public bool GlowVisible => glow != null && glow.enabled;
    public Color HeatColor => FeelMath.Warm(Accent, HotColor, heat * HotShare);
    public float TrailTime => trail != null ? trail.time : 0f;
    public Vector3 RestScale => restScale;
    public Transform VisualTransform => Visual;

    private void Awake()
    {
        ball = GetComponent<Ball>();
        if (Visual == null)
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) Visual = sprite.transform;
        }

        if (Visual != null)
        {
            if (restKnown) Visual.localScale = restScale;
            else restScale = Visual.localScale;
            restKnown = true;
        }

        BuildTrail();
        BuildGlow();
    }

    private void OnEnable()
    {
        ball.PaddleStruck += OnPaddleStruck;
        ball.WallHit += OnWallHit;
        ball.Launched += OnLaunched;
        ball.RoundReset += OnStopped;
        ball.Stopped += OnStopped;
    }

    private void OnDisable()
    {
        ball.PaddleStruck -= OnPaddleStruck;
        ball.WallHit -= OnWallHit;
        ball.Launched -= OnLaunched;
        ball.RoundReset -= OnStopped;
        ball.Stopped -= OnStopped;
        Rest();
        Cool();
    }

    private void Update()
    {
        if (Visual == null) return;

        float spring = 0f;
        if (elapsed >= 0f)
        {
            spring = FeelMath.Spring(elapsed, Frequency, Damping);
            elapsed = spring == 0f ? -1f : elapsed + FeelMath.Step(Time.unscaledDeltaTime);
        }

        Vector2 scale = FeelMath.Blend(squashed, spring);
        if (ball.InPlay && FlightStretch > 0f)
        {
            Vector2 velocity = ball.LastVelocity;
            float calm = 1f - Mathf.Min(1f, Mathf.Abs(spring) * 4f);
            float stretch = FlightStretch * calm;
            if (Mathf.Abs(velocity.y) >= Mathf.Abs(velocity.x)) scale += new Vector2(-stretch * 0.5f, stretch);
            else scale += new Vector2(stretch, -stretch * 0.5f);
        }

        Visual.localScale = new Vector3(restScale.x * scale.x, restScale.y * scale.y, restScale.z);
    }

    private void LateUpdate()
    {
        float target = ball.InPlay ? FeelMath.Warmth(ball.Heat) : 0f;
        heat = Mathf.MoveTowards(heat, target, HeatFade * FeelMath.Step(Time.unscaledDeltaTime));
        ShowHeat();
    }

    public void SetAccent(Color color)
    {
        color.a = 1f;
        Accent = color;
        PaintTrail();
        ShowHeat();
    }

    public void Bounce(Vector2 normal, bool paddle)
    {
        squashed = FeelMath.Squash(normal, paddle ? PaddleSquash : WallSquash);
        elapsed = 0f;
    }

    private void OnPaddleStruck(Paddle paddle, float offset)
    {
        Bounce(ball.LastNormal, true);

        if (paddle != null)
        {
            var kick = paddle.GetComponent<PaddleFeel>();
            if (kick != null) kick.Kick();
        }

        if (GameFeel.Instance != null) GameFeel.Instance.Impact(ball.LastContact, ball.LastNormal, Accent, true);
    }

    private void OnWallHit()
    {
        Bounce(ball.LastNormal, false);
        if (GameFeel.Instance != null) GameFeel.Instance.Impact(ball.LastContact, ball.LastNormal, Accent, false);
    }

    private void OnLaunched()
    {
        Rest();
        if (trail == null) return;

        trail.Clear();
        trail.emitting = true;
    }

    private void OnStopped()
    {
        Rest();
        Cool();
        if (trail == null) return;

        trail.emitting = false;
        trail.Clear();
    }

    private void Cool()
    {
        heat = 0f;
        ShowHeat();
    }

    private void ShowHeat()
    {
        if (glow != null)
        {
            bool lit = Glow != null && heat > 0.01f;
            glow.enabled = lit;
            if (lit)
            {
                var color = HeatColor;
                color.a = GlowAlpha * heat;
                glow.color = color;

                float size = glowUnit * (GlowSize + GlowGrowth * heat);
                glow.transform.localScale = new Vector3(size, size, 1f);
            }
        }

        if (Mathf.Abs(heat - paintedHeat) >= HeatStep || (heat == 0f && paintedHeat != 0f)) PaintTrail();
    }

    private void Rest()
    {
        elapsed = -1f;
        squashed = Vector2.one;
        if (Visual != null) Visual.localScale = restScale;
    }

    private void BuildTrail()
    {
        trail = GetComponentInChildren<TrailRenderer>(true);
        if (trail == null)
        {
            var holder = new GameObject("Trail");
            holder.transform.SetParent(transform, false);
            trail = holder.AddComponent<TrailRenderer>();
        }

        trail.time = TrailSeconds;
        trail.minVertexDistance = 0.04f;
        trail.numCapVertices = 6;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.autodestruct = false;
        trail.emitting = false;
        trail.sortingOrder = TrailOrder;
        trail.sharedMaterial = TrailMaterial();
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        trail.widthMultiplier = Diameter() * TrailWidth;
        PaintTrail();
    }

    private void BuildGlow()
    {
        var holder = transform.Find(GlowName);
        if (holder == null)
        {
            holder = new GameObject(GlowName).transform;
            holder.SetParent(transform, false);
        }

        glow = holder.GetComponent<SpriteRenderer>();
        if (glow == null) glow = holder.gameObject.AddComponent<SpriteRenderer>();

        glow.sprite = Glow;
        glow.sortingOrder = GlowOrder;
        glow.enabled = false;

        float width = Glow != null ? Glow.bounds.size.x : 0f;
        float parent = Mathf.Abs(transform.lossyScale.x);
        glowUnit = width > 0.0001f && parent > 0.0001f ? Diameter() / (width * parent) : 1f;
    }

    private void PaintTrail()
    {
        if (trail == null) return;

        paintedHeat = heat;
        Color color = HeatColor;
        colorKeys[0] = new GradientColorKey(color, 0f);
        colorKeys[1] = new GradientColorKey(Accent, 1f);
        alphaKeys[0] = new GradientAlphaKey(Mathf.Clamp01(TrailAlpha * (1f + 0.5f * heat)), 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        paint.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = paint;
        trail.time = TrailSeconds * (1f + TrailHeatBoost * heat);
    }

    private float Diameter()
    {
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null) return circle.radius * 2f * Mathf.Abs(transform.lossyScale.x);

        var body = GetComponent<Collider2D>();
        return body != null ? body.bounds.size.x : 0.4f;
    }

    private static Material TrailMaterial()
    {
        if (trailMaterial == null) trailMaterial = new Material(Shader.Find("Sprites/Default")) { name = "BallTrail" };
        return trailMaterial;
    }
}
