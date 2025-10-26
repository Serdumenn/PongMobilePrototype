using System.Collections;
using UnityEngine;

public class ball : MonoBehaviour
{
    public float launchSpeed = 8f;
    public float speedIncreasePerHit = 0.5f;
    public float maxSpeed = 16f;
    public GameObject trailEffect;
    public AudioSource bounceSound;

    private Rigidbody2D rb;
    private Vector2 initialPosition;
    private Coroutine resetRoutine;
    private bool waitingForLaunch = false;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        initialPosition = transform.position;
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
        resetRoutine = StartCoroutine(ResetBallWithCountdown());
    }

    IEnumerator ResetBallWithCountdown()
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
        float randomAngle = Random.Range(30f, 60f);
        float direction = Random.value < 0.5f ? -1f : 1f;
        Vector2 launchDir = Quaternion.Euler(0, 0, randomAngle * direction) * Vector2.up;

        rb.linearVelocity = launchDir * launchSpeed;

        if (trailEffect != null)
            trailEffect.SetActive(true);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (bounceSound != null)
            bounceSound.Play();

        var v = rb.linearVelocity;
        if (!float.IsFinite(v.x) || !float.IsFinite(v.y))
            v = Vector2.up * launchSpeed;

        float newSpeed = Mathf.Clamp(v.magnitude + speedIncreasePerHit, launchSpeed, maxSpeed);
        rb.linearVelocity = v.normalized * newSpeed;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Goal1") || col.CompareTag("Goal2"))
        {
            StartResetSequence();
        }
    }
}