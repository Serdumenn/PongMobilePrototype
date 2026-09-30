using UnityEngine;

public sealed class ResponsiveWalls : MonoBehaviour
{
    [Header("Wall References")]
    [SerializeField] private Transform LeftWall;
    [SerializeField] private Transform RightWall;
    [SerializeField] private Transform Roof;
    [SerializeField] private Transform Bottom;

    [Header("Dimensions")]
    [SerializeField] private float WallThickness = 0.5f;
    [SerializeField] private float EdgeOffset = 0.25f;

    [Header("Field")]
    [SerializeField] private float MaxFieldAspect = 0.5625f;

    [Header("Rails")]
    [SerializeField] private SpriteRenderer LeftRail;
    [SerializeField] private SpriteRenderer RightRail;
    [SerializeField] private float RailWidth = 0.09f;

    private Camera cam;
    private int lastScreenW;
    private int lastScreenH;
    private Rect lastSafeArea;

    public Rect Field { get; private set; }

    private void Awake()
    {
        var noBounce = SoloBall.GetSharedNoBounce();

        AssignMaterial(LeftWall, noBounce);
        AssignMaterial(RightWall, noBounce);
        AssignMaterial(Roof, noBounce);
        AssignMaterial(Bottom, noBounce);

        HideRenderer(LeftWall);
        HideRenderer(RightWall);
        HideRenderer(Roof);
        HideRenderer(Bottom);

        Recalculate();
    }

    private static void AssignMaterial(Transform wall, PhysicsMaterial2D mat)
    {
        if (wall == null) return;
        var col = wall.GetComponent<Collider2D>();
        if (col != null) col.sharedMaterial = mat;
    }

    private static void HideRenderer(Transform wall)
    {
        if (wall == null) return;
        var sr = wall.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }

    private void Update()
    {
        if (Screen.width != lastScreenW || Screen.height != lastScreenH || Screen.safeArea != lastSafeArea)
            Recalculate();
    }

    private void Recalculate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
        lastSafeArea = Screen.safeArea;

        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.orthographicSize * cam.aspect;
        Vector3 center = cam.transform.position;

        Rect safe = lastSafeArea;
        float depth = -center.z;
        float safeLeft = cam.ScreenToWorldPoint(new Vector3(safe.xMin, 0f, depth)).x;
        float safeRight = cam.ScreenToWorldPoint(new Vector3(safe.xMax, 0f, depth)).x;
        float top = cam.ScreenToWorldPoint(new Vector3(0f, safe.yMax, depth)).y;
        float bottom = center.y - halfHeight;

        float maxHalfWidth = MaxFieldAspect > 0f ? halfHeight * MaxFieldAspect : float.MaxValue;
        float left = Mathf.Max(safeLeft, center.x - maxHalfWidth);
        float right = Mathf.Min(safeRight, center.x + maxHalfWidth);

        Field = Rect.MinMaxRect(left, bottom, right, top);

        PlaceRail(LeftRail, left - RailWidth / 2f, left > safeLeft + 0.001f, top, bottom);
        PlaceRail(RightRail, right + RailWidth / 2f, right < safeRight - 0.001f, top, bottom);

        float sideHeight = (halfHeight + EdgeOffset) * 2f;
        float horizontalWidth = (halfWidth + EdgeOffset) * 2f;

        if (LeftWall != null)
        {
            LeftWall.position = new Vector3(left - WallThickness / 2f, center.y, 0f);
            LeftWall.localScale = new Vector3(WallThickness, sideHeight, 1f);
        }

        if (RightWall != null)
        {
            RightWall.position = new Vector3(right + WallThickness / 2f, center.y, 0f);
            RightWall.localScale = new Vector3(WallThickness, sideHeight, 1f);
        }

        if (Roof != null)
        {
            Roof.position = new Vector3(center.x, top + WallThickness / 2f, 0f);
            Roof.localScale = new Vector3(horizontalWidth, WallThickness, 1f);
        }

        if (Bottom != null)
        {
            Bottom.position = new Vector3(center.x, bottom - WallThickness / 2f, 0f);
            Bottom.localScale = new Vector3(horizontalWidth, WallThickness, 1f);
        }
    }

    private void PlaceRail(SpriteRenderer rail, float x, bool visible, float top, float bottom)
    {
        if (rail == null) return;

        rail.enabled = visible;
        if (!visible) return;

        float low = bottom - EdgeOffset;
        rail.transform.position = new Vector3(x, (top + low) / 2f, 0f);
        rail.transform.localScale = new Vector3(RailWidth, top - low, 1f);
    }
}
