using System;
using System.Globalization;
using UnityEngine;

public static class ReviewPrompt
{
    public const string DaysKey = "ReviewDaysPlayed";
    public const string LastDayKey = "ReviewLastDay";
    public const string LastAskedKey = "ReviewLastAsked";
    private const string DayFormat = "yyyy-MM-dd";

    public static int DaysPlayed => PlayerPrefs.GetInt(DaysKey, 0);

    public static DateTime LastAskedUtc
    {
        get
        {
            string saved = PlayerPrefs.GetString(LastAskedKey, string.Empty);
            bool valid = long.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks) && ticks > 0 && ticks <= DateTime.MaxValue.Ticks;
            return valid ? new DateTime(ticks, DateTimeKind.Utc) : default;
        }
    }

    public static void NoteRun(DateTime now)
    {
        string today = now.ToString(DayFormat, CultureInfo.InvariantCulture);
        string last = PlayerPrefs.GetString(LastDayKey, string.Empty);
        if (last == today) return;

        PlayerPrefs.SetInt(DaysKey, ReviewGate.CountDay(DaysPlayed, last, today));
        PlayerPrefs.SetString(LastDayKey, today);
        PlayerPrefs.Save();
    }

    public static bool TryAsk(bool newBest, int gamesPlayed, DateTime nowUtc)
    {
        if (!ReviewGate.ShouldAsk(newBest, DaysPlayed, gamesPlayed, LastAskedUtc, nowUtc)) return false;

        PlayerPrefs.SetString(LastAskedKey, nowUtc.Ticks.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        InAppReview.Request();
        return true;
    }
}
