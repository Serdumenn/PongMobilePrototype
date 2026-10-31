using UnityEngine;

public static class FieldSpots
{
    public const float Floor = 0.6f;

    public static Vector2 ToWorld(Rect field, Vector2 spot, float edge, float topGap)
    {
        float left = field.xMin + edge;
        float right = Mathf.Max(left, field.xMax - edge);
        float bottom = field.center.y - Floor;
        float top = Mathf.Max(bottom, field.yMax - topGap);
        return new Vector2(Mathf.Lerp(left, right, (Mathf.Clamp(spot.x, -1f, 1f) + 1f) * 0.5f), Mathf.Lerp(bottom, top, Mathf.Clamp01(spot.y)));
    }

    public static float Sway(float home, float amplitude, float phase, Rect field, float edge)
    {
        float x = home + Mathf.Sin(phase) * amplitude;
        return Mathf.Clamp(x, field.xMin + edge, field.xMax - edge);
    }
}
