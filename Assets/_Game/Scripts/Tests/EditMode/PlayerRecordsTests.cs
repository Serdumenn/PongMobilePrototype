using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerRecordsTests
{
    private string path;

    [SetUp]
    public void SetUp()
    {
        path = TestData.TempFile("records_test.json");
    }

    [TearDown]
    public void TearDown()
    {
        TestData.Delete(path);
    }

    private static DateTime At(int year, int month, int day, int hour = 12, int minute = 0)
    {
        return new DateTime(year, month, day, hour, minute, 0);
    }

    [Test]
    public void MissingFile_StartsEmpty()
    {
        var records = PlayerRecords.Load(path);

        Assert.AreEqual(0, records.GamesPlayed);
        Assert.AreEqual(0, records.TotalHits);
        Assert.AreEqual(0, records.LongestStreak);
        Assert.AreEqual(0f, records.PlayTimeSeconds);
        Assert.AreEqual(0, records.BestDayStreak);
        Assert.AreEqual(0, records.CurrentDayStreak(At(2026, 10, 1)));
        Assert.IsNull(records.BestDate("classic"));
    }

    [Test]
    public void RecordRun_AccumulatesStats()
    {
        var records = PlayerRecords.Load(path);
        var now = At(2026, 10, 1);

        records.RecordRun("classic", false, 7, 30f, now);
        records.RecordRun("classic", false, 4, 12.5f, now);
        records.RecordRun("rush", false, 9, -3f, now);
        records.AddHit();
        records.AddHit();

        Assert.AreEqual(3, records.GamesPlayed);
        Assert.AreEqual(9, records.LongestStreak);
        Assert.AreEqual(42.5f, records.PlayTimeSeconds, 0.0001f);
        Assert.AreEqual(2, records.TotalHits);
    }

    [Test]
    public void BestDate_IsStoredPerModeOnlyForNewBests()
    {
        var records = PlayerRecords.Load(path);

        records.RecordRun("classic", true, 1, 1f, At(2026, 9, 28));
        records.RecordRun("rush", false, 1, 1f, At(2026, 9, 29));
        Assert.AreEqual("2026-09-28", records.BestDate("classic"));
        Assert.IsNull(records.BestDate("rush"));

        records.RecordRun("classic", true, 1, 1f, At(2026, 10, 1));
        Assert.AreEqual("2026-10-01", records.BestDate("classic"));

        records.ClearBestDates();
        Assert.IsNull(records.BestDate("classic"));
    }

    [Test]
    public void DayStreak_CountsConsecutiveDaysAcrossMidnightAndYearEnd()
    {
        var records = PlayerRecords.Load(path);

        records.RecordRun("classic", false, 1, 1f, At(2026, 12, 30, 23, 50));
        Assert.AreEqual(1, records.CurrentDayStreak(At(2026, 12, 30, 23, 55)));

        records.RecordRun("classic", false, 1, 1f, At(2026, 12, 30, 23, 55));
        Assert.AreEqual(1, records.CurrentDayStreak(At(2026, 12, 30, 23, 59)));

        Assert.AreEqual(1, records.CurrentDayStreak(At(2026, 12, 31, 0, 5)));
        Assert.IsFalse(records.PlayedToday(At(2026, 12, 31, 0, 5)));

        records.RecordRun("rush", false, 1, 1f, At(2026, 12, 31, 0, 5));
        Assert.AreEqual(2, records.CurrentDayStreak(At(2026, 12, 31, 0, 6)));
        Assert.IsTrue(records.PlayedToday(At(2026, 12, 31, 18, 0)));

        records.RecordRun("classic", false, 1, 1f, At(2027, 1, 1));
        Assert.AreEqual(3, records.CurrentDayStreak(At(2027, 1, 1)));
        Assert.AreEqual(3, records.BestDayStreak);
    }

    [Test]
    public void DayStreak_MissedDayResets()
    {
        var records = PlayerRecords.Load(path);
        records.RecordRun("classic", false, 1, 1f, At(2027, 1, 1));
        records.RecordRun("classic", false, 1, 1f, At(2027, 1, 2));

        Assert.AreEqual(0, records.CurrentDayStreak(At(2027, 1, 4)));

        records.RecordRun("classic", false, 1, 1f, At(2027, 1, 4));
        Assert.AreEqual(1, records.CurrentDayStreak(At(2027, 1, 4)));
        Assert.AreEqual(2, records.BestDayStreak);
    }

    [Test]
    public void DayStreak_ReachesSevenForAWeek()
    {
        var records = PlayerRecords.Load(path);
        var day = At(2026, 10, 1);
        for (int i = 0; i < 7; i++) records.RecordRun("classic", false, 1, 1f, day.AddDays(i));

        Assert.AreEqual(7, records.CurrentDayStreak(day.AddDays(6)));
        Assert.AreEqual(7, records.BestDayStreak);
    }

    [Test]
    public void DayStreak_ClockMovedBackStaysSane()
    {
        var records = PlayerRecords.Load(path);
        for (int i = 0; i < 3; i++) records.RecordRun("classic", false, 1, 1f, At(2026, 10, 1).AddDays(i));

        var back = At(2026, 9, 29);
        records.RecordRun("classic", false, 1, 1f, back);

        Assert.AreEqual(1, records.CurrentDayStreak(back));
        Assert.AreEqual(3, records.BestDayStreak);
    }

    [Test]
    public void SaveAndLoad_RoundTrips()
    {
        var records = PlayerRecords.Load(path);
        records.RecordRun("classic", true, 6, 20f, At(2026, 10, 1));
        records.RecordRun("rush", true, 3, 60f, At(2026, 10, 2));
        records.AddHit();

        Assert.IsTrue(records.Save());
        Assert.IsTrue(records.Save());
        Assert.IsFalse(File.Exists(path + ".tmp"));

        var loaded = PlayerRecords.Load(path);
        Assert.AreEqual(2, loaded.GamesPlayed);
        Assert.AreEqual(1, loaded.TotalHits);
        Assert.AreEqual(6, loaded.LongestStreak);
        Assert.AreEqual(80f, loaded.PlayTimeSeconds, 0.0001f);
        Assert.AreEqual(2, loaded.BestDayStreak);
        Assert.AreEqual(2, loaded.CurrentDayStreak(At(2026, 10, 2)));
        Assert.AreEqual("2026-10-01", loaded.BestDate("classic"));
        Assert.AreEqual("2026-10-02", loaded.BestDate("rush"));
    }

    [Test]
    public void CorruptFile_StartsFresh()
    {
        File.WriteAllText(path, "{ this is not json");
        LogAssert.Expect(LogType.Warning, new Regex(@"^\[Records\] Save could not be read"));

        var records = PlayerRecords.Load(path);

        Assert.AreEqual(0, records.GamesPlayed);
        Assert.AreEqual(0, records.CurrentDayStreak(At(2026, 10, 1)));
    }
}
