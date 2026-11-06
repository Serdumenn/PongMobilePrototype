using UnityEngine;

public static class BackdropMotion
{
    private const float RallyEdge = 0.1f;
    private const float WobbleReach = 0.12f;

    public static float Wrap(float value, float min, float max)
    {
        float span = max - min;
        if (span <= 0f) return min;
        return min + Mathf.Repeat(value - min, span);
    }

    public static float Twinkle(float time, float phase)
    {
        float wave = Mathf.Sin(time * 2.6f + phase);
        return wave * wave;
    }

    public static Vector2 Rally(float time, float period)
    {
        float span = 1f - RallyEdge * 2f;
        float beat = period > 0f ? time / period : time;
        return new Vector2(RallyEdge + span * Mathf.PingPong(beat * 1.7f, 1f), RallyEdge + span * Mathf.PingPong(beat, 1f));
    }

    public static float Pulse(float time, float period)
    {
        return period > 0f ? Mathf.Repeat(time, period) / period : 0f;
    }

    public static Vector2 Wobble(float time, float phase)
    {
        return new Vector2(1f + WobbleReach * Mathf.Sin(time * 0.9f + phase), 1f + WobbleReach * Mathf.Sin(time * 0.7f + phase * 1.3f + 1f));
    }
}
