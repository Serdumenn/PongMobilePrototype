using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class SoloBall : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float LaunchSpeed = 5f;
    [SerializeField] private float MaxSpeed = 12f;
    [SerializeField] private float SpeedIncreasePercent = 0.01f;

    [Header("Stability")]
    [SerializeField, Range(0.05f, 0.95f)] private float MinVerticalDot = 0.40f;
    [SerializeField] private float WallBounceJitter = 3f;
    [SerializeField] private float CollisionSeparation = 0.05f;

    [Header("Serve")]
    [SerializeField] private Transform ServePoint;

    [Header("Refs")]
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private SoloGameManager Game;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    private float currentSpeed;
    private Vector2 lastVelocity;

    private bool roundActive;
    private bool waitingForServe;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        col.isTrigger = false;

        var noBounce = new PhysicsMaterial2D("NoBounce");
        noBounce.bounciness = 0f;
        noBounce.friction = 0f;
        col.sharedMaterial = noBounce;

        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
    }

    private void Start()
    {
        SetBallVisible(false);
        rb.simulated = false;
        col.enabled = false;
        SnapToServePoint();
    }

    private void Update()
    {
        if (!waitingForServe) return;

        bool tapped = Input.touchCount > 0 || Input.GetMouseButtonDown(0);
        if (tapped) Launch();
    }

    private void FixedUpdate()
    {
        if (!roundActive || waitingForServe || !rb.simulated) return;

        if (rb.linearVelocity.sqrMagnitude > 0.0001f)
            lastVelocity = rb.linearVelocity;
    }

    public void StartRound()
    {
        roundActive = true;
        currentSpeed = Mathf.Clamp(LaunchSpeed, 0.1f, MaxSpeed);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
        col.enabled = false;

        SnapToServePoint();
        SetBallVisible(true);

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
    }

    private void Launch()
    {
        waitingForServe = false;

        rb.simulated = true;
        col.enabled = true;
        rb.WakeUp();

        Vector2 dir = GetUpwardLaunchDir();
        lastVelocity = dir * currentSpeed;
        rb.linearVelocity = lastVelocity;
    }

    private Vector2 GetUpwardLaunchDir()
    {
        float x = (Random.value < 0.5f) ? -1f : 1f;
        float y = Random.Range(0.60f, 1.00f);
        Vector2 dir = new Vector2(x, y).normalized;

        if (Mathf.Abs(dir.y) < MinVerticalDot)
            dir = EnforceMinVertical(dir);

        return dir;
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

        Vector2 outDir = Vector2.Reflect(inVel.normalized, normal).normalized;

        SeparateFromSurface(normal);

        bool hitPaddle = collision.collider.GetComponentInParent<RacketController>() != null;
        if (hitPaddle)
        {
            Score?.AddPoint();
            currentSpeed = Mathf.Min(currentSpeed * (1f + SpeedIncreasePercent), MaxSpeed);

            if (outDir.y < 0f)
                outDir.y = Mathf.Abs(outDir.y);

            outDir = SafeDirection(outDir);
            ApplyVelocity(outDir);
            return;
        }

        if (WallBounceJitter > 0f)
        {
            float jitter = Random.Range(-WallBounceJitter, WallBounceJitter);
            outDir = (Quaternion.Euler(0f, 0f, jitter) * outDir).normalized;
        }

        outDir = SafeDirection(outDir);
        ApplyVelocity(outDir);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!roundActive) return;

        if (other.CompareTag("bottom"))
        {
            Score?.GameOver();
            Game?.GameOver();
            StopRound();
        }
    }

    private void ApplyVelocity(Vector2 dir)
    {
        lastVelocity = dir * currentSpeed;
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

        float signX = (dir.x == 0f)
            ? ((Random.value < 0.5f) ? -1f : 1f)
            : Mathf.Sign(dir.x);

        float x = signX * Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        return new Vector2(x, y).normalized;
    }

    private void SeparateFromSurface(Vector2 normal)
    {
        if (CollisionSeparation > 0f)
            rb.position += normal * CollisionSeparation;
    }

    private void SnapToServePoint()
    {
        Vector2 pos = (ServePoint != null) ? (Vector2)ServePoint.position : Vector2.zero;
        rb.position = pos;
        transform.position = pos;
    }

    private void SetBallVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
    }
}