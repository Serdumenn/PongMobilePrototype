using UnityEngine;

public enum EventKind
{
    Storm,
    GoldRain,
    Targets,
    Giant
}

public sealed class EventSchedule
{
    public const int KindCount = 4;

    private readonly MatchRandom rng;
    private readonly float gapMin;
    private readonly float gapMax;
    private int last = -1;

    public EventSchedule(int seed, float first, float gapMin, float gapMax)
    {
        rng = new MatchRandom(seed);
        this.gapMin = Mathf.Max(1f, gapMin);
        this.gapMax = Mathf.Max(this.gapMin, gapMax);
        NextAt = Mathf.Max(0f, first);
    }

    public float NextAt { get; private set; }

    public EventKind TakeKind()
    {
        int kind = Mathf.Min(KindCount - 1, (int)(rng.Value() * KindCount));
        if (kind == last) kind = (kind + 1 + Mathf.Min(KindCount - 2, (int)(rng.Value() * (KindCount - 1)))) % KindCount;
        last = kind;
        return (EventKind)kind;
    }

    public void Advance(float now)
    {
        NextAt = Mathf.Max(NextAt, now) + rng.Range(gapMin, gapMax);
    }

    public static bool Succeeded(EventKind kind, int progress, int target)
    {
        return kind == EventKind.Storm ? progress >= target : progress >= target && target > 0;
    }
}
