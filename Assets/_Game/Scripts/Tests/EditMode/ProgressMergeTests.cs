using System;
using NUnit.Framework;
using UnityEngine;

public sealed class ProgressMergeTests
{
    private static PlayerRecords Records(int games, int hits, string lastDay, int streak, params string[] bestModes)
    {
        var records = PlayerRecords.FromJson(null, null);
        for (int i = 0; i < hits; i++) records.AddHit();
        var day = DateTime.ParseExact(lastDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < games; i++)
        {
            var playedOn = day.AddDays(-Math.Max(0, streak - 1 - i));
            records.RecordRun(i < bestModes.Length ? bestModes[i] : "classic", i < bestModes.Length, 3, 10f, playedOn);
        }
        return records;
    }

    [Test]
    public void Records_KeepTheLargerStatsAndTheNewestStreak()
    {
        var phone = Records(4, 20, "2026-10-05", 2, "classic");
        var cloud = Records(9, 5, "2026-10-03", 3, "rush");

        phone.MergeFrom(cloud, false);
        var merged = PlayerRecords.FromJson(null, phone.ToJson());

        Assert.AreEqual(9, merged.GamesPlayed, "Larger game count");
        Assert.AreEqual(20, merged.TotalHits, "Larger hit count");
        Assert.AreEqual(2, merged.CurrentDayStreak(new DateTime(2026, 10, 5)), "The streak of the most recent day wins");
        Assert.AreEqual(3, merged.BestDayStreak, "Best streak is kept");
        Assert.IsNotNull(merged.BestDate("classic"));
        Assert.IsNotNull(merged.BestDate("rush"), "Best dates from the cloud are added");
    }

    [Test]
    public void Records_AfterAReset_KeepOnlyTheirOwnBestDates()
    {
        var phone = Records(4, 20, "2026-10-05", 1);
        phone.ClearBestDates();
        var cloud = Records(9, 5, "2026-10-03", 1, "rush");

        phone.MergeFrom(cloud, true);
        Assert.IsNull(phone.BestDate("rush"), "A reset is not undone by the backup");
    }

    [Test]
    public void Cosmetics_UniteOwnedItemsAndPurchases()
    {
        var phone = CosmeticInventory.FromJson(null, null);
        phone.Grant("ball_frog");
        phone.SetEquipped(CosmeticCategory.Ball, "ball_frog");
        phone.MarkOrderProcessed("order-1");

        var cloud = CosmeticInventory.FromJson(null, null);
        cloud.Grant("ball_astro");
        cloud.GrantNoAds();
        cloud.MarkOrderProcessed("order-2");
        cloud.SetEquipped(CosmeticCategory.Ball, "ball_astro");
        cloud.SetEquipped(CosmeticCategory.Theme, "theme_mint");

        phone.MergeFrom(cloud);
        var merged = CosmeticInventory.FromJson(null, phone.ToJson());

        Assert.IsTrue(merged.NoAds, "Purchases are never lost");
        Assert.IsTrue(merged.IsOrderProcessed("order-1"), "This phone keeps its own order list");
        Assert.IsFalse(merged.IsOrderProcessed("order-2"), "Order numbers are not taken from the backup");
        StringAssert.DoesNotContain("order-1", phone.ToBackupJson(), "Order numbers never go to the backup");
        Assert.AreEqual("ball_frog", merged.GetEquipped(CosmeticCategory.Ball), "This phone's choice stays");
        Assert.AreEqual("theme_mint", merged.GetEquipped(CosmeticCategory.Theme), "Empty choices are filled from the cloud");
        StringAssert.Contains("ball_astro", phone.ToJson(), "Items owned elsewhere are added");
        StringAssert.Contains("ball_frog", phone.ToJson());
    }

    [Test]
    public void Snapshot_TakesTheBestScoresAndTheNewestDaily()
    {
        var phone = new ProgressSnapshot { records = Records(1, 1, "2026-10-05", 1).ToJson(), cosmetics = CosmeticInventory.FromJson(null, null).ToJson(), dailyDay = "2026-10-05", dailyBest = 12 };
        phone.SetBest("bestScore", 11);
        phone.SetBest("bestScore_rush", 15);
        var cloud = new ProgressSnapshot { records = Records(5, 9, "2026-10-04", 1).ToJson(), cosmetics = CosmeticInventory.FromJson(null, null).ToJson(), dailyDay = "2026-10-05", dailyBest = 20 };
        cloud.SetBest("bestScore", 30);
        cloud.SetBest("bestScore_old", 4);

        var merged = ProgressSnapshot.Merge(phone, cloud, false);
        Assert.AreEqual(30, merged.Best("bestScore"));
        Assert.AreEqual(15, merged.Best("bestScore_rush"));
        Assert.AreEqual(4, merged.Best("bestScore_old"), "Unknown keys are kept");
        Assert.AreEqual(20, merged.dailyBest, "Same day: the higher daily best");

        cloud.dailyDay = "2026-10-04";
        Assert.AreEqual(12, ProgressSnapshot.Merge(phone, cloud, false).dailyBest, "An older daily never wins");

        var reset = ProgressSnapshot.Merge(phone, cloud, true);
        Assert.AreEqual(11, reset.Best("bestScore"), "After a reset the phone's bests win");
        Assert.AreEqual(0, reset.Best("bestScore_old"));
    }

    [Test]
    public void Snapshot_IgnoresANewerFormatAndCanCompare()
    {
        var phone = new ProgressSnapshot { records = "{}", cosmetics = "{}" };
        phone.SetBest("bestScore", 3);
        var future = new ProgressSnapshot { v = ProgressSnapshot.CurrentVersion + 1 };
        future.SetBest("bestScore", 99);

        Assert.AreSame(phone, ProgressSnapshot.Merge(phone, future, false), "A backup from a newer app version is not merged");
        Assert.AreSame(phone, ProgressSnapshot.Merge(phone, null, false));

        var copy = JsonUtility.FromJson<ProgressSnapshot>(JsonUtility.ToJson(phone));
        Assert.IsTrue(copy.SameProgress(phone));
        copy.SetBest("bestScore", 4);
        Assert.IsFalse(copy.SameProgress(phone));
    }
}
