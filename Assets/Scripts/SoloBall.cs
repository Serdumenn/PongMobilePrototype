using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SoloBall : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] float launchSpeed = 8f;
    [SerializeField] float maxSpeed = 16f;
    [SerializeField, Range(0f, 0.2f)] float speedIncreasePercent = 0.02f;
    [SerializeField] float resetDelay = 0.75f;

    [Header("Stability")]
    [SerializeField, Range(0f, 0.95f)] float minVerticalDot = 0.30f;
    [SerializeField] float speedUpCooldown = 0.12f;

    [Header("Variation (Optional)")]
    [SerializeField, Range(0f, 1f)] float paddleAimStrength = 0.6f;

    [Header("Anti-Stick")]
    [SerializeField] float paddleSeparation = 0.03f;

    [Header("Effects (Optional)")]
    [SerializeField] GameObject trailEffect;
    [SerializeField] AudioSource bounceSound;

    [Header("Refs")]
    [SerializeField] SoloScoreManager score;
    [SerializeField] SoloGameManager game;

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector3 startPos;
    Coroutine resetCo;

    bool canTriggerBottom;
    bool isStopped;
    float lastSpeedUpTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>() ?? GetComponent<SpriteRenderer>();

        startPos = transform.position;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (score == null) score = FindFirstObjectByType<SoloScoreManager>();
        if (game == null) game = FindFirstObjectByType<SoloGameManager>();

        if (trailEffect != null) trailEffect.SetActive(false);
    }

    void Start()
    {
        StartRound();
    }

    void OnDisable()
    {
        if (resetCo != null)
        {
            StopCoroutine(resetCo);
            resetCo = null;
        }
    }

    public void StartRound()
    {
        isStopped = false;

        if (resetCo != null) StopCoroutine(resetCo);
        resetCo = StartCoroutine(ResetAndServe());
    }

    IEnumerator ResetAndServe()
    {
        canTriggerBottom = false;

        col.enabled = false;
        rb.simulated = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = startPos;

        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);

        yield return new WaitForSeconds(resetDelay);

        if (isStopped) yield break;
        if (game != null && game.IsGameOver) yield break;

        if (sr != null) sr.enabled = true;

        rb.simulated = true;
        col.enabled = true;

        LaunchDownRandom();

        yield return new WaitForSeconds(0.10f);
        canTriggerBottom = true;

        resetCo = null;
    }

    void FixedUpdate()
    {
        if (isStopped || !rb.simulated) return;

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed <= 0.0001f) return;

        if (speed > maxSpeed)
        {
            v = (v / speed) * maxSpeed;
            speed = maxSpeed;
        }

        Vector2 dir = v / speed;
        if (Mathf.Abs(dir.y) < minVerticalDot)
        {
            float signY = Mathf.Sign(dir.y);
            if (signY == 0f) signY = 1f;

            float newY = signY * minVerticalDot;

            float signX = Mathf.Sign(dir.x);
            if (signX == 0f) signX = 1f;

            float newX = signX * Mathf.Sqrt(Mathf.Max(0f, 1f - newY * newY));

            dir = new Vector2(newX, newY);
            v = dir * speed;
        }

        rb.linearVelocity = v;
    }

    void LaunchDownRandom()
    {
        float angle = Random.Range(25f, 55f);
        float signX = Random.value < 0.5f ? -1f : 1f;

        Vector2 dir = (Vector2)(Quaternion.Euler(0, 0, angle * signX) * Vector2.down);
        dir = EnsureMinVerticalDir(dir);

        rb.linearVelocity = dir * launchSpeed;

        if (trailEffect != null) trailEffect.SetActive(true);
    }

    Vector2 EnsureMinVerticalDir(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return Vector2.down;

        dir.Normalize();

        if (Mathf.Abs(dir.y) < minVerticalDot)
        {
            float signY = Mathf.Sign(dir.y);
            if (signY == 0f) signY = 1f;

            float newY = signY * minVerticalDot;

            float signX = Mathf.Sign(dir.x);
            if (signX == 0f) signX = 1f;

            float newX = signX * Mathf.Sqrt(Mathf.Max(0f, 1f - newY * newY));

            dir = new Vector2(newX, newY).normalized;
        }

        return dir;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (isStopped || c == null || c.collider == null) return;

        if (c.collider.CompareTag("bottom"))
        {
            if (!canTriggerBottom) return;
            HandleBottomHit();
            return;
        }

        if (c.collider.CompareTag("wall"))
        {
            PlayBounce();
            return;
        }

        if (c.collider.CompareTag("racket"))
        {
            HandleRacketHit(c);
            return;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isStopped || other == null) return;
        if (!canTriggerBottom) return;

        if (other.CompareTag("bottom"))
        {
            HandleBottomHit();
        }
    }

    void HandleRacketHit(Collision2D c)
    {
        PlayBounce();

        if (score != null) score.AddPoint();

        Vector2 v = rb.linearVelocity;
        float speed = v.magnitude;

        if (speed < 0.001f) speed = launchSpeed;

        if (c.contactCount > 0)
        {
            Vector2 normal = c.GetContact(0).normal;
            v = Vector2.Reflect(v, normal);

            if (paddleSeparation > 0f)
                rb.position += normal * paddleSeparation;
        }

        if (v.y <= 0f) v.y = Mathf.Abs(v.y);

        if (paddleAimStrength > 0f)
        {
            Bounds b = c.collider.bounds;
            float half = Mathf.Max(0.0001f, b.extents.x);
            float offset01 = Mathf.Clamp((rb.position.x - b.center.x) / half, -1f, 1f);

            v.x += offset01 * paddleAimStrength * speed;
        }

        Vector2 dir = EnsureMinVerticalDir(v);

        if (Time.time - lastSpeedUpTime >= speedUpCooldown)
        {
            lastSpeedUpTime = Time.time;
            speed = Mathf.Min(speed * (1f + speedIncreasePercent), maxSpeed);
        }
        else
        {
            speed = Mathf.Min(speed, maxSpeed);
        }

        rb.linearVelocity = dir * speed;
    }

    void HandleBottomHit()
    {
        if (score != null) score.GameOver();
        if (game != null) game.GameOver();

        HardStopAndHide();
    }

    void PlayBounce()
    {
        if (bounceSound != null) bounceSound.Play();
    }

    public void HardStopAndHide()
    {
        isStopped = true;

        if (resetCo != null)
        {
            StopCoroutine(resetCo);
            resetCo = null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.simulated = false;
        col.enabled = false;

        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);
    }
}