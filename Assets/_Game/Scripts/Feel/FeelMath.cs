using UnityEngine;

public static class FeelMath
{
    private const float Settled = 0.002f;
    private const float AcrossShare = 0.7f;
    private const float MaxStep = 0.05f;

    public static float Step(float delta)
    {
        return Mathf.Clamp(delta, 0f, MaxStep);
    }

    public static float Spring(float time, float frequency, float damping)
    {
        if (time <= 0f) return 1f;

        float envelope = Mathf.Exp(-damping * time);
        return envelope < Settled ? 0f : envelope * Mathf.Cos(2f * Mathf.PI * frequency * time);
    }

    public static Vector2 Squash(Vector2 normal, float amount)
    {
        amount = Mathf.Clamp(amount, 0f, 0.9f);
        float along = 1f - amount;
        float across = 1f + amount * AcrossShare;
        return Mathf.Abs(normal.y) >= Mathf.Abs(normal.x) ? new Vector2(across, along) : new Vector2(along, across);
    }

    public static Vector2 Blend(Vector2 squashed, float spring)
    {
        return Vector2.one + (squashed - Vector2.one) * spring;
    }

    public static Vector2 Fan(Vector2 normal, int index, int count, float spreadDegrees)
    {
        float step = count > 1 ? (float)index / (count - 1) - 0.5f : 0f;
        float angle = step * spreadDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Vector2 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector2.up;
        return new Vector2(n.x * cos - n.y * sin, n.x * sin + n.y * cos);
    }

    public static float Warmth(float heat)
    {
        return Mathf.Sqrt(Mathf.Clamp01(heat));
    }

    public static Color Warm(Color color, Color hot, float amount)
    {
        var mixed = Color.Lerp(color, hot, Mathf.Clamp01(amount));
        mixed.a = color.a;
        return mixed;
    }

    public static Vector2 Drift(float time, float phase, Vector2 reach)
    {
        return new Vector2(Mathf.Sin(time * 0.9f + phase) * reach.x, Mathf.Cos(time * 0.7f + phase * 1.7f) * reach.y);
    }

    public static float Jitter(int seed)
    {
        float value = Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }
}
