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

    private Camera cam;
    private int lastScreenW;
    private int lastScreenH;

    private void Awake()
    {
        var noBounce = new PhysicsMaterial2D("WallNoBounce");
        noBounce.bounciness = 0f;
        noBounce.friction = 0f;

        AssignMaterial(LeftWall, noBounce);
        AssignMaterial(RightWall, noBounce);
        AssignMaterial(Roof, noBounce);
        AssignMaterial(Bottom, noBounce);
    }

    private void Start()
    {
        cam = Camera.main;
        Recalculate();
    }

    private static void AssignMaterial(Transform wall, PhysicsMaterial2D mat)
    {
        if (wall == null) return;
        var col = wall.GetComponent<Collider2D>();
        if (col != null) col.sharedMaterial = mat;
    }

    private void Update()
    {
        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
            Recalculate();
    }

    private void Recalculate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = cam.orthographicSize * cam.aspect;

        float sideHeight = (halfHeight + EdgeOffset) * 2f;
        float horizontalWidth = (halfWidth + EdgeOffset) * 2f;

        if (LeftWall != null)
        {
            LeftWall.position = new Vector3(-(halfWidth + WallThickness / 2f), 0f, 0f);
            LeftWall.localScale = new Vector3(WallThickness, sideHeight, 1f);
        }

        if (RightWall != null)
        {
            RightWall.position = new Vector3(halfWidth + WallThickness / 2f, 0f, 0f);
            RightWall.localScale = new Vector3(WallThickness, sideHeight, 1f);
        }

        if (Roof != null)
        {
            Roof.position = new Vector3(0f, halfHeight + WallThickness / 2f, 0f);
            Roof.localScale = new Vector3(horizontalWidth, WallThickness, 1f);
        }

        if (Bottom != null)
        {
            Bottom.position = new Vector3(0f, -(halfHeight + WallThickness / 2f), 0f);
            Bottom.localScale = new Vector3(horizontalWidth, WallThickness, 1f);
        }

        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
    }
}