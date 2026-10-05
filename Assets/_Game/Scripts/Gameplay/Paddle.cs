using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class Paddle : MonoBehaviour
{
    [Header("Side")]
    [SerializeField] private FieldSide PlaySide = FieldSide.Bottom;

    [Header("Movement")]
    [SerializeField] private float MoveSpeed = 12f;

    [Header("Tilt")]
    [SerializeField] private float MaxTiltDeg = 18f;
    [SerializeField] private float SpeedForMaxTilt = 10f;
    [SerializeField] private float TiltSmooth = 15f;

    [Header("Input Zone")]
    [SerializeField, Range(0.1f, 1f)] private float InputZoneRatio = 0.5f;

    [Header("Refs")]
    [SerializeField, FormerlySerializedAs("Walls")] private FieldLayout Layout;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Camera mainCam;

    private Vector2 basePosition;
    private float halfLength;
    private Vector3 baseScale;
    private float lengthScale = 1f;
    private float currentTiltAngle;
    private FieldSide defaultSide;
    private Vector2 defaultPosition;
    private IPaddleController defaultController;
    private Sprite defaultSprite;

    public FieldSide Side => PlaySide;
    public Vector2 Inward => PlaySide.Inward();
    public IPaddleController Controller { get; set; }
    public bool InputEnabled { get; set; }
    public bool HasActiveInput { get; private set; }
    public float HalfWidth => halfLength;
    public Vector2 Home => basePosition;
    public Vector2 DefaultHome => defaultPosition;
    public SpriteRenderer Visual => sr;

    private bool Horizontal => PlaySide.MovesHorizontally();
    private float TiltSign => PlaySide == FieldSide.Top || PlaySide == FieldSide.Left ? -1f : 1f;

    private float BaseAngle => PlaySide switch
    {
        FieldSide.Top => 180f,
        FieldSide.Left => -90f,
        FieldSide.Right => 90f,
        _ => 0f
    };

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        mainCam = Camera.main;
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();

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

        basePosition = rb.position;
        baseScale = transform.localScale;
        CacheHalfLength();
        if (Controller == null) Controller = new TouchZoneController(PlaySide, InputZoneRatio);

        defaultSide = PlaySide;
        defaultPosition = basePosition;
        defaultController = Controller;
        defaultSprite = sr != null ? sr.sprite : null;
    }

    public void Configure(FieldSide side, Vector2 home, IPaddleController controller)
    {
        PlaySide = side;
        basePosition = home;
        Controller = controller;
        PlaceAtHome();
    }

    public void ResetToDefault()
    {
        PlaySide = defaultSide;
        basePosition = defaultPosition;
        Controller = defaultController;
        if (sr != null && defaultSprite != null) sr.sprite = defaultSprite;
        ResetPosition();
    }

    public void SetSprite(Sprite sprite)
    {
        if (sr == null) return;
        if (defaultSprite == null) defaultSprite = sr.sprite;
        if (sprite != null) sr.sprite = sprite;
    }

    public void RememberSprite()
    {
        if (sr != null) defaultSprite = sr.sprite;
    }

    public void ResetPosition()
    {
        Vector2 home = Horizontal ? new Vector2(0f, basePosition.y) : new Vector2(basePosition.x, 0f);
        rb.position = home;
        transform.position = new Vector3(home.x, home.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, BaseAngle);
        currentTiltAngle = 0f;
    }

    public void PlaceAtHome()
    {
        rb.position = basePosition;
        transform.position = new Vector3(basePosition.x, basePosition.y, 0f);
        transform.rotation = Quaternion.Euler(0f, 0f, BaseAngle);
        currentTiltAngle = 0f;
    }

    public float LengthScale => lengthScale;

    public void SetLengthScale(float scale)
    {
        lengthScale = Mathf.Max(0.1f, scale);
        transform.localScale = new Vector3(baseScale.x * lengthScale, baseScale.y, baseScale.z);
        CacheHalfLength();
    }

    public void SetVisible(bool visible)
    {
        if (sr != null) sr.enabled = visible;
    }

    public float AxisOffset(Vector2 worldPoint)
    {
        float half = Mathf.Max(0.01f, halfLength);
        return Mathf.Clamp((AxisValue(worldPoint) - AxisValue(transform.position)) / half, -1f, 1f);
    }

    private void CacheHalfLength()
    {
        if (sr == null)
        {
            halfLength = 0.5f;
            return;
        }

        Vector3 scale = sr.transform.lossyScale;
        halfLength = sr.sprite != null ? sr.sprite.bounds.extents.x * Mathf.Abs(scale.x) : sr.bounds.extents.x;
    }

    private void FixedUpdate()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float current = AxisValue(rb.position);
        float target = ClampToField(GetTarget(current));
        float next = Mathf.MoveTowards(current, target, MoveSpeed * Time.fixedDeltaTime);

        rb.MovePosition(WithAxis(next));

        float velocity = (next - current) / Time.fixedDeltaTime;
        float tiltNormalized = Mathf.Clamp(velocity / Mathf.Max(0.01f, SpeedForMaxTilt), -1f, 1f);
        float targetAngle = -tiltNormalized * MaxTiltDeg * TiltSign;

        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetAngle, TiltSmooth * Time.fixedDeltaTime);
        rb.MoveRotation(BaseAngle + currentTiltAngle);
    }

    private float GetTarget(float current)
    {
        HasActiveInput = false;
        if (!InputEnabled || Controller == null) return current;
        if (!Controller.TryGetTarget(this, mainCam, out float target)) return current;

        HasActiveInput = true;
        return target;
    }

    private float ClampToField(float value)
    {
        float min;
        float max;

        if (Layout != null && Layout.Field.width > 0f)
        {
            Vector2 range = Layout.RangeFor(PlaySide);
            min = range.x;
            max = range.y;
        }
        else
        {
            float depth = -mainCam.transform.position.z;
            Vector3 low = mainCam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 high = mainCam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            min = Horizontal ? low.x : low.y;
            max = Horizontal ? high.x : high.y;
        }

        return Mathf.Clamp(value, min + halfLength, max - halfLength);
    }

    private float AxisValue(Vector2 point)
    {
        return Horizontal ? point.x : point.y;
    }

    private Vector2 WithAxis(float value)
    {
        return Horizontal ? new Vector2(value, basePosition.y) : new Vector2(basePosition.x, value);
    }
}
