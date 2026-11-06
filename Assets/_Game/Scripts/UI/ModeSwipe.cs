using UnityEngine;

public static class ModeSwipe
{
    public const float CommitShare = 0.25f;
    public const float FlickSpeed = 900f;
    public const float MinFlick = 24f;
    public const float TapSlop = 12f;

    public static int Decide(float drag, float velocity, float spacing)
    {
        float size = Mathf.Abs(drag);
        bool far = size >= Mathf.Abs(spacing) * CommitShare;
        bool flick = size >= MinFlick && Mathf.Abs(velocity) >= FlickSpeed && Mathf.Sign(velocity) == Mathf.Sign(drag);
        if (!far && !flick) return 0;
        return drag < 0f ? 1 : -1;
    }

    public static bool IsTap(float drag)
    {
        return Mathf.Abs(drag) < TapSlop;
    }
}
