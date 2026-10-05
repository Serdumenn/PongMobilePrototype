using System.Collections.Generic;
using UnityEngine;

public sealed class FieldLayout : MonoBehaviour
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

    [Header("Court")]
    [SerializeField] private float CourtMargin = 0.8f;
    [SerializeField] private float CornerSize = 0.6f;
    [SerializeField] private Color CourtColor = new Color(1f, 0.973f, 0.933f, 1f);
    [SerializeField] private Color CornerColor = new Color(0.941f, 0.851f, 0.753f, 1f);
    [SerializeField] private Color MidlineColor = new Color(0.91f, 0.847f, 0.776f, 1f);
    [SerializeField] private Color ClosedSideColor = new Color(0.851f, 0.796f, 0.733f, 1f);
    [SerializeField] private float ClosedSideWidth = 0.16f;
    [SerializeField] private int MidlineDashes = 11;

    [Header("Portal")]
    [SerializeField] private float PortalTopInset = 2.4f;
    [SerializeField] private float PortalLineHeight = 0.12f;
    [SerializeField] private Color PortalColor = new Color(0.18f, 0.769f, 0.714f, 0.55f);

    private readonly Dictionary<FieldSide, Goal> goals = new Dictionary<FieldSide, Goal>();
    private readonly Dictionary<FieldSide, bool> closedSides = new Dictionary<FieldSide, bool>();
    private readonly List<Transform> corners = new List<Transform>();
    private readonly Dictionary<FieldSide, Transform> closedBars = new Dictionary<FieldSide, Transform>();
    private readonly List<Transform> dashes = new List<Transform>();
    private Transform portalLine;
    private SpriteRenderer court;
    private Camera cam;
    private int lastScreenW;
    private int lastScreenH;
    private Rect lastSafeArea;

    public Rect Field { get; private set; }
    public FieldTopology Topology { get; private set; } = FieldTopology.Solo;
    public float Corner => Topology == FieldTopology.FourSides ? CornerSize : 0f;

    private void Awake()
    {
        var noBounce = Ball.GetSharedNoBounce();

        foreach (var wall in new[] { LeftWall, RightWall, Roof, Bottom })
        {
            AssignMaterial(wall, noBounce);
            HideRenderer(wall);
        }

        RegisterGoal(Bottom, FieldSide.Bottom);
        RegisterGoal(Roof, FieldSide.Top);
        RegisterGoal(LeftWall, FieldSide.Left);
        RegisterGoal(RightWall, FieldSide.Right);

        ApplyGoals();
        Recalculate();
    }

    private void RegisterGoal(Transform wall, FieldSide side)
    {
        if (wall == null) return;
        var goal = wall.GetComponent<Goal>();
        if (goal != null) goals[side] = goal;
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

    public void SetTopology(FieldTopology topology)
    {
        Topology = topology;
        closedSides.Clear();
        ApplyGoals();
        Recalculate();
    }

    public void CloseSide(FieldSide side)
    {
        closedSides[side] = true;
        ApplyGoals();
        UpdateCourt();
    }

    public bool IsOpen(FieldSide side)
    {
        return goals.TryGetValue(side, out var goal) && goal.IsOpen;
    }

    public Vector2 HomeFor(FieldSide side, float inset)
    {
        Rect f = Field;
        return side switch
        {
            FieldSide.Top => new Vector2(f.center.x, f.yMax - inset),
            FieldSide.Left => new Vector2(f.xMin + inset, f.center.y),
            FieldSide.Right => new Vector2(f.xMax - inset, f.center.y),
            _ => new Vector2(f.center.x, f.yMin + inset)
        };
    }

    public Vector2 RangeFor(FieldSide side)
    {
        Rect f = Field;
        float corner = Corner;
        return side.MovesHorizontally()
            ? new Vector2(f.xMin + corner, f.xMax - corner)
            : new Vector2(f.yMin + corner, f.yMax - corner);
    }

    private bool SideOpen(FieldSide side)
    {
        if (closedSides.ContainsKey(side)) return false;

        return Topology switch
        {
            FieldTopology.TopBottom => side == FieldSide.Bottom || side == FieldSide.Top,
            FieldTopology.Portal => side == FieldSide.Bottom || side == FieldSide.Top,
            FieldTopology.Live => side == FieldSide.Bottom || side == FieldSide.Top,
            FieldTopology.FourSides => true,
            _ => side == FieldSide.Bottom
        };
    }

    private void ApplyGoals()
    {
        foreach (var pair in goals)
        {
            pair.Value.SetOpen(SideOpen(pair.Key));
            pair.Value.Portal = Topology == FieldTopology.Portal && pair.Key == FieldSide.Top;
        }
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
        Vector3 safeMin = cam.ScreenToWorldPoint(new Vector3(safe.xMin, safe.yMin, depth));
        Vector3 safeMax = cam.ScreenToWorldPoint(new Vector3(safe.xMax, safe.yMax, depth));

        if (Topology == FieldTopology.FourSides)
        {
            float side = Mathf.Max(1f, Mathf.Min(safeMax.x - safeMin.x, safeMax.y - safeMin.y) - CourtMargin * 2f);
            Vector2 mid = new Vector2((safeMin.x + safeMax.x) / 2f, (safeMin.y + safeMax.y) / 2f);
            Field = new Rect(mid.x - side / 2f, mid.y - side / 2f, side, side);
        }
        else if (Topology == FieldTopology.Live)
        {
            float top = safeMax.y - PortalTopInset;
            float bottom = center.y - halfHeight;
            float maxHalfWidth = MaxFieldAspect > 0f ? halfHeight * MaxFieldAspect : float.MaxValue;
            float left = Mathf.Max(safeMin.x, center.x - maxHalfWidth);
            float right = Mathf.Min(safeMax.x, center.x + maxHalfWidth);
            float aspect = MaxFieldAspect > 0f ? MaxFieldAspect : 0.5625f;
            float width = Mathf.Max(1f, Mathf.Min(right - left, (top - bottom) * aspect));
            float mid = (left + right) / 2f;
            Field = new Rect(mid - width / 2f, bottom, width, width / aspect);
        }
        else
        {
            float top = Topology == FieldTopology.Portal ? safeMax.y - PortalTopInset : safeMax.y;
            float bottom = center.y - halfHeight;
            float maxHalfWidth = MaxFieldAspect > 0f ? halfHeight * MaxFieldAspect : float.MaxValue;
            float left = Mathf.Max(safeMin.x, center.x - maxHalfWidth);
            float right = Mathf.Min(safeMax.x, center.x + maxHalfWidth);
            Field = Rect.MinMaxRect(left, bottom, right, top);
        }

        Rect f = Field;
        bool rails = Topology != FieldTopology.FourSides;
        PlaceRail(LeftRail, f.xMin - RailWidth / 2f, rails && f.xMin > safeMin.x + 0.001f, f.yMax, f.yMin);
        PlaceRail(RightRail, f.xMax + RailWidth / 2f, rails && f.xMax < safeMax.x - 0.001f, f.yMax, f.yMin);

        if (Topology == FieldTopology.FourSides)
        {
            float span = f.width;
            Place(LeftWall, new Vector2(f.xMin - WallThickness / 2f, f.center.y), new Vector2(WallThickness, span + WallThickness * 2f));
            Place(RightWall, new Vector2(f.xMax + WallThickness / 2f, f.center.y), new Vector2(WallThickness, span + WallThickness * 2f));
            Place(Roof, new Vector2(f.center.x, f.yMax + WallThickness / 2f), new Vector2(span, WallThickness));
            Place(Bottom, new Vector2(f.center.x, f.yMin - WallThickness / 2f), new Vector2(span, WallThickness));
        }
        else
        {
            float sideHeight = (halfHeight + EdgeOffset) * 2f;
            float horizontalWidth = (halfWidth + EdgeOffset) * 2f;
            Place(LeftWall, new Vector2(f.xMin - WallThickness / 2f, center.y), new Vector2(WallThickness, sideHeight));
            Place(RightWall, new Vector2(f.xMax + WallThickness / 2f, center.y), new Vector2(WallThickness, sideHeight));
            Place(Roof, new Vector2(center.x, f.yMax + WallThickness / 2f), new Vector2(horizontalWidth, WallThickness));
            Place(Bottom, new Vector2(center.x, f.yMin - WallThickness / 2f), new Vector2(horizontalWidth, WallThickness));
        }

        UpdateCourt();
        UpdateMidline();
        UpdatePortal();
    }

    private void UpdatePortal()
    {
        bool show = Topology == FieldTopology.Portal;
        if (!show && portalLine == null) return;

        if (portalLine == null) portalLine = CreateBlock("portal_line", PortalColor, -44, false);
        portalLine.gameObject.SetActive(show);
        if (!show) return;

        Rect f = Field;
        portalLine.position = new Vector3(f.center.x, f.yMax, 0f);
        portalLine.localScale = new Vector3(f.width, PortalLineHeight, 1f);
    }

    private static void Place(Transform wall, Vector2 position, Vector2 size)
    {
        if (wall == null) return;
        wall.position = new Vector3(position.x, position.y, 0f);
        wall.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void UpdateCourt()
    {
        bool show = Topology == FieldTopology.FourSides;
        if (!show && court == null) return;

        if (court == null) court = CreateBlock("court", CourtColor, -60, false).GetComponent<SpriteRenderer>();
        while (show && corners.Count < 4) corners.Add(CreateBlock("corner", CornerColor, -40, true));

        court.enabled = show;
        foreach (var corner in corners) corner.gameObject.SetActive(show);
        UpdateClosedBars(show);
        if (!show) return;

        Rect f = Field;
        court.transform.position = new Vector3(f.center.x, f.center.y, 0f);
        court.transform.localScale = new Vector3(f.width, f.height, 1f);

        float c = CornerSize;
        Vector2[] spots =
        {
            new Vector2(f.xMin + c / 2f, f.yMin + c / 2f),
            new Vector2(f.xMax - c / 2f, f.yMin + c / 2f),
            new Vector2(f.xMin + c / 2f, f.yMax - c / 2f),
            new Vector2(f.xMax - c / 2f, f.yMax - c / 2f)
        };
        for (int i = 0; i < 4; i++)
        {
            corners[i].position = new Vector3(spots[i].x, spots[i].y, 0f);
            corners[i].localScale = new Vector3(c, c, 1f);
        }
    }

    private void UpdateClosedBars(bool court)
    {
        foreach (FieldSide side in System.Enum.GetValues(typeof(FieldSide)))
        {
            bool show = court && closedSides.ContainsKey(side);
            if (!show && !closedBars.ContainsKey(side)) continue;
            if (!closedBars.TryGetValue(side, out var bar)) closedBars[side] = bar = CreateBlock($"closed_{side}", ClosedSideColor, -42, false);

            bar.gameObject.SetActive(show);
            if (!show) continue;

            Rect f = Field;
            float w = ClosedSideWidth;
            switch (side)
            {
                case FieldSide.Top: Size(bar, new Vector2(f.center.x, f.yMax - w / 2f), new Vector2(f.width, w)); break;
                case FieldSide.Left: Size(bar, new Vector2(f.xMin + w / 2f, f.center.y), new Vector2(w, f.height)); break;
                case FieldSide.Right: Size(bar, new Vector2(f.xMax - w / 2f, f.center.y), new Vector2(w, f.height)); break;
                default: Size(bar, new Vector2(f.center.x, f.yMin + w / 2f), new Vector2(f.width, w)); break;
            }
        }
    }

    private static void Size(Transform block, Vector2 position, Vector2 size)
    {
        block.position = new Vector3(position.x, position.y, 0f);
        block.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void UpdateMidline()
    {
        bool show = Topology == FieldTopology.TopBottom || Topology == FieldTopology.Live;
        if (!show && dashes.Count == 0) return;

        while (show && dashes.Count < MidlineDashes) dashes.Add(CreateBlock("dash", MidlineColor, -45, false));
        foreach (var dash in dashes) dash.gameObject.SetActive(show);
        if (!show) return;

        Rect f = Field;
        float step = f.width / MidlineDashes;
        for (int i = 0; i < dashes.Count; i++)
        {
            dashes[i].position = new Vector3(f.xMin + step * (i + 0.5f), f.center.y, 0f);
            dashes[i].localScale = new Vector3(step * 0.55f, 0.06f, 1f);
        }
    }

    private Transform CreateBlock(string name, Color color, int order, bool solid)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LeftRail != null ? LeftRail.sprite : null;
        sr.color = color;
        sr.sortingOrder = order;

        if (solid)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.sharedMaterial = Ball.GetSharedNoBounce();
        }

        return go.transform;
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
