using NUnit.Framework;
using UnityEditor;

public sealed class PingiRoadTests
{
    private const string RoadPath = "Assets/_Game/Data/road_pingi.asset";

    [Test]
    public void Curve_GrowsUntilTheCapThenStaysFlat()
    {
        Assert.AreEqual(60, RoadMath.XpToNext(1, 60, 10, 50));
        Assert.AreEqual(150, RoadMath.XpToNext(10, 60, 10, 50));
        Assert.AreEqual(550, RoadMath.XpToNext(50, 60, 10, 50));
        Assert.AreEqual(550, RoadMath.XpToNext(80, 60, 10, 50));
        Assert.AreEqual(60, RoadMath.XpToNext(0, 60, 10, 50));
    }

    [Test]
    public void Standing_CarriesLeftoverXpIntoTheNextLevel()
    {
        var start = RoadMath.StandingAt(0, 60, 10, 50);
        Assert.AreEqual(1, start.Level);
        Assert.AreEqual(0, start.Into);
        Assert.AreEqual(60, start.Need);

        var mid = RoadMath.StandingAt(75, 60, 10, 50);
        Assert.AreEqual(2, mid.Level);
        Assert.AreEqual(15, mid.Into);
        Assert.AreEqual(70, mid.Need);

        Assert.AreEqual(3, RoadMath.StandingAt(130, 60, 10, 50).Level);
        Assert.AreEqual(1, RoadMath.StandingAt(-40, 60, 10, 50).Level);
    }

    [Test]
    public void Standing_KeepsGoingPastTheCap()
    {
        int toCap = 0;
        for (int level = 1; level < 50; level++) toCap += RoadMath.XpToNext(level, 60, 10, 50);
        Assert.AreEqual(50, RoadMath.StandingAt(toCap, 60, 10, 50).Level);
        Assert.AreEqual(52, RoadMath.StandingAt(toCap + 1100, 60, 10, 50).Level);
    }

    [Test]
    public void Rewards_PayGoldEveryLevelAndStarsEveryFifth()
    {
        Assert.AreEqual(0, RoadMath.GoldFor(1, 30, 5, 50));
        Assert.AreEqual(40, RoadMath.GoldFor(2, 30, 5, 50));
        Assert.AreEqual(280, RoadMath.GoldFor(50, 30, 5, 50));
        Assert.AreEqual(280, RoadMath.GoldFor(90, 30, 5, 50));

        Assert.AreEqual(0, RoadMath.StarsFor(4, 5, 2));
        Assert.AreEqual(2, RoadMath.StarsFor(5, 5, 2));
        Assert.AreEqual(2, RoadMath.StarsFor(55, 5, 2));
        Assert.AreEqual(0, RoadMath.StarsFor(5, 0, 2));
    }

    [Test]
    public void RoadAsset_GivesItsOwnCostumesAtMilestones()
    {
        var road = AssetDatabase.LoadAssetAtPath<PingiRoad>(RoadPath);
        Assert.IsNotNull(road, "Missing road asset");
        Assert.AreSame(TestData.LoadCatalog(), road.Catalog);

        var levels = road.ItemLevels();
        CollectionAssert.AreEqual(new[] { 10, 25, 40 }, levels);
        foreach (int level in levels)
        {
            var reward = road.RewardFor(level);
            Assert.IsNotNull(reward.Item, "No costume at level " + level);
            Assert.AreEqual(0, reward.Stars, "A costume level should not also pay stars");
            Assert.Greater(reward.Gold, 0);
            Assert.IsFalse(reward.Item.IsPurchasable, reward.Item.Id);
            Assert.IsFalse(reward.Item.SoldForGold, reward.Item.Id);
        }

        Assert.IsNull(road.RewardFor(15).Item);
        Assert.AreEqual(2, road.RewardFor(15).Stars);
    }

    [Test]
    public void Inventory_KeepsXpAndRoadLevelThroughAMerge()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        Assert.AreEqual(0, phone.Xp);
        Assert.AreEqual(1, phone.RoadLevel);

        phone.AddXp(200);
        phone.AddXp(-30);
        phone.ReachLevel(3);
        Assert.AreEqual(200, phone.Xp);
        Assert.AreEqual(3, phone.RoadLevel);

        var cloud = CosmeticInventory.FromJson(null, "{}");
        cloud.AddXp(500);
        cloud.ReachLevel(6);
        phone.MergeFrom(cloud);
        Assert.AreEqual(500, phone.Xp);
        Assert.AreEqual(6, phone.RoadLevel);

        var copy = CosmeticInventory.FromJson(null, phone.ToBackupJson());
        Assert.AreEqual(500, copy.Xp);
        Assert.AreEqual(6, copy.RoadLevel);

        Assert.IsFalse(phone.XpSeeded);
        cloud.MarkXpSeeded();
        phone.MergeFrom(cloud);
        Assert.IsTrue(phone.XpSeeded, "A gift given on one phone counts everywhere");
    }

    [Test]
    public void LevelCostumes_OpenOnlyThroughTheRoad()
    {
        var item = TestData.Item("paddle_road", CosmeticCategory.Paddle, UnlockKind.Level, 10);
        var inventory = CosmeticInventory.FromJson(null, "{}");
        inventory.GrantPass();
        Assert.IsFalse(inventory.IsUnlocked(item), "Pingi Pass must not open road costumes");

        inventory.Grant(item.Id);
        Assert.IsTrue(inventory.IsUnlocked(item));
    }
}
