using UnityEngine;

public sealed class ViewportAnchor : MonoBehaviour
{
    [SerializeField] private Vector2 ViewportPoint = new Vector2(0.5f, 0.5f);
    [SerializeField, Range(0f, 3f)] private float WidthFraction = 0f;

    private Camera cam;
    private SpriteRenderer sr;
    private int lastScreenW;
    private int lastScreenH;
    private Vector3 anchoredPosition;
    private Vector3 anchoredScale = Vector3.one;
    private Vector2 sway;
    private float swell = 1f;
    private bool anchored;

    public Vector3 AnchoredPosition => anchoredPosition;
    public Vector3 AnchoredScale => anchoredScale;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anchoredPosition = transform.position;
        anchoredScale = transform.localScale;
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
        anchoredPosition = new Vector3(p.x, p.y, transform.position.z);

        if (WidthFraction > 0f && sr != null && sr.sprite != null)
        {
            float viewWidth = cam.orthographicSize * cam.aspect * 2f;
            float scale = viewWidth * WidthFraction / sr.sprite.bounds.size.x;
            anchoredScale = new Vector3(scale, scale, 1f);
        }

        anchored = true;
        Place();
    }

    public void SetSway(Vector2 offset, float scale)
    {
        sway = offset;
        swell = Mathf.Max(0.01f, scale);
        if (anchored) Place();
    }

    private void Place()
    {
        transform.position = new Vector3(anchoredPosition.x + sway.x, anchoredPosition.y + sway.y, anchoredPosition.z);
        transform.localScale = new Vector3(anchoredScale.x * swell, anchoredScale.y * swell, anchoredScale.z);
    }
}
