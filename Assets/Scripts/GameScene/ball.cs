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

    [Tooltip("Minimum time between speed-ups.")]
    public float collisionCooldown = 0.06f;

    [Tooltip("Extra guard to prevent rapid multi speed-ups.")]
    public float speedUpCooldown = 0.12f;

    [Tooltip("If false, only racket collisions speed up (walls won't).")]
    public bool increaseSpeedOnWalls = false;

    [Header("Effects")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    [Header("Scoring")]

    private Rigidbody2D rb;
    private Collider2D col2d;
    private SpriteRenderer sr;

    private Vector2 initialPosition;
    private Coroutine resetRoutine;

    private bool ignoreTriggers;
    private float lastGoalTime;
    private const float GOAL_COOLDOWN = 0.5f;

    private float lastCollisionTime;
    private int lastCollisionFrame;
    private int lastColliderId;
    private float lastSpeedUpTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col2d = GetComponent<Collider2D>();
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

    void OnDisable()
    {
        StopAllCoroutines();
        resetRoutine = null;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd < 0.001f) return;

        if (spd > maxSpeed)
            rb.linearVelocity = (v / spd) * maxSpeed;

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

    public void HardStopAndHide()
    {
        StopAllCoroutines();
        resetRoutine = null;

        ignoreTriggers = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (col2d != null) col2d.enabled = false;
        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);
    }

    private void StartResetSequence()
    {
        if (!isActiveAndEnabled) return;

        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }

        resetRoutine = StartCoroutine(ResetBallRoutine());
    }

    private IEnumerator ResetBallRoutine()
    {
        ignoreTriggers = true;
        lastGoalTime = Time.time;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
            rb.position = initialPosition;
        }
        else
        {
            transform.position = initialPosition;
        }

        if (col2d != null) col2d.enabled = false;
        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);

        yield return new WaitForSecondsRealtime(0.75f);

        if (scoreManager != null && scoreManager.IsGameOver)
            yield break;

        if (sr != null) sr.enabled = true;
        if (col2d != null) col2d.enabled = true;
        if (rb != null) rb.simulated = true;

        LaunchBall();

        yield return new WaitForSecondsRealtime(0.10f);
        ignoreTriggers = false;
    }

    private void LaunchBall()
    {
        if (rb == null) return;

        float angle = Random.Range(25f, 55f);
        float signX = Random.value < 0.5f ? -1f : 1f;
        float signY = Random.value < 0.5f ? -1f : 1f;

        Vector2 baseDir = signY > 0 ? Vector2.up : Vector2.down;
        Vector2 dir = Quaternion.Euler(0, 0, angle * signX) * baseDir;

        rb.linearVelocity = dir.normalized * launchSpeed;

        if (trailEffect != null)
            trailEffect.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (rb == null || c == null || c.collider == null) return;

        bool isRacket = c.collider.CompareTag("racket");
        bool isWall = c.collider.CompareTag("wall");
        if (!isRacket && !isWall) return;

        if (isWall && !increaseSpeedOnWalls)
        {
            PlayBounce();
            return;
        }

        int id = c.collider.GetInstanceID();

        if (Time.time - lastCollisionTime < collisionCooldown) return;
        if (Time.frameCount == lastCollisionFrame && id == lastColliderId) return;

        lastCollisionTime = Time.time;
        lastCollisionFrame = Time.frameCount;
        lastColliderId = id;

        PlayBounce();

        if (Time.time - lastSpeedUpTime < speedUpCooldown) return;
        lastSpeedUpTime = Time.time;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;

        float newSpeed = Mathf.Clamp(spd * (1f + speedIncreasePercent), launchSpeed, maxSpeed);
        if (spd > 0.001f)
            rb.linearVelocity = (v / spd) * newSpeed;
        else
            rb.linearVelocity = Vector2.up * launchSpeed;
    }

    private void PlayBounce()
    {
        if (bounceSound != null) bounceSound.Play();
    }

    private void OnTriggerEnter2D(Collider2D c)
    {
        if (c == null) return;
        if (ignoreTriggers) return;
        if (Time.time - lastGoalTime < GOAL_COOLDOWN) return;

        if (c.CompareTag("Goal1"))
        {
            ignoreTriggers = true;
            lastGoalTime = Time.time;

            bool ended = (scoreManager != null) && scoreManager.PlayerScored();
            if (!ended) StartResetSequence();
        }
        else if (c.CompareTag("Goal2"))
        {
            ignoreTriggers = true;
            lastGoalTime = Time.time;

            bool ended = (scoreManager != null) && scoreManager.EnemyScored();
            if (!ended) StartResetSequence();
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
