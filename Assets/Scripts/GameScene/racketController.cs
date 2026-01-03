using UnityEngine;

public class racketController : MonoBehaviour
{
    public Camera cam;
    public Rigidbody2D rb;

    [Header("Feel")]
    public float maxSpeed = 10f;
    public float followGain = 12f;

    [Tooltip("How fast we accelerate toward desired velocity (units/s^2).")]
    public float acceleration = 60f;

    [Tooltip("How fast we decelerate to 0 when no input (units/s^2).")]
    public float deceleration = 80f;

    public float wallMargin = 0.25f;

    private float halfCourtWidth;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        CacheCourtWidth();
    }

    void Update()
    {
        CacheCourtWidth();
    }

    void FixedUpdate()
    {
        if (rb == null || cam == null) return;

        bool hasInput = false;
        float targetX = rb.position.x;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, 0f));
            targetX = w.x;
            hasInput = true;
        }
        else if (Input.GetMouseButton(0))
        {
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f));
            targetX = w.x;
            hasInput = true;
        }

        float desiredVelX = 0f;

        if (hasInput)
        {
            targetX = ClampX(targetX);
            float dx = targetX - rb.position.x;
            desiredVelX = Mathf.Clamp(dx * followGain, -maxSpeed, maxSpeed);
        }

        float currentVelX = rb.linearVelocity.x;
        float a = hasInput ? acceleration : deceleration;
        float newVelX = Mathf.MoveTowards(currentVelX, desiredVelX, a * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelX, 0f);

        float clampedX = ClampX(rb.position.x);
        if (!Mathf.Approximately(clampedX, rb.position.x))
            rb.position = new Vector2(clampedX, rb.position.y);
    }

    void CacheCourtWidth()
    {
        if (cam == null || Screen.width <= 0) return;
        float edgeX = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        halfCourtWidth = Mathf.Abs(edgeX);
    }

    float ClampX(float x)
    {
        float w = Mathf.Max(0.5f, halfCourtWidth - wallMargin);
        return Mathf.Clamp(x, -w, w);
    }
}