using System;

public static class ShopClock
{
    private static readonly DateTime Epoch = new DateTime(2000, 1, 1);

    public static int DayIndex(DateTime local)
    {
        return (int)(local.Date - Epoch).TotalDays;
    }

    public static int SecondsToMidnight(DateTime local)
    {
        return (int)Math.Ceiling((local.Date.AddDays(1) - local).TotalSeconds);
    }

    public static uint Hash(string text)
    {
        uint hash = 2166136261;
        if (text == null) return hash;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }
}
