using System.Collections;
using UnityEngine;

public class ball : MonoBehaviour
{
    [Header("Launch")]
    public float launchSpeed = 8f;
    public float maxSpeed = 16f;

    [Tooltip("Percent speed increase per collision (e.g. 0.02 = +2%).")]
    [Range(0f, 0.2f)]
    public float speedIncreasePercent = 0.02f;

    [Header("Anti-Horizontal Lock")]
    [Tooltip("Minimum vertical component ratio (0-1). If abs(dir.y) drops below this, we nudge it up.")]
    [Range(0.05f, 0.8f)]
    public float minVerticalDot = 0.2f;

    [Header("FX")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 spawnPos;
    private Coroutine resetRoutine;
    private bool waitingForLaunch;

    private float lastNonZeroYSign = 1f;

    void Awake()
    {
        GameSettings.EnsureLoaded(GameSettings.DefaultDifficultyError, GameSettings.DefaultDiffNeed, GameSettings.DefaultStartMode);

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        spawnPos = transform.position;
    }

    void Start()
    {
        StartResetSequence();
    }

    void StartResetSequence()
    {
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
        resetRoutine = StartCoroutine(ResetAndLaunchFlow());
    }

    IEnumerator ResetAndLaunchFlow()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        transform.position = spawnPos;

        waitingForLaunch = true;

        if (trailEffect != null) trailEffect.SetActive(false);
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(1.0f);

        if (sr != null) sr.enabled = true;

        bool autoLaunch = false;
        try
        {
            autoLaunch = (GameSettings.CurrentStartMode == StartMode.AutoAfterCountdown);
        }
        catch
        {
            autoLaunch = false;
        }

        if (autoLaunch)
        {
            LaunchBall();
            waitingForLaunch = false;
            yield break;
        }

        while (waitingForLaunch)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                LaunchBall();
                waitingForLaunch = false;
            }
            yield return null;
        }
    }

    void LaunchBall()
    {
        if (rb == null) return;

        float angle = Random.Range(30f, 60f);
        float leftRight = Random.value < 0.5f ? -1f : 1f;

        Vector2 dir = Quaternion.Euler(0f, 0f, angle * leftRight) * Vector2.up;
        rb.linearVelocity = dir.normalized * launchSpeed;

        lastNonZeroYSign = Mathf.Sign(dir.y) == 0 ? lastNonZeroYSign : Mathf.Sign(dir.y);

        if (trailEffect != null) trailEffect.SetActive(true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb == null) return;

        if (bounceSound != null) bounceSound.Play();

        Vector2 v = rb.linearVelocity;

        if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || v.sqrMagnitude < 0.0001f)
        {
            v = Vector2.up * launchSpeed;
        }

        float speed = Mathf.Clamp(v.magnitude * (1f + speedIncreasePercent), launchSpeed, maxSpeed);
        v = v.normalized * speed;

        v = EnforceMinVertical(v);

        rb.linearVelocity = v;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Goal1") || col.CompareTag("Goal2"))
        {
            StartResetSequence();
        }
    }

    Vector2 EnforceMinVertical(Vector2 velocity)
    {
        float speed = velocity.magnitude;
        if (speed <= Mathf.Epsilon) return Vector2.up * launchSpeed;

        Vector2 dir = velocity / speed;

        float absY = Mathf.Abs(dir.y);
        if (absY >= minVerticalDot)
        {
            if (Mathf.Abs(dir.y) > 0.0001f) lastNonZeroYSign = Mathf.Sign(dir.y);
            return velocity;
        }

        float signY = (Mathf.Abs(dir.y) > 0.0001f) ? Mathf.Sign(dir.y) : lastNonZeroYSign;

        float signX = (Mathf.Abs(dir.x) > 0.0001f) ? Mathf.Sign(dir.x) : (Random.value < 0.5f ? -1f : 1f);

        float clampedY = minVerticalDot * signY;
        float clampedX = Mathf.Sqrt(Mathf.Clamp01(1f - clampedY * clampedY)) * signX;

        lastNonZeroYSign = signY;

        return new Vector2(clampedX, clampedY) * speed;
    }
}