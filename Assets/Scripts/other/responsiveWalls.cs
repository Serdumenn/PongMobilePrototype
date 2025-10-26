using UnityEngine;

public class responsiveWalls : MonoBehaviour
{
    public GameObject solWall;
    public GameObject sagWall;
    public float wallThickness = 0.5f;

    private Camera cam;
    private int lastW, lastH;

    void Start()
    {
        cam = Camera.main;
        UpdateSideWalls();
    }

    void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH)
            UpdateSideWalls();
    }

    void UpdateSideWalls()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || solWall == null || sagWall == null) return;

        float distance = Mathf.Abs(cam.transform.position.z);
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 topRight   = cam.ViewportToWorldPoint(new Vector3(1, 1, distance));

        float midY = (bottomLeft.y + topRight.y) / 2f;

        solWall.transform.position = new Vector3(bottomLeft.x - wallThickness / 2f, midY, 0f);
        sagWall.transform.position = new Vector3(topRight.x  + wallThickness / 2f, midY, 0f);

        float screenHeight = topRight.y - bottomLeft.y;

        var solCollider = solWall.GetComponent<BoxCollider2D>();
        var sagCollider = sagWall.GetComponent<BoxCollider2D>();

        if (solCollider) solCollider.size = new Vector2(wallThickness, screenHeight);
        if (sagCollider) sagCollider.size = new Vector2(wallThickness, screenHeight);

        lastW = Screen.width;
        lastH = Screen.height;
    }
}