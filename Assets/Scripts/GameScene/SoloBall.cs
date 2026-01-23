using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SoloBall : MonoBehaviour
{
    [Header("Launch")]
    public float launchSpeed = 8f;
    public float maxSpeed = 16f;
    [Range(0f, 0.2f)] public float speedIncreasePercent = 0.02f; // +2%
    public float resetDelay = 0.75f;

    [Header("Stability")]
    [Range(0.05f, 0.6f)] public float minVerticalDot = 0.20f;
    public float speedUpCooldown = 0.12f;

    [Header("Effects (Optional)")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    [Header("Refs")]
    public SoloScoreManager score;
    public SoloGameManager game;

    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;

    Vector2 spawnPos;
    float lastSpeedUpTime;
    bool isStopping;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        spawnPos = rb.position;

        if (score == null) score = FindAnyObjectByType<SoloScoreManager>();
        if (game == null) game = FindAnyObjectByType<SoloGameManager>();
    }

    void FixedUpdate()
    {
        if (isStopping) return;

        Vector2 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd < 0.001f) return;

        if (spd > maxSpeed)
            rb.linearVelocity = (v / spd) * maxSpeed;

        Vector2 dir = rb.linearVelocity.normalized;
        if (Mathf.Abs(dir.y) < minVerticalDot)
        {
            float signY = 1f;
            float y = minVerticalDot * signY;
            float x = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)) * Mathf.Sign(dir.x == 0 ? 1f : dir.x);
            rb.linearVelocity = new Vector2(x, y).normalized * Mathf.Min(spd, maxSpeed);
        }
    }

    public void StartRound()
    {
        StopAllCoroutines();
        isStopping = false;
        StartCoroutine(ResetAndServe());
    }

    IEnumerator ResetAndServe()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.position = spawnPos;
        rb.simulated = false;

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);

        yield return new WaitForSeconds(resetDelay);

        if (score != null && score.IsGameOver) yield break;

        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
        rb.simulated = true;

        LaunchUpRandom();
        if (trailEffect != null) trailEffect.SetActive(true);
    }

    void LaunchUpRandom()
    {
        float angle = Random.Range(25f, 55f);
        float signX = Random.value < 0.5f ? -1f : 1f;
        Vector2 dir = (Vector2)(Quaternion.Euler(0, 0, angle * signX) * Vector2.up);

        if (Mathf.Abs(dir.y) < minVerticalDot)
            dir = new Vector2(dir.x, Mathf.Sign(dir.y) * minVerticalDot).normalized;

        rb.linearVelocity = dir.normalized * launchSpeed;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (isStopping || c == null || c.collider == null) return;

        if (c.collider.CompareTag("bottom"))
        {
            HandleBottomHit();
            return;
        }

        if (c.collider.CompareTag("wall") || c.collider.CompareTag("roof"))
        {
            PlayBounce();
            return;
        }

        if (c.collider.CompareTag("racket"))
        {
            PlayBounce();

            if (Time.time - lastSpeedUpTime < speedUpCooldown) return;
            lastSpeedUpTime = Time.time;

            if (score != null) score.AddPoint();

            Vector2 v = rb.linearVelocity;
            float spd = v.magnitude;
            float newSpeed = Mathf.Clamp(spd * (1f + speedIncreasePercent), launchSpeed, maxSpeed);

            if (spd > 0.001f)
                rb.linearVelocity = (v / spd) * newSpeed;
            else
                rb.linearVelocity = Vector2.up * launchSpeed;
        }
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (isStopping || c == null) return;
        if (c.CompareTag("bottom"))
            HandleBottomHit();
    }

    void HandleBottomHit()
    {
        if (isStopping) return;
        isStopping = true;

        PlayBounce();

        if (score != null) score.GameOver();

        if (game != null) game.OnGameOver();
        else StopAndHide();
    }

    void PlayBounce()
    {
        if (bounceSound != null) bounceSound.Play();
    }

    public void StopAndHide()
    {
        isStopping = true;
        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        if (col != null) col.enabled = false;
        if (sr != null) sr.enabled = false;
        if (trailEffect != null) trailEffect.SetActive(false);
    }
}