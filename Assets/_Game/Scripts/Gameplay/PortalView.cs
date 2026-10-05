using System.Collections;
using UnityEngine;

public sealed class PortalView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer Ring;
    [SerializeField] private float Width = 3.2f;
    [SerializeField] private float LeaveSeconds = 0.45f;
    [SerializeField] private float ArriveSeconds = 0.5f;

    private Coroutine running;

    private void Awake()
    {
        if (Ring == null) Ring = GetComponent<SpriteRenderer>();
        if (Ring != null) Ring.enabled = false;
    }

    public void Leave(Vector2 point)
    {
        Play(point, LeaveSeconds, false);
    }

    public void Arrive(Vector2 point)
    {
        Play(point, ArriveSeconds, true);
    }

    public void Hide()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        if (Ring != null) Ring.enabled = false;
    }

    private void Play(Vector2 point, float seconds, bool arriving)
    {
        if (Ring == null || Ring.sprite == null) return;

        Hide();
        running = StartCoroutine(Animate(point, Mathf.Max(0.05f, seconds), arriving));
    }

    private IEnumerator Animate(Vector2 point, float seconds, bool arriving)
    {
        transform.position = new Vector3(point.x, point.y, 0f);
        float baseScale = Width / Mathf.Max(0.01f, Ring.sprite.bounds.size.x);
        Color color = Ring.color;
        Ring.enabled = true;

        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds;
            float ease = 1f - (1f - k) * (1f - k);
            float scale = arriving ? Mathf.Lerp(0.35f, 1.05f, ease) : Mathf.Lerp(1f, 1.35f, ease);
            float alpha = arriving ? (k < 0.75f ? 1f : (1f - k) / 0.25f) : 1f - k;

            transform.localScale = new Vector3(baseScale * scale, baseScale * scale, 1f);
            Ring.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        Ring.color = new Color(color.r, color.g, color.b, 1f);
        Ring.enabled = false;
        running = null;
    }
}
