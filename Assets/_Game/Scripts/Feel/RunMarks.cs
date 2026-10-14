public static class RunMarks
{
    public static bool IsMilestone(int streak, int[] marks, int step)
    {
        if (streak <= 0 || marks == null || marks.Length == 0) return false;

        int last = 0;
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] == streak) return true;
            if (marks[i] > last) last = marks[i];
        }

        return step > 0 && streak > last && (streak - last) % step == 0;
    }

    public static bool BeatsBest(int score, int previousBest, bool ranked)
    {
        return ranked && previousBest > 0 && score > previousBest;
    }

    public static bool NearMiss(float offset, float edge, int streak, int minStreak)
    {
        return streak >= minStreak && System.Math.Abs(offset) >= edge;
    }
}
