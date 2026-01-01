using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 5f;
    [Tooltip("Prediction step in world units (smaller = more accurate but more iterations).")]
    public float predictionStep = 0.15f;

    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D rb;
    public Rigidbody2D ballRb;

    [Header("Clamp")]
    public float edgePadding = 0.6f;

    private Camera cam;
    private float halfCourtWidth;
    private int cachedW, cachedH;

    private bool hasPrediction;
    private float cachedTargetX;
    private float lastBallVelY;

    private float difficultyError;

    void Awake()
    {
        GameSettings.EnsureLoaded();

        difficultyError = GameSettings.DifficultyError;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ball != null && ballRb == null) ballRb = ball.GetComponent<Rigidbody2D>();

        cam = Camera.main;
        CacheCourtWidth();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }
    }

    void Update()
    {
        if (Screen.width != cachedW || Screen.height != cachedH)
            CacheCourtWidth();
    }

    void FixedUpdate()
    {
        if (ball == null || rb == null) return;
        if (ballRb == null) ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null) return;

        Vector2 vel = ballRb.linearVelocity;
        bool movingTowardEnemy = vel.y > 0f;

        if (movingTowardEnemy)
        {
            if (!hasPrediction || Mathf.Sign(vel.y) != Mathf.Sign(lastBallVelY))
            {
                float error = Random.Range(-difficultyError, difficultyError);
                cachedTargetX = PredictBallTargetX(ball.position, vel, error);
                hasPrediction = true;
            }
        }
        else
        {
            hasPrediction = false;
        }

        lastBallVelY = vel.y;

        float targetX = hasPrediction ? cachedTargetX : rb.position.x;
        float newX = Mathf.MoveTowards(rb.position.x, targetX, moveSpeed * Time.fixedDeltaTime);

        newX = Mathf.Clamp(newX, -halfCourtWidth + edgePadding, halfCourtWidth - edgePadding);
        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    float PredictBallTargetX(Vector2 ballPos, Vector2 ballVel, float error)
    {
        if (ballVel.sqrMagnitude < 0.0001f) return rb.position.x;
        if (ballVel.y <= 0f) return rb.position.x;

        float targetY = transform.position.y;

        float step = Mathf.Max(0.05f, predictionStep);
        Vector2 dir = ballVel.normalized;

        int safety = 0;
        while (ballPos.y < targetY && safety++ < 10000)
        {
            ballPos += dir * step;

            if (ballPos.x > halfCourtWidth)
            {
                ballPos.x = halfCourtWidth;
                dir.x *= -1f;
            }
            else if (ballPos.x < -halfCourtWidth)
            {
                ballPos.x = -halfCourtWidth;
                dir.x *= -1f;
            }
        }

        return Mathf.Clamp(ballPos.x + error, -halfCourtWidth, halfCourtWidth);
    }

    void CacheCourtWidth()
    {
        cachedW = Screen.width;
        cachedH = Screen.height;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        halfCourtWidth = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
    }
}