using UnityEngine;

[RequireComponent(typeof(Paddle))]
public sealed class PaddleFeel : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform Visual;

    [Header("Kick")]
    [SerializeField] private float KickDistance = 0.14f;
    [SerializeField, Range(0f, 0.4f)] private float KickSquash = 0.16f;
    [SerializeField] private float Frequency = 3.6f;
    [SerializeField] private float Damping = 13f;

    [SerializeField, HideInInspector] private Vector3 restPosition;
    [SerializeField, HideInInspector] private Vector3 restScale = Vector3.one;
    [SerializeField, HideInInspector] private bool restKnown;

    private float strength;
    private float elapsed = -1f;

    public bool Animating => elapsed >= 0f;

    private void Awake()
    {
        if (Visual == null)
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null && sprite.transform != transform) Visual = sprite.transform;
        }

        if (Visual == null) return;

        if (restKnown)
        {
            Visual.localPosition = restPosition;
            Visual.localScale = restScale;
        }
        else
        {
            restPosition = Visual.localPosition;
            restScale = Visual.localScale;
        }

        restKnown = true;
    }

    public void Kick(float amount = 1f)
    {
        if (Visual == null) return;

        strength = Mathf.Clamp(amount, 0f, 2f);
        elapsed = 0f;
    }

    private void Update()
    {
        if (elapsed < 0f || Visual == null) return;

        float spring = FeelMath.Spring(elapsed, Frequency, Damping);
        elapsed = spring == 0f ? -1f : elapsed + FeelMath.Step(Time.unscaledDeltaTime);

        float amount = spring * strength;
        Visual.localPosition = restPosition + Vector3.down * (KickDistance * amount);
        Visual.localScale = new Vector3(restScale.x, restScale.y * (1f - KickSquash * amount), restScale.z);
    }

    private void OnDisable()
    {
        elapsed = -1f;
        if (Visual == null) return;

        Visual.localPosition = restPosition;
        Visual.localScale = restScale;
    }
}
