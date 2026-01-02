using UnityEngine;

public class RacketController : MonoBehaviour
{
    public Camera cam;
    public Rigidbody2D rb;

    [Header("Feel")]
    public float maxSpeed = 10f;
    public float followGain = 12f;
    public float damping = 0.35f;
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

        if (!hasInput)
        {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0f, damping), 0f);
            return;
        }

        targetX = ClampX(targetX);

        float dx = targetX - rb.position.x;
        float desiredVelX = Mathf.Clamp(dx * followGain, -maxSpeed, maxSpeed);

        float newVelX = Mathf.Lerp(rb.linearVelocity.x, desiredVelX, damping);
        rb.linearVelocity = new Vector2(newVelX, 0f);

        rb.position = new Vector2(ClampX(rb.position.x), rb.position.y);
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