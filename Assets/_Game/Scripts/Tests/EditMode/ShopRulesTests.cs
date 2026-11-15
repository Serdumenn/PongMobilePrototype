using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class ShopRulesTests
{
    private static List<CosmeticItem> Catalog()
    {
        return new List<CosmeticItem>
        {
            TestData.Item("ball_pingi", CosmeticCategory.Ball, UnlockKind.Free),
            TestData.Priced("ball_octo", CosmeticCategory.Ball, UnlockKind.Gold, 300, CosmeticTier.Common),
            TestData.Priced("ball_bunny", CosmeticCategory.Ball, UnlockKind.Gold, 700, CosmeticTier.Rare),
            TestData.Priced("ball_penguin", CosmeticCategory.Ball, UnlockKind.Gold, 1500, CosmeticTier.Epic),
            TestData.Priced("ball_dragon", CosmeticCategory.Ball, UnlockKind.Gold, 3000, CosmeticTier.Legendary),
            TestData.Priced("trail_leaf", CosmeticCategory.Trail, UnlockKind.Gold, 300, CosmeticTier.Common),
            TestData.Priced("trail_hearts", CosmeticCategory.Trail, UnlockKind.Gold, 700, CosmeticTier.Rare),
            TestData.Priced("hit_confetti", CosmeticCategory.Hit, UnlockKind.Gold, 300, CosmeticTier.Common),
            TestData.Priced("trail_comet", CosmeticCategory.Trail, UnlockKind.Showcase, 3000, CosmeticTier.Legendary),
            TestData.Priced("party_pingi", CosmeticCategory.Celebration, UnlockKind.Showcase, 1500, CosmeticTier.Epic),
            TestData.Priced("trail_snow", CosmeticCategory.Trail, UnlockKind.Season, 700, CosmeticTier.Rare),
            TestData.Priced("frame_galaxy", CosmeticCategory.Frame, UnlockKind.SetReward, 0, CosmeticTier.Epic),
            TestData.Item("skin_streak", CosmeticCategory.Ball, UnlockKind.Streak, 7)
        };
    }

    private static Func<CosmeticItem, bool> Owning(params string[] ids)
    {
        var set = new HashSet<string>(ids);
        return item => item.Unlock == UnlockKind.Free || set.Contains(item.Id);
    }

    private static CosmeticItem By(List<CosmeticItem> catalog, string id) => catalog.First(i => i.Id == id);

    [Test]
    public void Clock_CountsLocalDaysAndSecondsToMidnight()
    {
        var evening = new DateTime(2026, 11, 15, 23, 59, 30);
        Assert.AreEqual(ShopClock.DayIndex(new DateTime(2026, 11, 15, 0, 0, 1)), ShopClock.DayIndex(evening));
        Assert.AreEqual(ShopClock.DayIndex(evening) + 1, ShopClock.DayIndex(new DateTime(2026, 11, 16, 0, 0, 0)));
        Assert.AreEqual(30, ShopClock.SecondsToMidnight(evening));
    }

    [Test]
    public void Showcase_IsStableForTheSameDayAndSeed()
    {
        var catalog = Catalog();
        var a = Showcase.Pick(catalog, Owning(), "player-1", 20400, 3);
        var b = Showcase.Pick(catalog, Owning(), "player-1", 20400, 3);
        CollectionAssert.AreEqual(a.Select(i => i.Id).ToList(), b.Select(i => i.Id).ToList());
        Assert.AreEqual(3, a.Count);
        Assert.AreEqual(3, a.Distinct().Count());
    }

    [Test]
    public void Showcase_ChangesAcrossDaysOrPlayers()
    {
        var catalog = Catalog();
        var seen = new HashSet<string>();
        for (int day = 20400; day < 20410; day++)
            seen.Add(string.Join(",", Showcase.Pick(catalog, Owning(), "player-1", day, 3).Select(i => i.Id)));
        Assert.Greater(seen.Count, 1, "Ten days must not all show the same three items");
    }

    [Test]
    public void Showcase_SkipsOwnedSeasonRewardsAndUnsellable()
    {
        var catalog = Catalog();
        var owned = Owning("ball_octo", "trail_leaf");
        for (int day = 20400; day < 20430; day++)
        {
            foreach (var item in Showcase.Pick(catalog, owned, "p", day, 3))
            {
                Assert.IsFalse(owned(item), item.Id);
                Assert.AreNotEqual(UnlockKind.Season, item.Unlock, item.Id);
                Assert.AreNotEqual(UnlockKind.SetReward, item.Unlock, item.Id);
                Assert.IsTrue(item.SoldForGold || item.Unlock == UnlockKind.Showcase, item.Id);
            }
        }
    }

    [Test]
    public void Showcase_FirstSlotIsShowcaseOnlyWhileAnyIsLeft()
    {
        var catalog = Catalog();
        for (int day = 20400; day < 20410; day++)
            Assert.AreEqual(UnlockKind.Showcase, Showcase.Pick(catalog, Owning(), "p", day, 3)[0].Unlock);

        var noneLeft = Owning("trail_comet", "party_pingi");
        foreach (var item in Showcase.Pick(catalog, noneLeft, "p", 20400, 3))
            Assert.AreNotEqual(UnlockKind.Showcase, item.Unlock);
    }

    [Test]
    public void Showcase_ReturnsFewerWhenAlmostEverythingIsOwned()
    {
        var catalog = Catalog();
        var owned = Owning("ball_octo", "ball_bunny", "ball_penguin", "ball_dragon", "trail_leaf", "trail_hearts", "trail_comet", "party_pingi");
        var pick = Showcase.Pick(catalog, owned, "p", 20400, 3);
        CollectionAssert.AreEquivalent(new[] { "hit_confetti" }, pick.Select(i => i.Id).ToArray());
    }

    [Test]
    public void Box_OddsMatchTheTableWhenAllTiersHaveItems()
    {
        var odds = SurpriseBox.Odds(Catalog(), Owning(), 70, 25, 5);
        Assert.AreEqual(70f, odds[CosmeticTier.Common], 0.001f);
        Assert.AreEqual(25f, odds[CosmeticTier.Rare], 0.001f);
        Assert.AreEqual(5f, odds[CosmeticTier.Epic], 0.001f);
        Assert.IsFalse(odds.ContainsKey(CosmeticTier.Legendary));
    }

    [Test]
    public void Box_OddsShareOutEmptyTiers()
    {
        var owned = Owning("ball_octo", "trail_leaf", "hit_confetti");
        var odds = SurpriseBox.Odds(Catalog(), owned, 70, 25, 5);
        Assert.IsFalse(odds.ContainsKey(CosmeticTier.Common));
        Assert.AreEqual(100f, odds.Values.Sum(), 0.001f);
        Assert.AreEqual(25f / 30f * 100f, odds[CosmeticTier.Rare], 0.001f);
    }

    [Test]
    public void Box_NeverDrawsOwnedLegendarySeasonOrRewards()
    {
        var catalog = Catalog();
        var owned = Owning("trail_leaf");
        for (int i = 0; i < 200; i++)
        {
            var item = SurpriseBox.Draw(catalog, owned, 70, 25, 5, (i * 0.618f) % 1f, (i * 0.377f) % 1f);
            Assert.IsNotNull(item);
            Assert.IsFalse(owned(item), item.Id);
            Assert.AreNotEqual(CosmeticTier.Legendary, item.Tier, item.Id);
            Assert.AreNotEqual(UnlockKind.Season, item.Unlock, item.Id);
            Assert.AreNotEqual(UnlockKind.SetReward, item.Unlock, item.Id);
        }
    }

    [Test]
    public void Box_RollPicksTheTier()
    {
        var catalog = Catalog();
        Assert.AreEqual(CosmeticTier.Common, SurpriseBox.Draw(catalog, Owning(), 70, 25, 5, 0.1f, 0f).Tier);
        Assert.AreEqual(CosmeticTier.Rare, SurpriseBox.Draw(catalog, Owning(), 70, 25, 5, 0.8f, 0f).Tier);
        Assert.AreEqual(CosmeticTier.Epic, SurpriseBox.Draw(catalog, Owning(), 70, 25, 5, 0.99f, 0f).Tier);
    }

    [Test]
    public void Box_IsEmptyWhenEverythingEligibleIsOwned()
    {
        var owned = Owning("ball_octo", "ball_bunny", "ball_penguin", "trail_leaf", "trail_hearts", "hit_confetti", "party_pingi");
        Assert.AreEqual(0, SurpriseBox.Odds(Catalog(), owned, 70, 25, 5).Count);
        Assert.IsNull(SurpriseBox.Draw(Catalog(), owned, 70, 25, 5, 0.5f, 0.5f));
    }

    [Test]
    public void Sets_CountProgressAndPayTheRewardOnce()
    {
        var catalog = Catalog();
        var set = new CosmeticSet("space", "Space set", new List<CosmeticItem> { By(catalog, "ball_octo"), By(catalog, "trail_leaf"), By(catalog, "hit_confetti"), By(catalog, "trail_comet") }, By(catalog, "frame_galaxy"));
        var sets = new List<CosmeticSet> { set };

        Assert.AreEqual(2, CosmeticSets.Progress(set, Owning("ball_octo", "trail_leaf")));
        Assert.AreEqual(0, CosmeticSets.RewardsDue(sets, Owning("ball_octo", "trail_leaf")).Count);

        var all = Owning("ball_octo", "trail_leaf", "hit_confetti", "trail_comet");
        Assert.IsTrue(CosmeticSets.Complete(set, all));
        CollectionAssert.AreEqual(new[] { "frame_galaxy" }, CosmeticSets.RewardsDue(sets, all).Select(i => i.Id).ToArray());

        var paid = Owning("ball_octo", "trail_leaf", "hit_confetti", "trail_comet", "frame_galaxy");
        Assert.AreEqual(0, CosmeticSets.RewardsDue(sets, paid).Count);
    }

    [Test]
    public void Season_OpensAcrossNewYearAndCountsDays()
    {
        var winter = new SeasonDef("winter", "Winter Fest", 12, 15, 1, 15, new List<CosmeticItem>());
        Assert.IsFalse(SeasonWindow.IsOpen(winter, new DateTime(2026, 12, 14, 23, 0, 0)));
        Assert.IsTrue(SeasonWindow.IsOpen(winter, new DateTime(2026, 12, 15, 0, 0, 0)));
        Assert.IsTrue(SeasonWindow.IsOpen(winter, new DateTime(2027, 1, 15, 23, 0, 0)));
        Assert.IsFalse(SeasonWindow.IsOpen(winter, new DateTime(2027, 1, 16, 0, 0, 0)));
        Assert.IsFalse(SeasonWindow.IsOpen(winter, new DateTime(2026, 7, 1)));

        Assert.AreEqual(32, SeasonWindow.DaysLeft(winter, new DateTime(2026, 12, 15)));
        Assert.AreEqual(1, SeasonWindow.DaysLeft(winter, new DateTime(2027, 1, 15)));
        Assert.AreEqual(0, SeasonWindow.DaysLeft(winter, new DateTime(2027, 2, 1)));

        var summer = new SeasonDef("summer", "Summer", 6, 1, 8, 31, new List<CosmeticItem>());
        Assert.IsTrue(SeasonWindow.IsOpen(summer, new DateTime(2027, 7, 4)));
        Assert.IsFalse(SeasonWindow.IsOpen(summer, new DateTime(2027, 9, 1)));
        Assert.AreSame(winter, SeasonWindow.Current(new List<SeasonDef> { summer, winter }, new DateTime(2026, 12, 20)));
        Assert.IsNull(SeasonWindow.Current(new List<SeasonDef> { summer, winter }, new DateTime(2026, 10, 1)));
    }

    [Test]
    public void OnSale_FollowsUnlockKind()
    {
        var catalog = Catalog();
        var winter = new SeasonDef("winter", "Winter Fest", 12, 15, 1, 15, new List<CosmeticItem> { By(catalog, "trail_snow") });
        var seasons = new List<SeasonDef> { winter };
        var showcase = new List<CosmeticItem> { By(catalog, "trail_comet") };
        var inSeason = new DateTime(2026, 12, 20);
        var outOfSeason = new DateTime(2026, 11, 20);

        Assert.IsTrue(ShopRules.OnSale(By(catalog, "trail_leaf"), seasons, showcase, outOfSeason));
        Assert.IsTrue(ShopRules.OnSale(By(catalog, "trail_snow"), seasons, showcase, inSeason));
        Assert.IsFalse(ShopRules.OnSale(By(catalog, "trail_snow"), seasons, showcase, outOfSeason));
        Assert.IsTrue(ShopRules.OnSale(By(catalog, "trail_comet"), seasons, showcase, outOfSeason));
        Assert.IsFalse(ShopRules.OnSale(By(catalog, "party_pingi"), seasons, showcase, outOfSeason));
        Assert.IsFalse(ShopRules.OnSale(By(catalog, "frame_galaxy"), seasons, showcase, outOfSeason));
        Assert.IsFalse(ShopRules.OnSale(By(catalog, "ball_pingi"), seasons, showcase, outOfSeason));
    }

    [Test]
    public void Discount_RoundsToWholeGold()
    {
        Assert.AreEqual(210, ShopRules.Discounted(300, 0.3f));
        Assert.AreEqual(490, ShopRules.Discounted(700, 0.3f));
        Assert.AreEqual(1050, ShopRules.Discounted(1500, 0.3f));
        Assert.AreEqual(300, ShopRules.Discounted(300, 0f));
    }
}
