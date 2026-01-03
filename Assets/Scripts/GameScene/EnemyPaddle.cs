using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("AI Settings (base, overridden by difficulty)")]
    public float moveSpeed = 7f;
    public float reactionDelay = 0.08f;
    public float aimError = 0.20f;
    public float returnToCenterSpeed = 3f;

    [Header("Stability")]
    public float wallMargin = 0.20f;

    [Tooltip("If time-to-intercept is below this, AI will 'lock' closer to ball x (prevents embarrassing misses).")]
    public float closeCatchTime = 0.28f;

    [Tooltip("Smoothing for targetX updates.")]
    public float targetSmoothing = 14f;

    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D rb;
    public Rigidbody2D ballRb;

    private Camera cam;
    private float halfCourtWidth;

    private float nextThinkTime;
    private float targetX;
    private float smoothedTargetX;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        cam = Camera.main;
        CacheCourtWidth();

        if (ball == null)
        {
            var b = GameObject.FindGameObjectWithTag("ball");
            if (b) ball = b.transform;
        }

        if (ball != null && ballRb == null)
            ballRb = ball.GetComponent<Rigidbody2D>();

        ApplyDifficulty();

        float x0 = rb != null ? rb.position.x : transform.position.x;
        targetX = x0;
        smoothedTargetX = x0;
    }

    void Update()
    {
        CacheCourtWidth();
    }

    void FixedUpdate()
    {
        if (rb == null || ball == null) return;
        if (ballRb == null) ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null) return;

        Vector2 bPos = ballRb.position;
        Vector2 bVel = ballRb.linearVelocity;

        bool coming = IsBallComingToThisPaddle(bVel);

        float timeToPaddle = float.PositiveInfinity;
        if (coming && Mathf.Abs(bVel.y) > 0.01f)
            timeToPaddle = (rb.position.y - bPos.y) / bVel.y;

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + reactionDelay;

            if (coming && timeToPaddle > 0f && timeToPaddle < 5f)
            {
                float predicted = PredictXAtY(bPos, bVel, rb.position.y);

                float ballSpeed = bVel.magnitude;
                float err = ComputeAimError(ballSpeed, timeToPaddle);

                if (timeToPaddle <= closeCatchTime)
                    err = 0f;

                predicted += Random.Range(-err, err);

                targetX = ClampX(predicted);
            }
            else
            {
                targetX = 0f;
        }

        smoothedTargetX = Mathf.Lerp(smoothedTargetX, targetX, targetSmoothing * Time.fixedDeltaTime);

        float speed = coming ? moveSpeed : returnToCenterSpeed;
        float newX = Mathf.MoveTowards(rb.position.x, smoothedTargetX, speed * Time.fixedDeltaTime);
        newX = ClampX(newX);

        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    private float ComputeAimError(float ballSpeed, float timeToPaddle)
    {
        float s = Mathf.InverseLerp(2f, 10f, ballSpeed);

        float t = Mathf.Clamp01(timeToPaddle / 0.9f);

        return aimError * Mathf.Clamp01(0.25f + 0.75f * s) * t;
    }

    private void ApplyDifficulty()
    {
        GameSettings.ForceReload();
        int d = GameSettings.DifficultyLevel;

        switch (d)
        {
            case 0:
                moveSpeed = 7.0f;
                reactionDelay = 0.09f;
                aimError = 0.32f;
                returnToCenterSpeed = 2.8f;
                closeCatchTime = 0.30f;
                break;

            case 1:
                moveSpeed = 8.0f;
                reactionDelay = 0.065f;
                aimError = 0.20f;
                returnToCenterSpeed = 3.2f;
                closeCatchTime = 0.27f;
                break;

            case 2:
                moveSpeed = 9.4f;
                reactionDelay = 0.045f;
                aimError = 0.12f;
                returnToCenterSpeed = 3.6f;
                closeCatchTime = 0.24f;
                break;
        }
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

        float w = Mathf.Max(0.5f, halfCourtWidth);
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

        float edge = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        halfCourtWidth = Mathf.Max(0.5f, Mathf.Abs(edge) - wallMargin);
    }

    private float ClampX(float x)
    {
        return Mathf.Clamp(x, -halfCourtWidth, halfCourtWidth);
    }
}