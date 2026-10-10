using System;

public static class ReviewGate
{
    public const int MinDays = 3;
    public const int MinGames = 10;
    public const int CooldownDays = 30;

    public static bool ShouldAsk(bool newBest, int daysPlayed, int gamesPlayed, DateTime lastAskedUtc, DateTime nowUtc)
    {
        if (!newBest || daysPlayed < MinDays || gamesPlayed < MinGames) return false;
        if (lastAskedUtc == default) return true;
        return (nowUtc - lastAskedUtc).TotalDays >= CooldownDays;
    }

    public static int CountDay(int daysPlayed, string lastDay, string today)
    {
        return lastDay == today ? daysPlayed : daysPlayed + 1;
    }
}
