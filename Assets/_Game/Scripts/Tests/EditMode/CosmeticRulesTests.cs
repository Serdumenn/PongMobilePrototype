using NUnit.Framework;

public sealed class CosmeticRulesTests
{
    private static CosmeticInventory WithPass()
    {
        var inventory = CosmeticInventory.FromJson(null, "{}");
        inventory.GrantPass();
        return inventory;
    }

    [Test]
    public void Pass_UnlocksNewFacesPaddlesAndThemes()
    {
        var inventory = WithPass();
        Assert.IsTrue(inventory.IsUnlocked(TestData.Priced("ball_bunny", CosmeticCategory.Ball, UnlockKind.Gold, 700)));
        Assert.IsTrue(inventory.IsUnlocked(TestData.Priced("paddle_lava", CosmeticCategory.Paddle, UnlockKind.Gold, 700)));
        Assert.IsTrue(inventory.IsUnlocked(TestData.Priced("theme_space", CosmeticCategory.Theme, UnlockKind.Gold, 1500)));
        Assert.IsTrue(inventory.IsUnlocked(TestData.Priced("ball_snow", CosmeticCategory.Ball, UnlockKind.Season, 1500)));
    }

    [Test]
    public void Pass_DoesNotUnlockEffectsOrFrames()
    {
        var inventory = WithPass();
        Assert.IsFalse(inventory.IsUnlocked(TestData.Priced("trail_hearts", CosmeticCategory.Trail, UnlockKind.Gold, 700)));
        Assert.IsFalse(inventory.IsUnlocked(TestData.Priced("hit_bolt", CosmeticCategory.Hit, UnlockKind.Gold, 1500)));
        Assert.IsFalse(inventory.IsUnlocked(TestData.Priced("party_pingi", CosmeticCategory.Celebration, UnlockKind.Showcase, 1500)));
        Assert.IsFalse(inventory.IsUnlocked(TestData.Priced("frame_crown", CosmeticCategory.Frame, UnlockKind.Showcase, 3000)));
    }

    [Test]
    public void SetReward_NeedsOwnershipEvenWithPass()
    {
        var inventory = WithPass();
        var reward = TestData.Priced("frame_galaxy", CosmeticCategory.Frame, UnlockKind.SetReward, 0);
        var face = TestData.Priced("ball_reward", CosmeticCategory.Ball, UnlockKind.SetReward, 0);
        Assert.IsFalse(inventory.IsUnlocked(reward));
        Assert.IsFalse(inventory.IsUnlocked(face));

        inventory.Grant("frame_galaxy");
        Assert.IsTrue(inventory.IsUnlocked(reward));
    }

    [Test]
    public void FreeEffects_AreAlwaysUnlocked()
    {
        var inventory = CosmeticInventory.FromJson(null, "{}");
        Assert.IsTrue(inventory.IsUnlocked(TestData.Item("trail_basic", CosmeticCategory.Trail, UnlockKind.Free)));
        Assert.IsTrue(inventory.IsUnlocked(TestData.Item("frame_none", CosmeticCategory.Frame, UnlockKind.Free)));
    }

    [Test]
    public void SoldForGold_CoversGoldAndSeasonButNotShowcaseOrRewards()
    {
        Assert.IsTrue(TestData.Priced("trail_leaf", CosmeticCategory.Trail, UnlockKind.Gold, 300).SoldForGold);
        Assert.IsTrue(TestData.Priced("trail_snow", CosmeticCategory.Trail, UnlockKind.Season, 700).SoldForGold);
        Assert.IsFalse(TestData.Priced("trail_comet", CosmeticCategory.Trail, UnlockKind.Showcase, 3000).SoldForGold);
        Assert.IsFalse(TestData.Priced("frame_galaxy", CosmeticCategory.Frame, UnlockKind.SetReward, 0).SoldForGold);
        Assert.IsFalse(TestData.Priced("trail_free", CosmeticCategory.Trail, UnlockKind.Gold, 0).SoldForGold);
    }

    [Test]
    public void IsEffect_MarksOnlyTheNewKinds()
    {
        Assert.IsFalse(TestData.Item("a", CosmeticCategory.Ball, UnlockKind.Free).IsEffect);
        Assert.IsFalse(TestData.Item("b", CosmeticCategory.Theme, UnlockKind.Free).IsEffect);
        Assert.IsTrue(TestData.Item("c", CosmeticCategory.Trail, UnlockKind.Free).IsEffect);
        Assert.IsTrue(TestData.Item("d", CosmeticCategory.Hit, UnlockKind.Free).IsEffect);
        Assert.IsTrue(TestData.Item("e", CosmeticCategory.Celebration, UnlockKind.Free).IsEffect);
        Assert.IsTrue(TestData.Item("f", CosmeticCategory.Frame, UnlockKind.Free).IsEffect);
    }

