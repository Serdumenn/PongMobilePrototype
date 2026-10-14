using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraPunch : MonoBehaviour
{
    [SerializeField] private float Frequency = 5f;
    [SerializeField] private float Damping = 16f;
    [SerializeField] private float MaxOffset = 0.15f;

    private Vector2 direction;
    private float amplitude;
    private float elapsed = -1f;
    private Vector3 restPosition;
    private bool displaced;

    public bool Animating => elapsed >= 0f;
    public Vector2 Offset => elapsed < 0f ? Vector2.zero : direction * (amplitude * FeelMath.Spring(elapsed, Frequency, Damping));

    public void Kick(Vector2 toward, float amount)
    {
        if (amount <= 0f || toward.sqrMagnitude < 0.0001f) return;

        direction = toward.normalized;
        amplitude = Mathf.Min(amount, MaxOffset);
        elapsed = 0f;
    }

    private void Update()
    {
        if (elapsed < 0f) return;

        elapsed += FeelMath.Step(Time.unscaledDeltaTime);
        if (FeelMath.Spring(elapsed, Frequency, Damping) == 0f) elapsed = -1f;
    }

    private void OnPreCull()
    {
        Vector2 offset = Offset;
        if (offset == Vector2.zero) return;

        restPosition = transform.localPosition;
        transform.localPosition = restPosition + new Vector3(offset.x, offset.y, 0f);
        displaced = true;
    }

    private void OnPostRender()
    {
        Restore();
    }

    private void OnDisable()
    {
        elapsed = -1f;
        Restore();
    }

    private void Restore()
    {
        if (!displaced) return;

        transform.localPosition = restPosition;
        displaced = false;
    }
}
