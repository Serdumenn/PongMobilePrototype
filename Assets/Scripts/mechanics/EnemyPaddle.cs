using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Rigidbody2D ballRb;

    [Header("Movement")]
    public float moveSpeed = 8f;

    public float smoothTime = 0.08f;

    public float wallPadding = 0.35f;

    [Header("Prediction")]
    public float predictionInterval = 0.10f;

    [Tooltip("Easy(world units)")]
    public float errorEasy = 1.2f;

    [Tooltip("Medium(world units)")]
    public float errorMedium = 0.6f;

    [Tooltip("Hard(world units)")]
    public float errorHard = 0.15f;

    private Camera cam;
    private float halfCourtWidth;
    private float nextPredictTime;
    private float targetX;
    private float xVelRef;
    private float currentErrorOffset;
    private float prevBallVy;
    private int cachedDifficulty = -999;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!ballRb)
        {
            var ballObj = GameObject.FindWithTag("Ball");
            if (ballObj) ballRb = ballObj.GetComponent<Rigidbody2D>();
        }

        cam = Camera.main;
        CacheCourtWidth();

        targetX = transform.position.x;
        nextPredictTime = 0f;
        prevBallVy = 0f;
    }

    private void Update()
    {
        CacheCourtWidth();
    }

    private void FixedUpdate()
    {
        if (!rb || !ballRb) return;

        var ballVel = ballRb.linearVelocity;
        var ballPos = ballRb.position;

        ApplyDifficultyIfNeeded();

        if (ballVel.y > 0f && prevBallVy <= 0f)
        {
            currentErrorOffset = Random.Range(-GetErrorRange(), GetErrorRange());
            nextPredictTime = 0f;
        }
        prevBallVy = ballVel.y;

        if (Time.time >= nextPredictTime)
        {
            targetX = PredictTargetX(ballPos, ballVel);
            nextPredictTime = Time.time + predictionInterval;
        }

        float maxX = Mathf.Max(0.1f, halfCourtWidth - wallPadding);
        float desiredX = Mathf.Clamp(targetX, -maxX, maxX);

        float newX = Mathf.SmoothDamp(rb.position.x, desiredX, ref xVelRef, smoothTime, moveSpeed, Time.fixedDeltaTime);
        newX = Mathf.Clamp(newX, -maxX, maxX);

        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    private void ApplyDifficultyIfNeeded()
    {
        int diff = GameSettings.GetDifficulty(0);
        if (diff == cachedDifficulty) return;

        cachedDifficulty = diff;

        switch (diff)
        {
            case 0:
                moveSpeed = 7.0f;
                break;
            case 1:
                moveSpeed = 9.0f;
                break;
            case 2:
                moveSpeed = 11.5f;
                break;
        }
    }

    private float GetErrorRange()
    {
        switch (cachedDifficulty < 0 ? GameSettings.GetDifficulty(0) : cachedDifficulty)
        {
            case 0: return errorEasy;
            case 1: return errorMedium;
            case 2: return errorHard;
            default: return errorEasy;
        }
    }

    private float PredictTargetX(Vector2 ballPos, Vector2 ballVel)
    {
        if (ballVel.y <= 0.01f) return rb.position.x;

        float enemyY = rb.position.y;

        float t = (enemyY - ballPos.y) / ballVel.y;
        if (t <= 0f) return rb.position.x;

        float rawX = ballPos.x + ballVel.x * t;

        float maxX = Mathf.Max(0.1f, halfCourtWidth - wallPadding);
        float reflectedX = ReflectInBounds(rawX, maxX);

        reflectedX += currentErrorOffset;

        return Mathf.Clamp(reflectedX, -maxX, maxX);
    }

    private float ReflectInBounds(float x, float maxX)
    {
        float L = maxX * 2f;
        float twoL = L * 2f;
        float shifted = x + maxX;

        float r = Mathf.Repeat(shifted, twoL);
        if (r > L) r = twoL - r;
        return r - maxX;
    }

    private void CacheCourtWidth()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        float left = cam.ScreenToWorldPoint(new Vector3(0, 0, 0)).x;
        float right = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;

        halfCourtWidth = Mathf.Abs(right - left) * 0.5f;
    }
}