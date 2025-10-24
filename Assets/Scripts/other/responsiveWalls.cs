using UnityEngine;

public class responsiveWalls : MonoBehaviour
{
    public GameObject solWall;
    public GameObject sagWall;
    public float wallThickness = 0.5f;

    void Start()
    {
        UpdateSideWalls();
    }

    void UpdateSideWalls()
    {
        float distance = Mathf.Abs(Camera.main.transform.position.z);

        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 topRight   = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, distance));

        float midY = (bottomLeft.y + topRight.y) / 2;

        solWall.transform.position = new Vector3(bottomLeft.x - wallThickness / 2, midY, 0);
        sagWall.transform.position = new Vector3(topRight.x + wallThickness / 2, midY, 0);

        float screenHeight = topRight.y - bottomLeft.y;

        BoxCollider2D solCollider = solWall.GetComponent<BoxCollider2D>();
        BoxCollider2D sagCollider = sagWall.GetComponent<BoxCollider2D>();

        if(solCollider != null)
            solCollider.size = new Vector2(wallThickness, screenHeight);
        if(sagCollider != null)
            sagCollider.size = new Vector2(wallThickness, screenHeight);
    }
}