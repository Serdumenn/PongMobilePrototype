using System.Collections;
using UnityEngine;

public class ball : MonoBehaviour
{
    [Header("Launch / Speed")]
    public float launchSpeed = 8f;
    [Tooltip("Each collision multiplies speed by this value. 1.02 = +%2")]
    public float speedMultiplierPerHit = 1.02f;
    public float maxSpeed = 16f;

    [Header("Controlled Pong Bounce")]
    [Range(0f, 89f)] public float maxBounceAngle = 75f;
    [Tooltip("Prevents near-horizontal infinite loops. 0.25 means |vy| >= 25% of speed.")]
    [Range(0.05f, 0.9f)] public float minVerticalRatio = 0.25f;
    [Tooltip("Tiny randomness to avoid repeated identical trajectories.")]
    [Range(0f, 0.1f)] public float tinyRandomness = 0.02f;

    [Header("Visual / Audio")]
    public GameObject trailEffect;
    public AudioSource bounceSound;

    Rigidbody2D rb;
    Vector2 initialPosition;
    Coroutine resetRoutine;
    bool waitingForLaunch;
    SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        initialPosition = transform.position;
    }

    void Start() => StartResetSequence();

    void StartResetSequence()
    {
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ResetBallTapToLaunch());
    }

    IEnumerator ResetBallTapToLaunch()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = initialPosition;
        waitingForLaunch = true;
        if (sr) sr.enabled = false;

        yield return new WaitForSeconds(1.0f);

        if (sr) sr.enabled = true;

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
        float angle = Random.Range(-45f, 45f);
        float dirY = (Random.value < 0.5f) ? -1f : 1f;

        Vector2 dir = AngleToDir(angle, dirY);
        rb.linearVelocity = dir * launchSpeed;

        if (trailEffect != null) trailEffect.SetActive(true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (bounceSound) bounceSound.Play();

        float speed = Mathf.Clamp(rb.linearVelocity.magnitude * speedMultiplierPerHit, launchSpeed, maxSpeed);

        if (collision.collider.CompareTag("Paddle"))
        {
            Transform paddle = collision.collider.transform;

            float halfWidth = GetApproxHalfWidth(collision.collider);
            float relX = (halfWidth > 0.0001f) ? (transform.position.x - paddle.position.x) / halfWidth : 0f;
            relX = Mathf.Clamp(relX, -1f, 1f);

            float bounceAngle = relX * maxBounceAngle;

            float dirY = (transform.position.y >= paddle.position.y) ? 1f : -1f;

            Vector2 dir = AngleToDir(bounceAngle, dirY);
            dir = EnforceMinVertical(dir, dirY);

            dir = AddTinyNoise(dir);

            rb.linearVelocity = dir.normalized * speed;
            return;
        }

        Vector2 v = rb.linearVelocity;
        if (!float.IsFinite(v.x) || !float.IsFinite(v.y)) v = Vector2.up * launchSpeed;

        Vector2 dir2 = v.normalized;
        float signY = Mathf.Sign(dir2.y);
        if (signY == 0) signY = (Random.value < 0.5f) ? -1f : 1f;

        dir2 = EnforceMinVertical(dir2, signY);
        dir2 = AddTinyNoise(dir2);

        rb.linearVelocity = dir2.normalized * speed;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Goal1") || col.CompareTag("Goal2"))
            StartResetSequence();
    }

    Vector2 AngleToDir(float angleDeg, float dirY)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float x = Mathf.Sin(rad);
        float y = Mathf.Cos(rad) * Mathf.Sign(dirY);
        return new Vector2(x, y);
    }

    Vector2 EnforceMinVertical(Vector2 dir, float dirYSign)
    {
        float absY = Mathf.Abs(dir.y);
        if (absY >= minVerticalRatio) return dir;

        float y = minVerticalRatio * Mathf.Sign(dirYSign);
        float xSign = (dir.x >= 0f) ? 1f : -1f;
        float x = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y)) * xSign;

        return new Vector2(x, y);
    }

    Vector2 AddTinyNoise(Vector2 dir)
    {
        if (tinyRandomness <= 0f) return dir;
        dir.x += Random.Range(-tinyRandomness, tinyRandomness);
        dir.y += Random.Range(-tinyRandomness, tinyRandomness);
        return dir.normalized;
    }

    float GetApproxHalfWidth(Collider2D col)
    {
        return col.bounds.extents.x;
    }
}