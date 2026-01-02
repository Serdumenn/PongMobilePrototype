using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("AI Settings (base, overridden by difficulty)")]
    public float moveSpeed = 7f;
    public float reactionDelay = 0.08f;
    public float aimError = 0.20f;
    public float returnToCenterSpeed = 3f;

    [Header("Scene References")]
    public Transform ball;
    public Rigidbody2D rb;
    public Rigidbody2D ballRb;

    private Camera cam;
    private float halfCourtWidth;
    private float nextThinkTime;
    private float targetX;

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
        targetX = rb != null ? rb.position.x : transform.position.x;
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

        if (Time.time >= nextThinkTime)
        {
            nextThinkTime = Time.time + reactionDelay;

            if (coming && Mathf.Abs(bVel.y) > 0.01f)
            {
                float predicted = PredictXAtY(bPos, bVel, rb.position.y);
                predicted += Random.Range(-aimError, aimError);
                targetX = Mathf.Clamp(predicted, -halfCourtWidth, halfCourtWidth);
            }
            else
            {
                targetX = 0f; // center
            }
        }

        float speed = coming ? moveSpeed : returnToCenterSpeed;
        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -halfCourtWidth, halfCourtWidth);

        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    private void ApplyDifficulty()
    {
        GameSettings.ForceReload();
        int d = GameSettings.DifficultyLevel; // 0/1/2

        // Bu değerler “easy bebek yener” sorununu çözecek şekilde biraz güçlendirildi.
        // İstersen sonra ince ayarlarız.
        switch (d)
        {
            case 0: // Easy
                moveSpeed = 6.2f;
                reactionDelay = 0.10f;
                aimError = 0.35f;
                returnToCenterSpeed = 2.6f;
                break;

            case 1: // Medium
                moveSpeed = 7.4f;
                reactionDelay = 0.07f;
                aimError = 0.22f;
                returnToCenterSpeed = 3.0f;
                break;

            case 2: // Hard
                moveSpeed = 9.2f;
                reactionDelay = 0.045f;
                aimError = 0.14f;
                returnToCenterSpeed = 3.6f;
                break;
        }
    }

    private bool IsBallComingToThisPaddle(Vector2 vel)
    {
        // Paddle yukarıdaysa (y>0): top geliyorsa vel.y > 0
        // Paddle aşağıdaysa (y<0): top geliyorsa vel.y < 0
        float y = rb.position.y;
        return y > 0f ? vel.y > 0f : vel.y < 0f;
    }

    private float PredictXAtY(Vector2 ballPos, Vector2 ballVel, float targetY)
    {
        float t = (targetY - ballPos.y) / ballVel.y;
        if (t < 0f) return rb.position.x;

        float rawX = ballPos.x + ballVel.x * t;

        // Mirror reflection within [-halfCourtWidth, +halfCourtWidth]
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

        halfCourtWidth = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        halfCourtWidth -= 0.2f; // small margin
        if (halfCourtWidth < 0.5f) halfCourtWidth = 0.5f;
    }
}