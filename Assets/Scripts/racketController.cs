using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class RacketController : MonoBehaviour
{
    [SerializeField] private float MoveSpeed = 12f;
    [SerializeField] private bool UseMouseInEditor = true;

    private Rigidbody2D rb;
    private Camera mainCam;

    private float baseY;
    private float halfWidthWorld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        // Pong paddle must not be pushed by ball
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        baseY = rb.position.y;
        CacheHalfWidth();
    }

    private void CacheHalfWidth()
    {
        // Use sprite bounds if present; fallback to small value
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) halfWidthWorld = sr.bounds.extents.x;
        else halfWidthWorld = 0.5f;
    }

    private void FixedUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float targetX = GetTargetWorldX();
        float clampedX = ClampToCamera(targetX);

        float newX = Mathf.MoveTowards(rb.position.x, clampedX, MoveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, baseY));
    }

    private float GetTargetWorldX()
    {
        // Touch
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(t.position.x, t.position.y, -mainCam.transform.position.z));
            return w.x;
        }

        // Mouse (Editor)
#if UNITY_EDITOR
        if (UseMouseInEditor && Input.GetMouseButton(0))
        {
            Vector3 w = mainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -mainCam.transform.position.z));
            return w.x;
        }
#endif

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