using System.Collections.Generic;
using NUnit.Framework;

public sealed class GoldTests
{
    [Test]
    public void Gold_EarnsSpendsAndNeverGoesNegative()
    {
        var inventory = CosmeticInventory.FromJson(null, "{}");
        inventory.EarnGold(120);
        inventory.EarnGold(-50);
        Assert.AreEqual(120, inventory.Gold);

        Assert.IsFalse(inventory.SpendGold(200));
        Assert.AreEqual(120, inventory.Gold);
        Assert.IsTrue(inventory.SpendGold(100));
        Assert.AreEqual(20, inventory.Gold);
        Assert.IsFalse(inventory.SpendGold(0));
    }

    [Test]
    public void Gold_MergesEarnedAndSpentSeparately()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        phone.EarnGold(300);
        phone.SpendGold(250);

        var cloud = CosmeticInventory.FromJson(null, "{}");
        cloud.EarnGold(400);

        phone.MergeFrom(cloud);
        Assert.AreEqual(150, phone.Gold, "Spending on one phone must survive a merge");
    }

    [Test]
    public void ScoreGold_PaysOnePerFivePoints()
    {
        Assert.AreEqual(0, RunPurse.ScoreGold(4, 5));
        Assert.AreEqual(1, RunPurse.ScoreGold(5, 5));
        Assert.AreEqual(9, RunPurse.ScoreGold(47, 5));
        Assert.AreEqual(0, RunPurse.ScoreGold(-10, 5));
        Assert.AreEqual(0, RunPurse.ScoreGold(30, 0));
    }

    [Test]
    public void Catalog_SellsOnlyEarnableCostumesForGold()
    {
        var catalog = TestData.LoadCatalog();
        int forGold = 0;
        foreach (var item in catalog.Items)
        {
            bool earnable = item.Unlock == UnlockKind.Score || item.Unlock == UnlockKind.Ads;
            if (earnable)
            {
                Assert.Greater(item.GoldPrice, 0, item.Id);
                Assert.IsTrue(item.SoldForGold, item.Id);
                forGold++;
            }
            else
            {
                Assert.IsFalse(item.SoldForGold, item.Id + " must not be sold for gold");
            }
        }

        Assert.AreEqual(13, forGold);
    }

    [Test]
    public void Arcade_PlaysLikeRushButAMissEndsTheRun()
    {
        var mode = TestData.LoadMode("Assets/_Game/Data/Modes/mode_arcade.asset");
        Assert.AreEqual(GameModeKind.Arcade, mode.Kind);

        var rules = MatchRules.Create(mode);
        Assert.IsInstanceOf<ArcadeRules>(rules);
        Assert.IsFalse(rules.IsTimed);

        var player = new Participant(0, FieldSide.Bottom, null);
        rules.Begin(new List<Participant> { player });
        Assert.AreEqual(1, rules.ScoreHit(player, 0.6f).Points);
        Assert.IsTrue(rules.ScoreHit(player, 0f).Perfect);
        Assert.IsTrue(rules.MissWouldEnd(player));
        Assert.IsTrue(rules.OnMiss(player));
        Assert.AreEqual(OnlineScores.ArcadeBoard, OnlineScores.BoardFor(mode));
    }
}
