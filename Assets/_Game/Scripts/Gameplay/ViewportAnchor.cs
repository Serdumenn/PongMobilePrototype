using UnityEngine;

public sealed class ViewportAnchor : MonoBehaviour
{
    [SerializeField] private Vector2 ViewportPoint = new Vector2(0.5f, 0.5f);
    [SerializeField, Range(0f, 3f)] private float WidthFraction = 0f;

    private Camera cam;
    private SpriteRenderer sr;
    private int lastScreenW;
    private int lastScreenH;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        cam = Camera.main;
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
            Apply();
    }

    private void Apply()
    {
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 p = cam.ViewportToWorldPoint(new Vector3(ViewportPoint.x, ViewportPoint.y, -cam.transform.position.z));
        transform.position = new Vector3(p.x, p.y, transform.position.z);

        if (WidthFraction <= 0f || sr == null || sr.sprite == null) return;

        float viewWidth = cam.orthographicSize * cam.aspect * 2f;
        float scale = viewWidth * WidthFraction / sr.sprite.bounds.size.x;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
