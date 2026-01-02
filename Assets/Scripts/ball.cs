using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Launch")]
    [Min(0.1f)] public float launchSpeed = 8f;
    [Min(0.1f)] public float maxSpeed = 16f;

    [Tooltip("0.02 = %2 hız artışı (her geçerli çarpışmada).")]
    [Range(0f, 0.2f)] public float speedIncreasePercent = 0.02f;

    [Header("Stability")]
    [Tooltip("Y bileşeni bu değerin altına düşerse düzeltilir (yatay kilidi önler).")]
    [Range(0.05f, 0.6f)] public float minVerticalDot = 0.2f;

    [Tooltip("Aynı anda çoklu çarpışma spam'ini engeller (hız spike/jitter için kritik).")]
    [Range(0f, 0.2f)] public float collisionCooldown = 0.06f;

    [Header("FX")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    [Header("Scoring")]
    public ScoreManager scoreManager;

    [Header("Start Mode")]
    public StartMode startMode = StartMode.TapToLaunch;

    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Vector2 initialPos;
    private Coroutine resetRoutine;
    private bool waitingForLaunch;

    private float lastCollisionTime;
    private int lastCollisionFrame;
    private int lastColliderId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        initialPos = rb != null ? rb.position : (Vector2)transform.position;

        if (scoreManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            scoreManager = Object.FindAnyObjectByType<ScoreManager>();
#else
            scoreManager = Object.FindObjectOfType<ScoreManager>();
#endif
        }

        TryLoadSettings();
    }

    private void Start()
    {
        StartResetSequence();
    }

    private void TryLoadSettings()
    {
        try
        {
            GameSettings.EnsureLoaded();
            startMode = GameSettings.CurrentStartMode;
        }
        catch { /* GameSettings*/ }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd < 0.0001f) return;

        if (spd > maxSpeed)
        {
            rb.linearVelocity = (v / spd) * maxSpeed;
            v = rb.linearVelocity;
            spd = maxSpeed;
        }

        Vector2 dir = v / spd;
        if (Mathf.Abs(dir.y) < minVerticalDot)
        {
            float signY = dir.y >= 0 ? 1f : -1f;
            float signX = dir.x >= 0 ? 1f : -1f;

            float clampedY = minVerticalDot * signY;
            float remaining = Mathf.Sqrt(Mathf.Clamp01(1f - clampedY * clampedY));
            float clampedX = remaining * signX;

            rb.linearVelocity = new Vector2(clampedX, clampedY) * spd;
        }
    }

    public void StartResetSequence()
    {
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
        resetRoutine = StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (rb != null) rb.position = initialPos;
        else transform.position = initialPos;

        waitingForLaunch = true;

        if (sr) sr.enabled = false;
        if (trailEffect) trailEffect.SetActive(false);

        yield return new WaitForSeconds(0.75f);

        if (sr) sr.enabled = true;

        if (startMode == StartMode.AutoAfterCountdown)
        {
            Launch();
            waitingForLaunch = false;
            yield break;
        }

        while (waitingForLaunch)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                Launch();
                waitingForLaunch = false;
            }
            yield return null;
        }
    }

    public void Launch()
    {
        if (rb == null) return;

        float angle = Random.Range(25f, 55f);
        float signX = Random.value < 0.5f ? -1f : 1f;

        Vector2 dir = Quaternion.Euler(0, 0, angle * signX) * Vector2.up;
        rb.linearVelocity = dir.normalized * launchSpeed;

        if (trailEffect) trailEffect.SetActive(true);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (rb == null) return;

        int colliderId = col.collider != null ? col.collider.GetInstanceID() : 0;

        if (Time.time - lastCollisionTime < collisionCooldown) return;
        if (Time.frameCount == lastCollisionFrame && colliderId == lastColliderId) return;

        lastCollisionTime = Time.time;
        lastCollisionFrame = Time.frameCount;
        lastColliderId = colliderId;

        if (bounceSound) bounceSound.Play();

        string tag = col.collider != null ? col.collider.tag : "";
        if (tag != "racket" && tag != "wall") return;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd < 0.001f) spd = launchSpeed;

        float newSpeed = Mathf.Clamp(spd * (1f + speedIncreasePercent), launchSpeed, maxSpeed);
        rb.linearVelocity = (v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.up) * newSpeed;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col == null) return;

        if (col.CompareTag("Goal1"))
        {
            if (scoreManager) scoreManager.PlayerScored();
            StartResetSequence();
        }
        else if (col.CompareTag("Goal2"))
        {
            if (scoreManager) scoreManager.EnemyScored();
            StartResetSequence();
        }
    }

    public void SetStartMode(StartMode mode)
    {
        startMode = mode;
    }

    public void HardStop()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
        waitingForLaunch = false;
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }
    }
}