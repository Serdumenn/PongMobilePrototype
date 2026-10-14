using UnityEngine;

public sealed class BackdropFeel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Ball Ball;
    [SerializeField] private ViewportAnchor[] Blobs;

    [Header("Drift")]
    [SerializeField] private Vector2 Reach = new Vector2(0.16f, 0.22f);
    [SerializeField] private float DriftSpeed = 0.45f;
    [SerializeField, Range(0f, 0.1f)] private float Breath = 0.015f;
    [SerializeField, Range(0f, 3f)] private float HeatBoost = 1.5f;

    [Header("Pulse")]
    [SerializeField, Range(0f, 0.2f)] private float PulseCalm = 0.025f;
    [SerializeField, Range(0f, 0.2f)] private float PulseHot = 0.07f;
    [SerializeField] private float Frequency = 2.2f;
    [SerializeField] private float Damping = 7f;
    [SerializeField] private float HeatFade = 1.2f;

    private const float PhaseGap = 2.4f;
    private const float BreathRate = 1.3f;
    private const float HeatReach = 0.5f;

    private Ball listening;
    private float clock;
    private float pulse = -1f;
    private float heat;

    public float Heat => heat;
    public bool Pulsing => pulse >= 0f;
    public Vector2 MaxReach => Reach * (1f + HeatReach);

    private void OnEnable()
    {
        listening = Ball;
        if (listening != null) listening.PaddleStruck += OnPaddleStruck;
    }

    private void OnDisable()
    {
        if (listening != null) listening.PaddleStruck -= OnPaddleStruck;
        listening = null;
        pulse = -1f;
        heat = 0f;
        Place(0f);
    }

    private void Update()
    {
        float step = FeelMath.Step(Time.unscaledDeltaTime);
        float target = Ball != null && Ball.InPlay ? FeelMath.Warmth(Ball.Heat) : 0f;
        heat = Mathf.MoveTowards(heat, target, HeatFade * step);
        clock += step * DriftSpeed * (1f + HeatBoost * heat);

        float spring = 0f;
        if (pulse >= 0f)
        {
            spring = FeelMath.Spring(pulse, Frequency, Damping);
            pulse = spring == 0f ? -1f : pulse + step;
        }

        Place(spring);
    }

    public void Pulse()
    {
        pulse = 0f;
    }

    private void OnPaddleStruck(Paddle paddle, float offset)
    {
        Pulse();
    }

    private void Place(float spring)
    {
        if (Blobs == null) return;

        float amount = Mathf.Lerp(PulseCalm, PulseHot, heat) * spring;
        Vector2 reach = Reach * (1f + HeatReach * heat);
        for (int i = 0; i < Blobs.Length; i++)
        {
            if (Blobs[i] == null) continue;

            float phase = i * PhaseGap;
            float swell = 1f + Breath * Mathf.Sin(clock * BreathRate + phase) + amount * (i % 2 == 0 ? 1f : 0.7f);
            Blobs[i].SetSway(FeelMath.Drift(clock, phase, reach), swell);
        }
    }
}
