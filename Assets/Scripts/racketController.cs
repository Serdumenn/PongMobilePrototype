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

    private Rigidbody2D rb;
    private Camera mainCam;

    private float baseY;
    private float halfWidthWorld;
    private float currentTiltAngle;

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

        baseY = rb.position.y;
        CacheHalfWidth();
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
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, -mainCam.transform.position.z));
            return w.x;
        }

        if (Input.GetMouseButton(0))
        {
            Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCam.transform.position.z));
            return w.x;
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