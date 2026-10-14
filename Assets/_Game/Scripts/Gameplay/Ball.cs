using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Ball : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float LaunchSpeed = 5f;
    [SerializeField] private float MaxSpeed = 12f;
    [SerializeField] private float SpeedIncreasePercent = 0.01f;

    [Header("Pace")]
    [SerializeField] private bool SteadyPace = true;
    [SerializeField] private float PaceStart = 10f;
    [SerializeField] private float PaceMax = 18f;
    [SerializeField] private float PaceIncreasePercent = 0.013f;
    [SerializeField] private float MinSideSpeed = 1.5f;
    [SerializeField] private float MaxSideSpeed = 5.5f;

    [Header("Stability")]
    [SerializeField, Range(0.05f, 0.95f)] private float MinVerticalDot = 0.40f;
    [SerializeField] private float WallBounceJitter = 3f;
    [SerializeField] private float CollisionSeparation = 0.05f;

    [Header("Serve")]
    [SerializeField] private Transform ServePoint;

    [Header("Refs")]
    [SerializeField, FormerlySerializedAs("Paddle")] private Paddle DefaultServer;

    public event Action Launched;
    public event Action PaddleHit;
    public event Action WallHit;
    public event Action Missed;
    public event Action RoundReset;
    public event Action Stopped;
    public event Action<Paddle, float> PaddleStruck;
    public event Action<Goal> GoalEntered;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private FieldLayout layout;

    private Paddle server;
    private Vector2? serveOrigin;
    private float serveGap = 1f;
    private float currentSpeed;
    private float speedBoost = 1f;
    private Vector2 lastVelocity;

    private bool roundActive;
    private bool waitingForServe;

    public MatchRandom Rng { get; set; }
    public MatchRandom ServeRng { get; set; }
    public Paddle Server => server;
    public float CurrentSpeed => currentSpeed;
    public bool Steady => SteadyPace && (layout == null || layout.Topology != FieldTopology.FourSides);
    public float SpeedBoost => speedBoost;
    public bool InPlay => roundActive && !waitingForServe && rb.simulated;
    public Vector2 LastVelocity => lastVelocity;
    public bool WaitingForServe => waitingForServe;
    public Vector2 ExitPoint { get; private set; }
    public Vector2 LastNormal { get; private set; } = Vector2.up;
    public Vector2 LastContact { get; private set; }
    public Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

    private float StartSpeed => Steady ? PaceStart : LaunchSpeed;
    private float TopSpeed => Steady ? PaceMax : MaxSpeed;
    private float Growth => Steady ? PaceIncreasePercent : SpeedIncreasePercent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        layout = FindFirstObjectByType<FieldLayout>();

        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        col.isTrigger = false;
        col.sharedMaterial = GetSharedNoBounce();

        if (DefaultServer == null) DefaultServer = FindFirstObjectByType<Paddle>();
        server = DefaultServer;
        if (Rng == null) Rng = new MatchRandom(Environment.TickCount);
    }

    private void Start()
    {
        rb.simulated = false;
        col.enabled = false;
        if (ServePoint != null && DefaultServer != null)
            serveGap = Mathf.Max(0.1f, ServePoint.position.y - DefaultServer.Home.y);
        SnapToServePoint();
    }

    private void Update()
    {
        if (!waitingForServe) return;

        if (server != null && server.HasActiveInput)
            Launch();
    }

    private void FixedUpdate()
    {
        if (!roundActive || waitingForServe || !rb.simulated) return;

        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= 0.0001f) return;

        lastVelocity = velocity;

        bool steady = Steady;
        float actualSpeed = steady ? Mathf.Abs(velocity.y) : velocity.magnitude;
        if (actualSpeed < currentSpeed * speedBoost * 0.95f)
            ApplyVelocity(steady ? SafeDirection(velocity) : velocity.normalized);
    }

    public void SetServer(Paddle paddle)
    {
        server = paddle;
    }

    public void ServeFrom(Paddle paddle)
    {
        server = paddle;
        serveOrigin = paddle != null ? paddle.Home + paddle.Inward * serveGap : (Vector2?)null;
    }

    public void ClearServeOrigin()
    {
        serveOrigin = null;
    }

    public void ServeToward(Vector2 origin, Vector2 direction)
    {
        roundActive = true;
        waitingForServe = false;
        currentSpeed = Mathf.Clamp(StartSpeed, 0.1f, TopSpeed);

        SnapTo(origin);
        SetBallVisible(true);
        RoundReset?.Invoke();

        rb.simulated = true;
        col.enabled = true;
        rb.WakeUp();

        Vector2 dir = SafeDirection(direction.sqrMagnitude > 0.0001f ? direction : Vector2.up);
        ApplyVelocity(dir);
        Launched?.Invoke();
    }

    public void Enter(Vector2 origin, Vector2 direction, float speed)
    {
        ServeToward(origin, direction);
        currentSpeed = Mathf.Clamp(speed, Mathf.Min(StartSpeed, TopSpeed), TopSpeed);
        ApplyVelocity(lastVelocity.normalized);
    }

    public void SetSpeedBoost(float boost)
    {
        speedBoost = Mathf.Max(0.1f, boost);
        if (rb != null && rb.simulated && lastVelocity.sqrMagnitude > 0.0001f) ApplyVelocity(lastVelocity.normalized);
    }

    public void LaunchNow()
    {
        if (waitingForServe) Launch();
    }

    public void ScaleSpeed(float factor)
    {
        currentSpeed = Mathf.Clamp(currentSpeed * factor, Mathf.Min(StartSpeed, TopSpeed), TopSpeed);
        if (rb.simulated && lastVelocity.sqrMagnitude > 0.0001f) ApplyVelocity(lastVelocity.normalized);
    }

    public void StartRound()
    {
        roundActive = true;
        currentSpeed = Mathf.Clamp(StartSpeed, 0.1f, TopSpeed);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        col.enabled = false;

        SnapToServePoint();
        SetBallVisible(true);

        waitingForServe = false;
        RoundReset?.Invoke();
    }

    public void EnableServe()
    {
        waitingForServe = true;
    }

    public void StopRound()
    {
        roundActive = false;
        waitingForServe = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        col.enabled = false;

        SetBallVisible(false);
        SnapToServePoint();
        Stopped?.Invoke();
    }

    public void StopRoundKeepVisible()
    {
        roundActive = false;
        waitingForServe = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        col.enabled = false;

        SnapToServePoint();
        RoundReset?.Invoke();
    }

    private void Launch()
    {
        waitingForServe = false;

        rb.simulated = true;
        col.enabled = true;
        rb.WakeUp();

        ApplyVelocity(GetLaunchDir());

        Launched?.Invoke();
    }

    private Vector2 GetLaunchDir()
    {
        var dice = ServeRng ?? Rng;
        float x = dice.Sign();
        float y = dice.Range(0.60f, 1.00f);
        Vector2 dir = Steady
            ? BallPace.ServeDirection(x, Mathf.InverseLerp(0.60f, 1.00f, y), currentSpeed * speedBoost, MaxSideSpeed)
            : new Vector2(x, y).normalized;

        if (Mathf.Abs(dir.y) < MinVerticalDot)
            dir = EnforceMinVertical(dir);

        FieldSide side = server != null ? server.Side : FieldSide.Bottom;
        return side.FromBottomFrame(dir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!roundActive || waitingForServe) return;

        Vector2 inVel = (lastVelocity.sqrMagnitude > 0.0001f)
            ? lastVelocity
            : rb.linearVelocity;

        if (inVel.sqrMagnitude < 0.0001f)
            inVel = Vector2.down;

        Vector2 normal = (collision.contactCount > 0)
            ? collision.GetContact(0).normal
            : Vector2.up;

        LastNormal = normal;
        LastContact = collision.contactCount > 0 ? collision.GetContact(0).point : rb.position;

        Vector2 outDir = Vector2.Reflect(inVel.normalized, normal).normalized;

        SeparateFromSurface(normal);

        var paddle = collision.collider.GetComponentInParent<Paddle>();
        if (paddle != null)
        {
            PaddleStruck?.Invoke(paddle, paddle.AxisOffset(rb.position));

            currentSpeed = Mathf.Min(currentSpeed * (1f + Growth), TopSpeed);

            Vector2 inward = paddle.Inward;
            float along = Vector2.Dot(outDir, inward);
            if (along < 0f)
                outDir -= 2f * along * inward;

            outDir = SafeDirection(outDir);
            ApplyVelocity(outDir);
            HapticManager.Light();
            PaddleHit?.Invoke();
            return;
        }

        if (WallBounceJitter > 0f)
        {
            float jitter = Rng.Range(-WallBounceJitter, WallBounceJitter);
            outDir = (Quaternion.Euler(0f, 0f, jitter) * outDir).normalized;
        }

        outDir = SafeDirection(outDir);
        ApplyVelocity(outDir);
        WallHit?.Invoke();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!roundActive) return;

        var goal = other.GetComponent<Goal>();
        if (goal == null || !goal.IsOpen) return;

        ExitPoint = rb.position;
        if (!goal.Portal)
        {
            Missed?.Invoke();
            HapticManager.Medium();
        }
        StopRound();
        GoalEntered?.Invoke(goal);
    }

    private void ApplyVelocity(Vector2 dir)
    {
        float pace = currentSpeed * speedBoost;
        lastVelocity = Steady ? BallPace.Velocity(dir, pace, MinSideSpeed, MaxSideSpeed) : dir * pace;
        rb.linearVelocity = lastVelocity;
    }

    private Vector2 SafeDirection(Vector2 dir)
    {
        dir = dir.normalized;
        if (Mathf.Abs(dir.y) < MinVerticalDot)
            dir = EnforceMinVertical(dir);
        return dir.normalized;
    }

    private Vector2 EnforceMinVertical(Vector2 dir)
    {
        dir = dir.normalized;

        float signY = (dir.y >= 0f) ? 1f : -1f;
        float y = MinVerticalDot * signY;

        float signX = (dir.x == 0f) ? Rng.Sign() : Mathf.Sign(dir.x);

        float x = signX * Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        return new Vector2(x, y).normalized;
    }

    private void SeparateFromSurface(Vector2 normal)
    {
        if (CollisionSeparation > 0f)
            rb.position += normal * CollisionSeparation;
    }

    public void SnapToServePoint()
    {
        if (serveOrigin.HasValue)
        {
            SnapTo(serveOrigin.Value);
            return;
        }

        SnapTo((ServePoint != null) ? (Vector2)ServePoint.position : Vector2.zero);
    }

    public void SnapTo(Vector2 pos)
    {
        rb.position = pos;
        transform.position = pos;
    }

    public void SetBallVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
    }

    private static PhysicsMaterial2D sharedNoBounce;
    public static PhysicsMaterial2D GetSharedNoBounce()
    {
        if (sharedNoBounce == null)
        {
            sharedNoBounce = new PhysicsMaterial2D("NoBounce");
            sharedNoBounce.bounciness = 0f;
            sharedNoBounce.friction = 0f;
        }
        return sharedNoBounce;
    }
}
