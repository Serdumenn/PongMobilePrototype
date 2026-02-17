using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class SoloBall : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] private float LaunchSpeed = 8f;
    [SerializeField] private float MaxSpeed = 16f;
    [SerializeField] private float SpeedIncreasePercent = 0.02f; // ONLY when hitting objects tagged "wall"
    [SerializeField] private float ResetDelay = 0.15f;

    [Header("Stability")]
    [SerializeField, Range(0.05f, 0.95f)] private float MinVerticalDot = 0.30f; // prevents near-horizontal loops

    [Header("Serve Point (IMPORTANT)")]
    [SerializeField] private Transform ServePoint;

    [Header("Refs")]
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private SoloGameManager Game;

    [Header("Control")]
    [SerializeField] private bool AutoStart = false;

    private Rigidbody2D rb;
    private Collider2D col;

    private float currentSpeed;
    private bool roundActive;

    private Vector2 lastVelocity; // for clean reflection (independent from paddle movement)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Physics baseline for Pong feel
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Ball should be a solid collider (bottom should be trigger)
        col.isTrigger = false;

        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
    }

    private void Start()
    {
        rb.simulated = false;
        col.enabled = false;

        // put ball to serve point at boot
        SnapToServePoint();

        if (AutoStart)
            StartRound();
    }

    private void FixedUpdate()
    {
        if (!roundActive || !rb.simulated) return;

        lastVelocity = rb.linearVelocity;

        // Keep magnitude stable (paddle should not change speed)
        float speed = lastVelocity.magnitude;
        if (speed < 0.001f)
        {
            // dead ball failsafe
            rb.linearVelocity = GetUpwardLaunchDir() * currentSpeed;
            return;
        }

        if (Mathf.Abs(speed - currentSpeed) > 0.05f)
            rb.linearVelocity = (lastVelocity / speed) * currentSpeed;
    }

    public void StartRound()
    {
        StopAllCoroutines();

        roundActive = true;
        currentSpeed = Mathf.Clamp(LaunchSpeed, 0.1f, MaxSpeed);

        rb.simulated = true;
        col.enabled = true;

        SnapToServePoint();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        StartCoroutine(CoLaunch());
    }

    public void OnGameOver()
    {
        roundActive = false;
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.simulated = false;
        col.enabled = false;

        SnapToServePoint();
    }

    private IEnumerator CoLaunch()
    {
        if (ResetDelay > 0f)
            yield return new WaitForSeconds(ResetDelay);

        if (!roundActive) yield break;

        rb.WakeUp();
        rb.linearVelocity = GetUpwardLaunchDir() * currentSpeed;
    }

    private Vector2 GetUpwardLaunchDir()
    {
        // Always upward at start. Never downward.
        float x = (Random.value < 0.5f) ? -1f : 1f;
        float y = Random.Range(0.60f, 1.00f); // guaranteed up
        Vector2 dir = new Vector2(x, y).normalized;

        // ensure not too horizontal
        if (Mathf.Abs(Vector2.Dot(dir, Vector2.up)) < MinVerticalDot)
            dir = EnforceMinVertical(dir);

        return dir;
    }

    private Vector2 EnforceMinVertical(Vector2 dirNormalized)
    {
        dirNormalized = dirNormalized.normalized;

        float signY = (dirNormalized.y >= 0f) ? 1f : -1f;
        float y = MinVerticalDot * signY;

        float signX = (dirNormalized.x == 0f)
            ? ((Random.value < 0.5f) ? -1f : 1f)
            : Mathf.Sign(dirNormalized.x);

        float x = signX * Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        return new Vector2(x, y).normalized;
    }

    private void SnapToServePoint()
    {
        if (ServePoint != null)
            rb.position = ServePoint.position;
        else
            rb.position = Vector2.zero;

        transform.position = rb.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!roundActive) return;

        // Treat "paddle" by component presence (tag bağımlılığı yok)
        bool hitPaddle = collision.collider.GetComponentInParent<RacketController>() != null;

        // Reflect using contact normal & last velocity (paddle movement won't affect speed/direction)
        Vector2 inVel = (lastVelocity.sqrMagnitude > 0.0001f) ? lastVelocity : rb.linearVelocity;
        Vector2 normal = (collision.contactCount > 0) ? collision.GetContact(0).normal : Vector2.up;

        Vector2 outDir = Vector2.Reflect(inVel.normalized, normal).normalized;

        if (hitPaddle)
        {
            // Score +1 each paddle hit
            Score?.AddPoint();

            // After paddle bounce, MUST go upward
            if (outDir.y < 0f) outDir.y = Mathf.Abs(outDir.y);

            // Keep from being too horizontal
            if (Mathf.Abs(Vector2.Dot(outDir, Vector2.up)) < MinVerticalDot)
                outDir = EnforceMinVertical(outDir);

            rb.linearVelocity = outDir * currentSpeed;
            return;
        }

        // Walls tagged "wall" gradually increase speed (only here!)
        if (collision.collider.CompareTag("wall"))
            currentSpeed = Mathf.Min(currentSpeed * (1f + SpeedIncreasePercent), MaxSpeed);

        // Keep from being too horizontal (applies to roof/side walls too)
        if (Mathf.Abs(Vector2.Dot(outDir, Vector2.up)) < MinVerticalDot)
            outDir = EnforceMinVertical(outDir);

        rb.linearVelocity = outDir * currentSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!roundActive) return;

        if (other.CompareTag("bottom"))
        {
            // GameOver flow is owned by GameManager
            Game?.GameOver();
        }
    }
}