public sealed class Participant
{
    public Participant(int index, FieldSide side, Paddle paddle)
    {
        Index = index;
        Side = side;
        Paddle = paddle;
        Name = $"Player {index + 1}";
    }

    public int Index { get; }
    public FieldSide Side { get; }
    public Paddle Paddle { get; }
    public string Name { get; set; }
    public CosmeticItem Look { get; set; }
    public int HitStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public int PerfectStreak { get; set; }
    public int Score { get; private set; }
    public int Lives { get; set; }
    public bool Eliminated { get; set; }

    public void BeginRun()
    {
        HitStreak = 0;
        LongestStreak = 0;
        PerfectStreak = 0;
        Score = 0;
        Lives = 0;
        Eliminated = false;
    }

    public void RegisterHit()
    {
        HitStreak++;
        if (HitStreak > LongestStreak) LongestStreak = HitStreak;
    }

    public void BreakStreak()
    {
        HitStreak = 0;
    }

    public void AddPoint()
    {
        Score++;
    }
}
