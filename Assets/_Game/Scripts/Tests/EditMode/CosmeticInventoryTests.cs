using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CosmeticInventoryTests
{
    private string path;
    private CosmeticItem free;
    private CosmeticItem score;
    private CosmeticItem ads;
    private CosmeticItem passOnly;
    private CosmeticItem streak;

    [SetUp]
    public void SetUp()
    {
        path = TestData.TempFile("cosmetics_test.json");
        free = TestData.Item("ball_free", CosmeticCategory.Ball, UnlockKind.Free);
        score = TestData.Item("ball_score", CosmeticCategory.Ball, UnlockKind.Score, 10, "skin_score");
        ads = TestData.Item("paddle_ads", CosmeticCategory.Paddle, UnlockKind.Ads, 5, "paddle_ads");
        passOnly = TestData.Item("ball_pass", CosmeticCategory.Ball, UnlockKind.PassOnly);
        streak = TestData.Item("ball_streak", CosmeticCategory.Ball, UnlockKind.Streak, 7);
    }

    [TearDown]
    public void TearDown()
    {
        TestData.Delete(path);
        foreach (var item in new[] { free, score, ads, passOnly, streak }) Object.DestroyImmediate(item);
    }

    [Test]
    public void FreshInventory_OnlyFreeItemsUnlocked()
    {
        var inventory = CosmeticInventory.Load(path);

        Assert.IsTrue(inventory.IsUnlocked(free));
        Assert.IsFalse(inventory.IsUnlocked(score));
        Assert.IsFalse(inventory.IsUnlocked(ads));
        Assert.IsFalse(inventory.IsUnlocked(passOnly));
        Assert.IsFalse(inventory.IsUnlocked(streak));
        Assert.IsFalse(inventory.IsUnlocked(null));
        Assert.IsFalse(inventory.HasPass);
        Assert.IsFalse(inventory.NoAds);
    }

    [Test]
    public void Grant_UnlocksOnceAndRejectsEmpty()
    {
        var inventory = CosmeticInventory.Load(path);

        Assert.IsTrue(inventory.Grant(score.Id));
        Assert.IsFalse(inventory.Grant(score.Id));
        Assert.IsFalse(inventory.Grant(""));
        Assert.IsFalse(inventory.Grant(null));
        Assert.IsTrue(inventory.IsUnlocked(score));
    }

    [Test]
    public void Pass_UnlocksEverythingExceptStreakRewards()
    {
        var inventory = CosmeticInventory.Load(path);
        inventory.GrantPass();

        Assert.IsTrue(inventory.HasPass);
        Assert.IsTrue(inventory.IsUnlocked(score));
        Assert.IsTrue(inventory.IsUnlocked(ads));
        Assert.IsTrue(inventory.IsUnlocked(passOnly));
        Assert.IsFalse(inventory.IsUnlocked(streak));

        inventory.Grant(streak.Id);
        Assert.IsTrue(inventory.IsUnlocked(streak));
    }

    [Test]
    public void AdViews_UnlockAtThreshold()
    {
        var inventory = CosmeticInventory.Load(path);

        for (int i = 1; i <= 4; i++)
        {
            Assert.IsFalse(inventory.AddAdView(ads));
            Assert.AreEqual(i, inventory.AdViews(ads));
        }

        Assert.IsTrue(inventory.AddAdView(ads));
        Assert.AreEqual(5, inventory.AdViews(ads));
        Assert.IsTrue(inventory.IsUnlocked(ads));
        Assert.IsFalse(inventory.AddAdView(ads));
        Assert.AreEqual(0, inventory.AdViews(score));
    }

    [Test]
    public void Equipped_IsTrackedPerCategory()
    {
        var inventory = CosmeticInventory.Load(path);

        inventory.SetEquipped(CosmeticCategory.Ball, "ball_a");
        inventory.SetEquipped(CosmeticCategory.Paddle, "paddle_b");
        inventory.SetEquipped(CosmeticCategory.Theme, "theme_c");

        Assert.AreEqual("ball_a", inventory.GetEquipped(CosmeticCategory.Ball));
        Assert.AreEqual("paddle_b", inventory.GetEquipped(CosmeticCategory.Paddle));
        Assert.AreEqual("theme_c", inventory.GetEquipped(CosmeticCategory.Theme));
    }

    [Test]
    public void Orders_AreProcessedOnce()
    {
        var inventory = CosmeticInventory.Load(path);

        Assert.IsFalse(inventory.IsOrderProcessed("GPA.1"));
        inventory.MarkOrderProcessed("GPA.1");
        inventory.MarkOrderProcessed("GPA.1");
        inventory.MarkOrderProcessed(null);

        Assert.IsTrue(inventory.IsOrderProcessed("GPA.1"));
        Assert.IsFalse(inventory.IsOrderProcessed(""));
        Assert.IsFalse(inventory.IsOrderProcessed(null));
    }

    [Test]
    public void SaveAndLoad_RoundTrips()
    {
        var inventory = CosmeticInventory.Load(path);
        inventory.Grant(score.Id);
        inventory.AddAdView(ads);
        inventory.AddAdView(ads);
        inventory.SetEquipped(CosmeticCategory.Ball, score.Id);
        inventory.GrantNoAds();
        inventory.MarkOrderProcessed("GPA.7");

        Assert.IsTrue(inventory.Save());
        Assert.IsTrue(inventory.Save());
        Assert.IsFalse(File.Exists(path + ".tmp"));

        var loaded = CosmeticInventory.Load(path);
        Assert.IsTrue(loaded.IsUnlocked(score));
        Assert.AreEqual(2, loaded.AdViews(ads));
        Assert.AreEqual(score.Id, loaded.GetEquipped(CosmeticCategory.Ball));
        Assert.IsTrue(loaded.NoAds);
        Assert.IsFalse(loaded.HasPass);
        Assert.IsTrue(loaded.IsOrderProcessed("GPA.7"));
    }

    [Test]
    public void CorruptFile_StartsFresh()
    {
        File.WriteAllText(path, "garbage");
        LogAssert.Expect(LogType.Warning, new Regex(@"^\[Cosmetics\] Save could not be read"));

        var inventory = CosmeticInventory.Load(path);

        Assert.IsFalse(inventory.HasPass);
        Assert.IsFalse(inventory.IsUnlocked(score));
    }
}
