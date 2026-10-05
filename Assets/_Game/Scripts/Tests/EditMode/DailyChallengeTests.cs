using System;
using NUnit.Framework;
using UnityEngine;

public sealed class DailyChallengeTests
{
    private bool hadDay;
    private bool hadBest;
    private string savedDay;
    private int savedBest;

    [SetUp]
    public void SaveDaily()
    {
        hadDay = PlayerPrefs.HasKey("DailyBestDay");
        hadBest = PlayerPrefs.HasKey("DailyBest");
        savedDay = PlayerPrefs.GetString("DailyBestDay", string.Empty);
        savedBest = PlayerPrefs.GetInt("DailyBest", 0);
        PlayerPrefs.DeleteKey("DailyBestDay");
        PlayerPrefs.DeleteKey("DailyBest");
    }

    [TearDown]
    public void RestoreDaily()
    {
        if (hadDay) PlayerPrefs.SetString("DailyBestDay", savedDay);
        else PlayerPrefs.DeleteKey("DailyBestDay");
        if (hadBest) PlayerPrefs.SetInt("DailyBest", savedBest);
        else PlayerPrefs.DeleteKey("DailyBest");
        PlayerPrefs.Save();
    }

    [Test]
    public void Seed_IsTheSameAllDayAndChangesAtUtcMidnight()
    {
        var morning = new DateTime(2026, 10, 5, 0, 0, 1, DateTimeKind.Utc);
        var night = new DateTime(2026, 10, 5, 23, 59, 59, DateTimeKind.Utc);
        var next = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);

        Assert.AreEqual(DailyChallenge.SeedFor(morning), DailyChallenge.SeedFor(night));
        Assert.AreNotEqual(DailyChallenge.SeedFor(night), DailyChallenge.SeedFor(next));
        Assert.AreEqual("2026-10-05", DailyChallenge.DayKey(night));
        Assert.AreEqual("Oct 5", DailyChallenge.Label(night));
    }

    [Test]
    public void ResetsIn_CountsDownToMidnight()
    {
        Assert.AreEqual(TimeSpan.FromHours(6), DailyChallenge.ResetsIn(new DateTime(2026, 10, 5, 18, 0, 0, DateTimeKind.Utc)));
        Assert.AreEqual("6 h", DailyChallenge.FormatResetsIn(TimeSpan.FromHours(6)));
        Assert.AreEqual("6 h", DailyChallenge.FormatResetsIn(TimeSpan.FromHours(5.5)));
        Assert.AreEqual("45 min", DailyChallenge.FormatResetsIn(TimeSpan.FromMinutes(44.2)));
        Assert.AreEqual("1 min", DailyChallenge.FormatResetsIn(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void BestToday_KeepsTheHighestAndForgetsYesterday()
    {
        var today = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(0, DailyChallenge.BestToday(today));

        Assert.IsTrue(DailyChallenge.Record(14, today));
        Assert.IsFalse(DailyChallenge.Record(9, today));
        Assert.IsFalse(DailyChallenge.Record(14, today));
        Assert.IsTrue(DailyChallenge.Record(20, today.AddHours(3)));
        Assert.AreEqual(20, DailyChallenge.BestToday(today));

        var tomorrow = today.AddDays(1);
        Assert.AreEqual(0, DailyChallenge.BestToday(tomorrow), "A new day starts fresh");
        Assert.IsTrue(DailyChallenge.Record(3, tomorrow));
    }

    [Test]
    public void Scores_ReadNamesAndPartnersFromMetadata()
    {
        Assert.AreEqual("Happy Penguin", OnlineScores.NameFrom("{\"name\":\"Happy Penguin\"}", "abc"));
        Assert.AreEqual(PlayerNames.ForId("abc"), OnlineScores.NameFrom(null, "abc"));
        Assert.AreEqual(PlayerNames.ForId("abc"), OnlineScores.NameFrom("not json", "abc"));
        Assert.AreEqual("Brave Otter", OnlineScores.PartnerFrom("{\"name\":\"A\",\"partner\":\"Brave Otter\"}"));
        Assert.IsNull(OnlineScores.PartnerFrom(null));
    }

    [Test]
    public void Scores_MapSoloModesToBoards()
    {
        var classic = TestData.LoadMode(TestData.ClassicModePath);
        var rush = TestData.LoadMode(TestData.RushModePath);
        Assert.AreEqual(OnlineScores.ClassicBoard, OnlineScores.BoardFor(classic));
        Assert.AreEqual(OnlineScores.RushBoard, OnlineScores.BoardFor(rush));
        Assert.IsNull(OnlineScores.BoardFor(null));
    }
}