    [Test]
    public void EquippedEffects_SurviveSaveAndFillEmptySlotsOnMerge()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        phone.SetEquipped(CosmeticCategory.Trail, "trail_hearts");
        phone.SetEquipped(CosmeticCategory.Hit, "hit_stars");
        phone.SetEquipped(CosmeticCategory.Celebration, "party_rain");
        phone.SetEquipped(CosmeticCategory.Frame, "frame_ice");

        var copy = CosmeticInventory.FromJson(null, phone.ToJson());
        Assert.AreEqual("trail_hearts", copy.GetEquipped(CosmeticCategory.Trail));
        Assert.AreEqual("hit_stars", copy.GetEquipped(CosmeticCategory.Hit));
        Assert.AreEqual("party_rain", copy.GetEquipped(CosmeticCategory.Celebration));
        Assert.AreEqual("frame_ice", copy.GetEquipped(CosmeticCategory.Frame));

        var fresh = CosmeticInventory.FromJson(null, "{}");
        fresh.SetEquipped(CosmeticCategory.Trail, "trail_leaf");
        fresh.MergeFrom(phone);
        Assert.AreEqual("trail_leaf", fresh.GetEquipped(CosmeticCategory.Trail), "A local choice wins over the backup");
        Assert.AreEqual("frame_ice", fresh.GetEquipped(CosmeticCategory.Frame), "An empty slot takes the backup's choice");
    }

    [Test]
    public void OldSave_LoadsWithEmptyEffectSlots()
    {
        var old = CosmeticInventory.FromJson(null, "{\"Version\":1,\"Owned\":[\"skin_minty\"],\"Ball\":\"ball_pingi\",\"GoldEarned\":40}");
        Assert.IsTrue(string.IsNullOrEmpty(old.GetEquipped(CosmeticCategory.Trail)));
        Assert.IsTrue(string.IsNullOrEmpty(old.GetEquipped(CosmeticCategory.Frame)));
        Assert.AreEqual("ball_pingi", old.GetEquipped(CosmeticCategory.Ball));
        Assert.AreEqual(40, old.Gold);
        Assert.AreEqual(0, old.ShowcaseDiscountDay);
    }

    [Test]
    public void ShowcaseOfTheDay_IsKeptUntilTheDayChanges()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        Assert.IsNull(phone.ShowcaseFor(20400));

        phone.KeepShowcase(20400, new[] { "trail_comet", "ball_bear", "hit_notes" });
        CollectionAssert.AreEqual(new[] { "trail_comet", "ball_bear", "hit_notes" }, phone.ShowcaseFor(20400));
        Assert.IsNull(phone.ShowcaseFor(20401));

        var copy = CosmeticInventory.FromJson(null, phone.ToJson());
        CollectionAssert.AreEqual(new[] { "trail_comet", "ball_bear", "hit_notes" }, copy.ShowcaseFor(20400));

        var older = CosmeticInventory.FromJson(null, "{}");
        older.KeepShowcase(20399, new[] { "ball_fox" });
        older.MergeFrom(phone);
        CollectionAssert.AreEqual(new[] { "trail_comet", "ball_bear", "hit_notes" }, older.ShowcaseFor(20400));
    }

    [Test]
    public void ShopSeed_IsMadeOnceAndTravelsWithTheBackup()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        string seed = phone.ShopSeed;
        Assert.IsFalse(string.IsNullOrEmpty(seed));
        Assert.AreEqual(seed, phone.ShopSeed);
        Assert.AreEqual(seed, CosmeticInventory.FromJson(null, phone.ToJson()).ShopSeed);

        var newPhone = CosmeticInventory.FromJson(null, "{}");
        newPhone.MergeFrom(phone);
        Assert.AreEqual(seed, newPhone.ShopSeed, "A restored backup brings the same daily showcase");
    }

    [Test]
    public void ShowcaseDiscount_OncePerDayAndMergeKeepsLatest()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        Assert.IsTrue(phone.UseShowcaseDiscount(20400));
        Assert.IsFalse(phone.UseShowcaseDiscount(20400));
        Assert.IsTrue(phone.UseShowcaseDiscount(20401));

        var other = CosmeticInventory.FromJson(null, "{}");
        other.UseShowcaseDiscount(20399);
        other.MergeFrom(phone);
        Assert.AreEqual(20401, other.ShowcaseDiscountDay);
        Assert.IsFalse(other.UseShowcaseDiscount(20401));
    }
}
