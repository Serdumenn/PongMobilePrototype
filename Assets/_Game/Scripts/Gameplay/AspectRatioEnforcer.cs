using UnityEngine;

public sealed class AspectRatioEnforcer : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private float TargetAspect = 0.5625f;

    private Camera cam;
    private int lastScreenW;
    private int lastScreenH;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

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

        if (cam == null) return;

        float deviceAspect = (float)Screen.width / Screen.height;

        if (deviceAspect <= TargetAspect)
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        float normalizedWidth = TargetAspect / deviceAspect;
        float barWidth = (1f - normalizedWidth) / 2f;

        cam.rect = new Rect(barWidth, 0f, normalizedWidth, 1f);
    }

    private void OnValidate()
    {
        if (TargetAspect <= 0f) TargetAspect = 0.5625f;
    }
}
