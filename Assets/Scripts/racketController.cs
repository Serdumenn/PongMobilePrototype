using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class RacketController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float MoveSpeed = 12f;

    [Header("Tilt")]
    [SerializeField] private float MaxTiltDeg = 18f;
    [SerializeField] private float SpeedForMaxTilt = 10f;
    [SerializeField] private float TiltSmooth = 15f;

    [Header("Input Zone")]
    [SerializeField, Range(0.1f, 1f)] private float InputZoneRatio = 0.5f;

    private Rigidbody2D rb;
    private Camera mainCam;

    private float baseY;
    private float halfWidthWorld;
    private float currentTiltAngle;

    public bool InputEnabled { get; set; }
    public bool HasActiveInput { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var noBounce = new PhysicsMaterial2D("PaddleNoBounce");
        noBounce.bounciness = 0f;
        noBounce.friction = 0f;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.sharedMaterial = noBounce;

        baseY = rb.position.y;
        CacheHalfWidth();
    }

    public void ResetPosition()
    {
        rb.position = new Vector2(0f, baseY);
        transform.position = new Vector3(0f, baseY, 0f);
        transform.rotation = Quaternion.identity;
        currentTiltAngle = 0f;
    }

    public void SetVisible(bool visible)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;
    }

    private void CacheHalfWidth()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        halfWidthWorld = (sr != null) ? sr.bounds.extents.x : 0.5f;
    }

    private void FixedUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float targetX = GetTargetWorldX();
        float clampedX = ClampToCamera(targetX);

        float oldX = rb.position.x;
        float newX = Mathf.MoveTowards(oldX, clampedX, MoveSpeed * Time.fixedDeltaTime);

        rb.MovePosition(new Vector2(newX, baseY));

        float velocityX = (newX - oldX) / Time.fixedDeltaTime;
        float tiltNormalized = Mathf.Clamp(velocityX / Mathf.Max(0.01f, SpeedForMaxTilt), -1f, 1f);
        float targetAngle = -tiltNormalized * MaxTiltDeg;

        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, TiltSmooth * Time.fixedDeltaTime);
        rb.MoveRotation(currentTiltAngle);
    }

    private float GetTargetWorldX()
    {
        HasActiveInput = false;
        if (!InputEnabled) return rb.position.x;

        float threshold = Screen.height * InputZoneRatio;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.position.y < threshold)
            {
                HasActiveInput = true;
                Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, -mainCam.transform.position.z));
                return w.x;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (Input.mousePosition.y < threshold)
            {
                HasActiveInput = true;
                Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCam.transform.position.z));
                return w.x;
            }
        }

        return rb.position.x;
    }

    private float ClampToCamera(float x)
    {
        Vector3 left = mainCam.ViewportToWorldPoint(new Vector3(0f, 0.5f, -mainCam.transform.position.z));
        Vector3 right = mainCam.ViewportToWorldPoint(new Vector3(1f, 0.5f, -mainCam.transform.position.z));

        float minX = left.x + halfWidthWorld;
        float maxX = right.x - halfWidthWorld;

        return Mathf.Clamp(x, minX, maxX);
    }
}