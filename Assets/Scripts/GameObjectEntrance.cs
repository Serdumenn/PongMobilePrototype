using System.Collections;
using UnityEngine;

public sealed class GameObjectEntrance : MonoBehaviour
{
    [SerializeField] private float Duration = 0.30f;
    [SerializeField] private float Delay = 0f;
    [SerializeField] private float OffScreenOffset = 3f;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Coroutine activeEntrance;
    private bool initialized;

    private void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void PlayEntrance(Direction from)
    {
        EnsureInit();
        if (activeEntrance != null) StopCoroutine(activeEntrance);
        activeEntrance = StartCoroutine(RunEntrance(from));
    }

    public void ResetToTarget(Vector3 target)
    {
        EnsureInit();
        transform.position = target;
        SetAlpha(1f);
    }

    private IEnumerator RunEntrance(Direction from)
    {
        transform.rotation = Quaternion.identity;

        Vector3 targetPosition = transform.position;

        bool hadSimulated = false;
        if (rb != null)
        {
            hadSimulated = rb.simulated;
            rb.simulated = false;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        float camHalfWidth = 5f;
        Camera cam = Camera.main;
        if (cam != null) camHalfWidth = cam.orthographicSize * cam.aspect;

        float offsetX = from == Direction.Left
            ? -(camHalfWidth + OffScreenOffset)
            : (camHalfWidth + OffScreenOffset);

        Vector3 startPos = targetPosition;
        startPos.x = targetPosition.x + offsetX;

        transform.position = startPos;
        SetAlpha(0f);

        if (Delay > 0f)
            yield return WaitUnscaled(Delay);

        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / Duration));

            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            SetAlpha(t);

            yield return null;
        }

        transform.position = targetPosition;
        SetAlpha(1f);

        if (rb != null) rb.simulated = hadSimulated;

        activeEntrance = null;
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetAlpha(float a)
    {
        if (sr == null) return;
        sr.enabled = true;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public enum Direction
    {
        Left,
        Right
    }
}
