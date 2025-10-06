using System.Collections;
using UnityEngine;

public class slider : MonoBehaviour
{
    [Range(1f, 10f)] public float speed = 5f;
    [Range(1f, 10f)] public float boundaryLimit = 7f;
    public GameObject upperPaddle;
    public GameObject lowerPaddle;
    public float racketHalfWidth;

    private Camera cam;
    private float halfScreenHeight;
    private int cachedW, cachedH;
    private Transform upT, lowT;

    void Awake()
    {
        cam = Camera.main;
        CachePaddles();
        CacheScreen();
        SetBoundaryLimit();
    }

    void Update()
    {
        // Ekran döner/çözünürlük değişir → sınırları güncelle
        if (Screen.width != cachedW || Screen.height != cachedH)
        {
            CacheScreen();
            SetBoundaryLimit();
        }

        HandleTouches();
    }

    void CachePaddles()
    {
        if (upperPaddle) upT = upperPaddle.transform;
        if (lowerPaddle) lowT = lowerPaddle.transform;
    }

    void CacheScreen()
    {
        cachedW = Screen.width;
        cachedH = Screen.height;
        halfScreenHeight = cachedH / 2f;
    }

    void HandleTouches()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || Input.touchCount == 0) return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                continue;

            Vector3 sp = new Vector3(t.position.x, t.position.y, 0f);
            Vector2 world = cam.ScreenToWorldPoint(sp);

            if (t.position.y > halfScreenHeight && upT != null)
                MovePaddle(upT, world);
            else if (t.position.y < halfScreenHeight && lowT != null)
                MovePaddle(lowT, world);
        }
    }

    void MovePaddle(Transform p, Vector2 touchWorld)
    {
        Vector2 target = new Vector2(
            Mathf.Clamp(touchWorld.x, -boundaryLimit, boundaryLimit),
            p.position.y
        );

        // Snappy yerine pürüzsüz:
        Vector2 next = Vector2.MoveTowards((Vector2)p.position, target, speed * Time.deltaTime);
        p.position = new Vector3(next.x, next.y, p.position.z);
    }

    void SetBoundaryLimit()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float half = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
        boundaryLimit = Mathf.Max(0f, half - racketHalfWidth);
    }
}
