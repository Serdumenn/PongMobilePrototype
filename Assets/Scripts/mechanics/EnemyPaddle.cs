using Unity.Collections;
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

    private Camera cam;
    private float halfCourtWidth;
    private int cachedW, cachedH;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

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
        if (ball == null) return;

        float targetX = PredictBallTargetX();
        float newX = Mathf.MoveTowards(rb.position.x, targetX, moveSpeed * Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -halfCourtWidth + 0.5f, halfCourtWidth - 0.5f);
        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    float PredictBallTargetX()
    {
        Vector2 ballPos = ball.position;
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        Vector2 ballVel = ballRb.velocity;

        if (ballVel.y <= 0)
            return rb.position.x;

        float topBoundary = Mathf.Abs(transform.position.y);
        float stepDistance = predictionStep;

        while (ballPos.y < topBoundary)
        {
            ballPos += ballVel.normalized * stepDistance;

            if (Mathf.Abs(ballPos.x) > halfCourtWidth)
            {
                ballVel.x *= -1;
                ballPos.x = Mathf.Sign(ballPos.x) * halfCourtWidth;
            }
        }

        float error = Random.Range(-difficultyError, difficultyError);
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