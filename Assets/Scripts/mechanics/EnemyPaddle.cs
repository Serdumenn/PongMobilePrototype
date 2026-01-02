using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 5f;
    public float difficultyError = 0.3f;
    public float predictionStep = 0.3f;

    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D rb;
    public Rigidbody2D ballRb;

    [Header("Clamp")]
    public float edgePadding = 0.6f;

    Camera cam;
    float halfCourtWidth;
    int cachedW, cachedH;

    float cachedTargetX;
    float lastBallVelY;
    bool hasPrediction;

    void Awake()
    {
        GameSettings.EnsureLoaded(difficultyError, GameSettings.DefaultDiffNeed, GameSettings.DefaultStartMode);
        difficultyError = GameSettings.DifficultyError;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (ball != null && ballRb == null) ballRb = ball.GetComponent<Rigidbody2D>();

        cam = Camera.main;
        CacheCourtWidth();
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
        bool towardEnemy = vel.y > 0f;

        if (towardEnemy)
        {
            if (!hasPrediction || Mathf.Sign(vel.y) != Mathf.Sign(lastBallVelY))
            {
                float err = Random.Range(-difficultyError, difficultyError);
                cachedTargetX = PredictBallTargetX(ball.position, vel, err);
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
        if (ballVel.y <= 0f) return rb.position.x;

        float targetY = rb.position.y; // enemy paddle y-line
        float step = Mathf.Max(0.05f, predictionStep);

        Vector2 pos = ballPos;
        Vector2 vel = ballVel.normalized * ballVel.magnitude;

        while (pos.y < targetY)
        {
            pos += vel.normalized * step;

            if (Mathf.Abs(pos.x) > halfCourtWidth)
            {
                vel.x *= -1f;
                pos.x = Mathf.Sign(pos.x) * halfCourtWidth;
            }
        }

        return Mathf.Clamp(pos.x + error, -halfCourtWidth, halfCourtWidth);
    }

    void CacheCourtWidth()
    {
        cachedW = Screen.width;
        cachedH = Screen.height;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        halfCourtWidth = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0f, 0f)).x;
    }
}