using UnityEngine;

public static class BallPace
{
    private const float ServeWide = 0.85f;
    private const float ServeNarrow = 0.35f;

    public static Vector2 Velocity(Vector2 direction, float pace, float minSide, float maxSide)
    {
        float rise = Mathf.Abs(direction.y);
        float side = rise > 0.0001f ? Mathf.Abs(direction.x) / rise * pace : maxSide;
        side = Mathf.Clamp(side, Mathf.Min(minSide, maxSide), maxSide);
        return new Vector2(direction.x >= 0f ? side : -side, direction.y >= 0f ? pace : -pace);
    }

    public static Vector2 Aim(float offset, Vector2 inward, Vector2 axis, Vector2 reflected, float spread, float carry)
    {
        float forward = Mathf.Abs(Vector2.Dot(reflected, inward));
        float drift = forward > 0.0001f ? Vector2.Dot(reflected, axis) / forward : 0f;
        float side = Mathf.Clamp(offset, -1f, 1f) * spread + drift * Mathf.Clamp01(carry);
        if (Mathf.Abs(side) < 0.0001f) side = drift < 0f ? -0.0001f : 0.0001f;
        return (inward + axis * side).normalized;
    }

    public static Vector2 ServeDirection(float sign, float roll, float pace, float maxSide)
    {
        float side = Mathf.Lerp(ServeWide, ServeNarrow, Mathf.Clamp01(roll)) * maxSide;
        return new Vector2(sign >= 0f ? side : -side, Mathf.Max(0.01f, pace)).normalized;
    }
}
