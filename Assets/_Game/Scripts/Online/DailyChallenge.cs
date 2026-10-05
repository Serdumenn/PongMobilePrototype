using System;
using System.Globalization;
using UnityEngine;

public static class DailyChallenge
{
    private const string BestKey = "DailyBest";
    private const string DayKeyPref = "DailyBestDay";

    public static string DayKey(DateTime utcNow)
    {
        return utcNow.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static int SeedFor(DateTime utcNow)
    {
        return unchecked((int)PlayerNames.Hash("pingi-daily-" + DayKey(utcNow)));
    }

    public static string Label(DateTime utcNow)
    {
        return Loc.T("{0:MMM d}", utcNow.Date);
    }

    public static TimeSpan ResetsIn(DateTime utcNow)
    {
        return utcNow.Date.AddDays(1) - utcNow;
    }

    public static string FormatResetsIn(TimeSpan left)
    {
        if (left.TotalHours >= 1) return Loc.T("{0} h", (int)Math.Ceiling(left.TotalHours - 0.0001));
        return Loc.T("{0} min", Math.Max(1, (int)Math.Ceiling(left.TotalMinutes)));
    }

    public static int BestToday(DateTime utcNow)
    {
        return PlayerPrefs.GetString(DayKeyPref, string.Empty) == DayKey(utcNow) ? PlayerPrefs.GetInt(BestKey, 0) : 0;
    }

    public static bool Record(int score, DateTime utcNow)
    {
        int best = BestToday(utcNow);
        if (score <= best) return false;

        PlayerPrefs.SetString(DayKeyPref, DayKey(utcNow));
        PlayerPrefs.SetInt(BestKey, score);
        PlayerPrefs.Save();
        return true;
    }
}
