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

    private static Material trailMaterial;

    private Ball ball;
    private TrailRenderer trail;
    [SerializeField, HideInInspector] private Vector3 restScale = Vector3.one;
    [SerializeField, HideInInspector] private bool restKnown;

    private Vector2 squashed = Vector2.one;
    private float elapsed = -1f;

    public Color AccentColor => Accent;
    public bool TrailEmitting => trail != null && trail.emitting;
    public bool Animating => elapsed >= 0f;
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

    public void SetAccent(Color color)
    {
        color.a = 1f;
        Accent = color;
        PaintTrail();
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
        if (trail == null) return;

        trail.emitting = false;
        trail.Clear();
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

    private void PaintTrail()
    {
        if (trail == null) return;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Accent, 0f), new GradientColorKey(Accent, 1f) },
            new[] { new GradientAlphaKey(TrailAlpha, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
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
