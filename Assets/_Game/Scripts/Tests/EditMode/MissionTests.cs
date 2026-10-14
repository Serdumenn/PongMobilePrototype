using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

public sealed class MissionTests
{
    private static readonly DateTime Day = new DateTime(2026, 10, 15);

    [Test]
    public void Book_GivesThreeDifferentMissionsADay()
    {
        for (int i = 0; i < 60; i++)
        {
            var missions = MissionBook.For(Day.AddDays(i), true);
            Assert.AreEqual(3, missions.Length);

            var kinds = new HashSet<MissionKind>();
            foreach (var mission in missions)
            {
                Assert.IsTrue(kinds.Add(mission.Kind), "Same kind twice on " + Day.AddDays(i).ToShortDateString());
                Assert.Greater(mission.Target, 0);
            }
        }
    }

    [Test]
    public void Book_IsTheSameForEveryoneAndChangesDaily()
    {
        var first = MissionBook.For(Day, true);
        CollectionAssert.AreEqual(Describe(first), Describe(MissionBook.For(Day, true)));

        var sets = new HashSet<string>();
        for (int i = 0; i < 30; i++) sets.Add(string.Join(",", Describe(MissionBook.For(Day.AddDays(i), true))));
        Assert.GreaterOrEqual(sets.Count, 10);
    }

    [Test]
    public void Book_SkipsRushMissionsWhileRushIsLocked()
    {
        for (int i = 0; i < 60; i++)
            foreach (var mission in MissionBook.For(Day.AddDays(i), false))
                Assert.IsFalse(mission.NeedsRush, mission.Kind.ToString());
    }

    [Test]
    public void Log_CountsCompletesAndPaysTheBonusOnce()
    {
        var log = MissionLog.Load(null);
        log.Roll("2026-10-15");

        Assert.IsFalse(log.Raise(0, 10, 30));
        Assert.AreEqual(10, log.Progress(0));
        Assert.IsTrue(log.Raise(0, 99, 30));
        Assert.AreEqual(30, log.Progress(0));
        Assert.IsFalse(log.Raise(0, 120, 30), "A finished mission finished again");
        Assert.IsFalse(log.ClaimBonus());

        Assert.IsTrue(log.Raise(1, 5, 5));
        Assert.IsTrue(log.Raise(2, 3, 3));
        Assert.AreEqual(3, log.DoneCount);
        Assert.IsTrue(log.ClaimBonus());
        Assert.IsFalse(log.ClaimBonus());
    }

    [Test]
    public void Log_StartsOverOnANewDayAndSurvivesARestart()
    {
        string folder = Path.Combine(Path.GetTempPath(), "PingiMissionTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "missions.json");
        try
        {
            var log = MissionLog.Load(path);
            log.Roll("2026-10-15");
            log.Raise(1, 7, 20);
            Assert.IsTrue(log.Save());

            var again = MissionLog.Load(path);
            Assert.AreEqual("2026-10-15", again.Day);
            Assert.AreEqual(7, again.Progress(1));
            Assert.IsFalse(again.Roll("2026-10-15"));

            Assert.IsTrue(again.Roll("2026-10-16"));
            Assert.AreEqual(0, again.Progress(1));
            Assert.AreEqual(0, again.DoneCount);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    public void Stars_AddUpMergeByMaximumAndStayOutOfThePass()
    {
        var a = CosmeticInventory.FromJson(null, "{}");
        a.AddStars(4);
        a.AddStars(-3);
        Assert.AreEqual(4, a.Stars);

        var b = CosmeticInventory.FromJson(null, "{}");
        b.AddStars(9);
        a.MergeFrom(b);
        Assert.AreEqual(9, a.Stars);

        var starry = TestData.Item("paddle_starry", CosmeticCategory.Paddle, UnlockKind.Stars, 15);
        a.GrantPass();
        Assert.IsFalse(a.IsUnlocked(starry), "The Pass must not hand out mission rewards");
        a.Grant(starry.Id);
        Assert.IsTrue(a.IsUnlocked(starry));
    }

    [Test]
    public void Catalog_HasTheStarRewardsInOrder()
    {
        var catalog = TestData.LoadCatalog();
        var steps = new List<int>();
        foreach (var item in catalog.Items)
            if (item != null && item.Unlock == UnlockKind.Stars)
            {
                Assert.IsFalse(item.IsPurchasable, item.Id);
                steps.Add(item.UnlockValue);
            }

        steps.Sort();
        CollectionAssert.AreEqual(new[] { 15, 40, 80 }, steps);
    }

    private static List<string> Describe(MissionDef[] missions)
    {
        var list = new List<string>();
        foreach (var mission in missions) list.Add(mission.Kind + ":" + mission.Target);
        return list;
    }
}
