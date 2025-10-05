using UnityEngine;

public class EnemyPaddle : MonoBehaviour
{
    public Transform ball;
    public float moveSpeed = 5f;
    public float minMoveThreshold = 0.5f;
    public float margin = 0.2f;

    public float rotationFactor = 5f;
    public float smoothRotation = 5f;

    private Rigidbody2D rb;
    private float paddleWidth;
    private Vector3 lastPosition;
    private float velocityX;

    public enum GameMode { Easy, Medium, Hard }
    public GameMode gameMode = GameMode.Medium;
    private float maxError;
    private float errorOffset;

    public saving prefs;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        paddleWidth = GetComponent<SpriteRenderer>().bounds.size.x;
        SetDifficultySettings();
        errorOffset = Random.Range(-maxError, maxError);
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        AutoMove();
        //UpdateRotation();
    }

    private void AutoMove()
    {
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        if (ball != null && ballRb.linearVelocity.magnitude > 0.1f)
        {
            float predictedX = PredictBallPosition();
            if (Mathf.Abs(predictedX - transform.position.x) > minMoveThreshold)
            {
                float newX = Mathf.Lerp(transform.position.x, predictedX, moveSpeed * Time.deltaTime);
                newX = Mathf.Clamp(newX, -GetCourtWidth() / 2f + paddleWidth / 2f + margin, GetCourtWidth() / 2f - paddleWidth / 2f - margin);
                rb.MovePosition(new Vector2(newX, transform.position.y));
            }
        }
    }

    /*private void UpdateRotation()
    {
        velocityX = (transform.position.x - lastPosition.x) / Time.fixedDeltaTime;
        float targetRotation = -velocityX * rotationFactor;

        if (Mathf.Abs(velocityX) > 0.01f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, targetRotation), Time.fixedDeltaTime * smoothRotation);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.fixedDeltaTime * smoothRotation);
        }

        lastPosition = transform.position;
    }*/

    private float PredictBallPosition()
    {
        Vector2 ballPos = ball.position;
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        Vector2 ballVelocity = ballRb.linearVelocity;
        if (ballVelocity.magnitude == 0) return transform.position.x;
        float predictedX = ballPos.x;
        while (ballPos.y < 5f && ballPos.y > -5f)
        {
            ballPos += ballVelocity.normalized * 0.1f;
            if (ballPos.x <= -GetCourtWidth() / 2f)
                ballVelocity.x = Mathf.Abs(ballVelocity.x);
            else if (ballPos.x >= GetCourtWidth() / 2f)
                ballVelocity.x = -Mathf.Abs(ballVelocity.x);
            if (ballPos.y >= 4f || ballPos.y <= -4f)
            {
                predictedX = ballPos.x;
                break;
            }
        }
        predictedX += errorOffset;
        return Mathf.Clamp(predictedX, -GetCourtWidth() / 2f + paddleWidth / 2f + margin, GetCourtWidth() / 2f - paddleWidth / 2f - margin);
    }

    private void SetDifficultySettings()
    {
        switch (gameMode)
        {
            case GameMode.Easy:
                maxError = 0.35f;
                break;
            case GameMode.Medium:
                maxError = 0.25f;
                break;
            case GameMode.Hard:
                maxError = 0.20f;
                break;
        }
    }

    private float GetCourtWidth()
    {
        return Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height)).x * 2f;
    }
}
