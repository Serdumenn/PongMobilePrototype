using System.Collections;
using UnityEngine;

public class ball : MonoBehaviour
{
    [Header("Launch")]
    public float launchSpeed = 8f;
    public float maxSpeed = 16f;

    [Tooltip("Percent speed increase per collision (0.02 = +2%).")]
    [Range(0f, 0.2f)]
    public float speedIncreasePercent = 0.02f;

    [Header("Stability")]
    [Tooltip("Minimum |y| component for direction (prevents near-horizontal lock).")]
    [Range(0.05f, 0.6f)]
    public float minVerticalDot = 0.20f;

    [Tooltip("Seconds. Prevents multi-hit spam causing sudden speed spikes.")]
    public float collisionCooldown = 0.06f;

    [Header("Effects")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    [Header("Scoring")]
    public ScoreManager scoreManager;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 initialPosition;
    private Coroutine resetRoutine;
    private bool waitingForLaunch;

    private float lastCollisionTime;
    private int lastCollisionFrame;
    private int lastColliderId;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        initialPosition = rb != null ? rb.position : (Vector2)transform.position;

        if (scoreManager == null)
            scoreManager = FindAny<ScoreManager>();
    }

    void Start()
    {
        StartResetSequence();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd < 0.001f) return;

        // Clamp max speed
        if (spd > maxSpeed)
            rb.linearVelocity = (v / spd) * maxSpeed;

        // Prevent near-horizontal lock
        Vector2 dir = rb.linearVelocity.normalized;
        if (Mathf.Abs(dir.y) < minVerticalDot)
        {
            float signY = dir.y >= 0 ? 1f : -1f;
            float signX = dir.x >= 0 ? 1f : -1f;

            float clampedY = minVerticalDot * signY;
            float clampedX = Mathf.Sqrt(Mathf.Clamp01(1f - clampedY * clampedY)) * signX;

            rb.linearVelocity = new Vector2(clampedX, clampedY) * spd;
        }
    }

    private void StartResetSequence()
    {
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
        resetRoutine = StartCoroutine(ResetBallRoutine());
    }

    private IEnumerator ResetBallRoutine()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (rb != null) rb.position = initialPosition;
        else transform.position = initialPosition;

        waitingForLaunch = true;

        if (sr) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);

        yield return new WaitForSeconds(0.75f);

        if (sr) sr.enabled = true;

        GameSettings.ForceReload();

        if (GameSettings.CurrentStartMode == StartMode.AutoAfterCountdown)
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

    private void LaunchBall()
    {
        float angle = Random.Range(25f, 55f);
        float signX = Random.value < 0.5f ? -1f : 1f;
        float signY = Random.value < 0.5f ? -1f : 1f;

        Vector2 baseDir = signY > 0 ? Vector2.up : Vector2.down;
        Vector2 dir = Quaternion.Euler(0, 0, angle * signX) * baseDir;

        if (rb != null)
            rb.linearVelocity = dir.normalized * launchSpeed;

        if (trailEffect != null)
            trailEffect.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (rb == null || col == null || col.collider == null) return;

        // tag filter
        string tag = col.collider.tag;
        if (tag != "racket" && tag != "wall")
            return;

        // spam guard
        int id = col.collider.GetInstanceID();
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        if (Time.frameCount == lastCollisionFrame && id == lastColliderId)
            return;

        lastCollisionTime = Time.time;
        lastCollisionFrame = Time.frameCount;
        lastColliderId = id;

        if (bounceSound != null)
            bounceSound.Play();

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;

        float newSpeed = Mathf.Clamp(spd * (1f + speedIncreasePercent), launchSpeed, maxSpeed);
        if (spd > 0.001f)
            rb.linearVelocity = (v / spd) * newSpeed;
        else
            rb.linearVelocity = Vector2.up * launchSpeed;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;

        // Cursor raporundaki gibi:
        // Goal1 = top/enemy side -> Player scores
        // Goal2 = bottom/player side -> Enemy scores
        if (col.CompareTag("Goal1"))
        {
            if (scoreManager != null) scoreManager.PlayerScored();
            StartResetSequence();
        }
        else if (col.CompareTag("Goal2"))
        {
            if (scoreManager != null) scoreManager.EnemyScored();
            StartResetSequence();
        }
    }

    private static T FindAny<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }
}