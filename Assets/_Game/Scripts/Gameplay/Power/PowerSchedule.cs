using UnityEngine;

public enum PowerKind
{
    BigPaddle,
    SlowBall,
    DoublePoints,
    Shield
}

public sealed class PowerSchedule
{
    public const int KindCount = 4;

    private readonly MatchRandom rng;
    private readonly float gapMin;
    private readonly float gapMax;
    private int last = -1;

    public PowerSchedule(int seed, float first, float gapMin, float gapMax)
    {
        rng = new MatchRandom(seed);
        this.gapMin = Mathf.Max(0.1f, gapMin);
        this.gapMax = Mathf.Max(this.gapMin, gapMax);
        NextAt = Mathf.Max(0f, first);
    }

    public float NextAt { get; private set; }

    public PowerKind TakeKind()
    {
        int kind = Mathf.Min(KindCount - 1, (int)(rng.Value() * KindCount));
        if (kind == last) kind = (kind + 1 + Mathf.Min(KindCount - 2, (int)(rng.Value() * (KindCount - 1)))) % KindCount;
        last = kind;
        return (PowerKind)kind;
    }

    public Vector2 TakeSpot(Rect area)
    {
        return new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax));
    }

    public void Advance()
    {
        NextAt += rng.Range(gapMin, gapMax);
    }
}
