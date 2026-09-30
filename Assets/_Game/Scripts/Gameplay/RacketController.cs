using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Refs")]
    [SerializeField] private ResponsiveWalls Walls;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Camera mainCam;

    private float baseY;
    private float halfWidthWorld;
    private float currentTiltAngle;

    public bool InputEnabled { get; set; }
    public bool HasActiveInput { get; private set; }
    public float HalfWidth => halfWidthWorld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        mainCam = Camera.main;
        if (Walls == null) Walls = FindFirstObjectByType<ResponsiveWalls>();

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
        if (sr != null) sr.enabled = visible;
    }

    private void CacheHalfWidth()
    {
        halfWidthWorld = (sr != null) ? sr.bounds.extents.x : 0.5f;
    }

    private void FixedUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float targetX = GetTargetWorldX();
        float clampedX = ClampToField(targetX);

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
        if (!TryGetPress(threshold, out Vector2 screenPos)) return rb.position.x;

        HasActiveInput = true;
        return mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCam.transform.position.z)).x;
    }

    private static bool TryGetPress(float threshold, out Vector2 position)
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed) continue;

                position = touch.position.ReadValue();
                if (position.y < threshold) return true;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            position = mouse.position.ReadValue();
            if (position.y < threshold) return true;
        }

        position = default;
        return false;
    }

    private float ClampToField(float x)
    {
        float minX;
        float maxX;

        if (Walls != null && Walls.Field.width > 0f)
        {
            minX = Walls.Field.xMin;
            maxX = Walls.Field.xMax;
        }
        else
        {
            minX = mainCam.ViewportToWorldPoint(new Vector3(0f, 0.5f, -mainCam.transform.position.z)).x;
            maxX = mainCam.ViewportToWorldPoint(new Vector3(1f, 0.5f, -mainCam.transform.position.z)).x;
        }

        return Mathf.Clamp(x, minX + halfWidthWorld, maxX - halfWidthWorld);
    }
}