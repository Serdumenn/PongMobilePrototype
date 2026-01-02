using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("Runtime (auto from difficulty)")]
    public float moveSpeed = 6f;
    public float reactionDelay = 0.08f;
    public float aimError = 0.25f;
    public float returnToCenterSpeed = 3f;

    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D rb;
    public Rigidbody2D ballRb;

    private Camera cam;
    private float halfCourtWidth;

    private float nextThinkTime;
    private float cachedTargetX;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        cam = Camera.main;
        CacheCourtWidth();

        cachedTargetX = rb != null ? rb.position.x : transform.position.x;

        ApplyDifficultyFromPrefs();
    }

    private void ApplyDifficultyFromPrefs()
    {
        int d = PlayerPrefs.GetInt("difficulty", 0);

        switch (d)
        {
            case 0: // Easy 
                moveSpeed = 6.2f;
                reactionDelay = 0.11f;
                aimError = 0.28f;
                returnToCenterSpeed = 3.0f;
                break;

            case 1: // Medium
                moveSpeed = 7.0f;
                reactionDelay = 0.07f;
                aimError = 0.20f;
                returnToCenterSpeed = 3.4f;
                break;

            case 2: // Hard
                moveSpeed = 9.0f;
                reactionDelay = 0.04f;
                aimError = 0.12f;
                returnToCenterSpeed = 3.8f;
                break;
        }
    }

    private void Update()
    {
        CacheCourtWidth();
    }

    private void FixedUpdate()
    {
        if (ball == null || rb == null) return;

        if (ballRb == null) ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null) return;

        Vector2 bPos = ballRb.position;
        Vector2 bVel = ballRb.linearVelocity;

        bool comingToEnemy = IsBallComingToThisPaddle(bVel);

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + reactionDelay;

            if (comingToEnemy && Mathf.Abs(bVel.y) > 0.01f)
            {
                float tx = PredictXAtY(bPos, bVel, rb.position.y);
                tx += Random.Range(-aimError, aimError);
                cachedTargetX = Mathf.Clamp(tx, -halfCourtWidth, halfCourtWidth);
            }
            else
            {
                cachedTargetX = 0f;
            }
        }

        float speed = comingToEnemy ? moveSpeed : returnToCenterSpeed;

        float newX = Mathf.MoveTowards(rb.position.x, cachedTargetX, speed * Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -halfCourtWidth, halfCourtWidth);

        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    private bool IsBallComingToThisPaddle(Vector2 vel)
    {
        float y = rb.position.y;
        return y > 0f ? vel.y > 0f : vel.y < 0f;
    }

    private float PredictXAtY(Vector2 ballPos, Vector2 ballVel, float targetY)
    {
        float t = (targetY - ballPos.y) / ballVel.y;
        if (t < 0f) return rb.position.x;

        float rawX = ballPos.x + ballVel.x * t;

        float w = halfCourtWidth;
        if (w <= 0.01f) return rawX;

        float range = 2f * w;
        float x = rawX + w;
        float m = Mathf.Repeat(x, range);
        float mirrored = (m <= w) ? m : (range - m);
        return mirrored - w;
    }

    private void CacheCourtWidth()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        halfCourtWidth = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;

        halfCourtWidth -= 0.2f;
        if (halfCourtWidth < 0.5f) halfCourtWidth = 0.5f;
    }
}