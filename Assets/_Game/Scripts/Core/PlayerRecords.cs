using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public sealed class PlayerRecords
{
    private const int CurrentVersion = 1;
    private const string DayFormat = "yyyy-MM-dd";

    [Serializable]
    private sealed class SaveData
    {
        public int Version = CurrentVersion;
        public int GamesPlayed;
        public int TotalHits;
        public int LongestStreak;
        public float PlayTimeSeconds;
        public List<string> BestModes = new List<string>();
        public List<string> BestDates = new List<string>();
        public string LastPlayedDay;
        public int DayStreak;
        public int BestDayStreak;
    }

    private readonly string path;
    private readonly SaveData data;

    public int GamesPlayed => data.GamesPlayed;
    public int TotalHits => data.TotalHits;
    public int LongestStreak => data.LongestStreak;
    public float PlayTimeSeconds => data.PlayTimeSeconds;
    public int BestDayStreak => data.BestDayStreak;

    private PlayerRecords(string path, SaveData data)
    {
        this.path = path;
        this.data = data;
    }

    public static PlayerRecords Load(string path)
    {
        SaveData loaded = null;
        try
        {
            if (File.Exists(path)) loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Records] Save could not be read, starting fresh: {e.Message}");
        }

        return new PlayerRecords(path, loaded ?? new SaveData());
    }

    public int CurrentDayStreak(DateTime now)
    {
        if (string.IsNullOrEmpty(data.LastPlayedDay)) return 0;
        var today = now.Date;
        return data.LastPlayedDay == Day(today) || data.LastPlayedDay == Day(today.AddDays(-1)) ? data.DayStreak : 0;
    }

    public bool PlayedToday(DateTime now)
    {
        return data.LastPlayedDay == Day(now.Date);
    }

    public string BestDate(string modeId)
    {
        int index = data.BestModes.IndexOf(modeId);
        return index >= 0 ? data.BestDates[index] : null;
    }

    public void AddHit()
    {
        data.TotalHits++;
    }

    public void RecordRun(string modeId, bool newBest, int longestStreak, float seconds, DateTime now)
    {
        data.GamesPlayed++;
        data.LongestStreak = Mathf.Max(data.LongestStreak, longestStreak);
        data.PlayTimeSeconds += Mathf.Max(0f, seconds);

        if (newBest)
        {
            int index = data.BestModes.IndexOf(modeId);
            if (index < 0)
            {
                data.BestModes.Add(modeId);
                data.BestDates.Add(Day(now.Date));
            }
            else data.BestDates[index] = Day(now.Date);
        }

        string today = Day(now.Date);
        if (data.LastPlayedDay != today)
        {
            data.DayStreak = data.LastPlayedDay == Day(now.Date.AddDays(-1)) ? data.DayStreak + 1 : 1;
            data.LastPlayedDay = today;
            data.BestDayStreak = Mathf.Max(data.BestDayStreak, data.DayStreak);
        }
    }

    public void ClearBestDates()
    {
        data.BestModes.Clear();
        data.BestDates.Clear();
    }

    public bool Save()
    {
        try
        {
            data.Version = CurrentVersion;
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Records] Save failed: {e.Message}");
            return false;
        }
    }

    private static string Day(DateTime date)
    {
        return date.ToString(DayFormat, CultureInfo.InvariantCulture);
    }
}
