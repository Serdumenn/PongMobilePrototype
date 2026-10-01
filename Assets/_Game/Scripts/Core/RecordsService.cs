using System;
using UnityEngine;

public sealed class RecordsService : MonoBehaviour
{
    private const string SaveFileName = "records.json";

    public PlayerRecords Records { get; private set; }

    public int DayStreak => Records.CurrentDayStreak(DateTime.Now);

    private void Awake()
    {
        Records = PlayerRecords.Load(SaveLocation.PathFor(SaveFileName));
    }

    public void RecordRun(GameModeDefinition mode, bool newBest, int longestStreak, float seconds)
    {
        Records.RecordRun(mode != null ? mode.Id : "classic", newBest, longestStreak, seconds, DateTime.Now);
        Records.Save();
    }

    public bool RecordMatch(int coopRally, float seconds)
    {
        bool newBest = Records.RecordMatch(coopRally, seconds, DateTime.Now);
        Records.Save();
        return newBest;
    }

    public void AddHit()
    {
        Records.AddHit();
    }

    public void ClearBestDates()
    {
        Records.ClearBestDates();
        Records.Save();
    }
}
