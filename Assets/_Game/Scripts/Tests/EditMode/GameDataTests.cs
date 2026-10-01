using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public sealed class GameDataTests
{
    [Test]
    public void Modes_KeepTheirTunedValues()
    {
        var classic = TestData.LoadMode(TestData.ClassicModePath);
        var rush = TestData.LoadMode(TestData.RushModePath);

        Assert.AreEqual("classic", classic.Id);
        Assert.AreEqual(GameModeKind.Classic, classic.Kind);
        Assert.AreEqual(SoloScoreManager.BestScoreKey, classic.BestScoreKey);
        Assert.AreEqual(0, classic.RequiredClassicBest);

        Assert.AreEqual("rush", rush.Id);
        Assert.AreEqual(GameModeKind.Rush, rush.Kind);
        Assert.AreEqual("bestScore_rush", rush.BestScoreKey);
        Assert.AreEqual(0, rush.RequiredClassicBest);
        Assert.AreEqual(60f, rush.DurationSeconds);
        Assert.AreEqual(5f, rush.MissPenaltySeconds);
        Assert.AreEqual(1f, rush.RespawnDelaySeconds);
        Assert.AreEqual(0.25f, rush.PerfectZone);
        Assert.AreEqual(2, rush.PerfectPoints);
        Assert.AreEqual(3, rush.ComboPoints);
        Assert.AreEqual(3, rush.ComboThreshold);
    }

    [Test]
    public void Catalog_HasNoMissingOrDuplicateItems()
    {
        var items = TestData.LoadCatalog().Items;

        Assert.AreEqual(19, items.Count);
        Assert.That(items, Has.None.Null);
        CollectionAssert.AllItemsAreUnique(items.Select(i => i.Id));
        Assert.That(items.Select(i => i.Id), Has.None.Null.And.None.Empty);
    }

    [Test]
    public void Catalog_HasExpectedCategoryCounts()
    {
        var catalog = TestData.LoadCatalog();

        Assert.AreEqual(9, catalog.InCategory(CosmeticCategory.Ball).Count);
        Assert.AreEqual(6, catalog.InCategory(CosmeticCategory.Paddle).Count);
        Assert.AreEqual(4, catalog.InCategory(CosmeticCategory.Theme).Count);
    }

    [Test]
    public void Catalog_DefaultsAreTheFreeStarters()
    {
        var catalog = TestData.LoadCatalog();

        Assert.AreEqual("ball_pingi", catalog.DefaultFor(CosmeticCategory.Ball).Id);
        Assert.AreEqual("paddle_teal", catalog.DefaultFor(CosmeticCategory.Paddle).Id);
        Assert.AreEqual("theme_softpop", catalog.DefaultFor(CosmeticCategory.Theme).Id);
    }

    [Test]
    public void Catalog_ProductsAreUniqueAndResolvable()
    {
        var catalog = TestData.LoadCatalog();
        var products = catalog.Items.Where(i => i.IsPurchasable).Select(i => i.ProductId).ToList();

        Assert.AreEqual(13, products.Count);
        CollectionAssert.AllItemsAreUnique(products);
        CollectionAssert.DoesNotContain(products, CosmeticCatalog.PassProductId);
        CollectionAssert.DoesNotContain(products, CosmeticCatalog.RemoveAdsProductId);
        foreach (var product in products) Assert.AreEqual(product, catalog.FindByProduct(product).ProductId);
    }

    [Test]
    public void Catalog_UnlockRulesMatchDesign()
    {
        var catalog = TestData.LoadCatalog();
        var expected = new Dictionary<string, (UnlockKind kind, int value)>
        {
            { "ball_pingi", (UnlockKind.Free, 0) },
            { "ball_minty", (UnlockKind.Score, 10) },
            { "ball_sunny", (UnlockKind.Score, 25) },
            { "ball_grape", (UnlockKind.Ads, 5) },
            { "ball_kitty", (UnlockKind.Ads, 10) },
            { "ball_panda", (UnlockKind.Ads, 10) },
            { "ball_froggy", (UnlockKind.Ads, 15) },
            { "ball_golden", (UnlockKind.PassOnly, 0) },
            { "ball_astro", (UnlockKind.Streak, 7) },
            { "paddle_teal", (UnlockKind.Free, 0) },
            { "paddle_coral", (UnlockKind.Score, 15) },
            { "paddle_candy", (UnlockKind.Ads, 5) },
            { "paddle_wood", (UnlockKind.Ads, 10) },
            { "paddle_rainbow", (UnlockKind.Ads, 15) },
            { "paddle_gold", (UnlockKind.PassOnly, 0) },
            { "theme_softpop", (UnlockKind.Free, 0) },
            { "theme_mint", (UnlockKind.Score, 30) },
            { "theme_sunset", (UnlockKind.Ads, 10) },
            { "theme_night", (UnlockKind.Ads, 15) }
        };

        foreach (var pair in expected)
        {
            var item = catalog.Find(pair.Key);
            Assert.IsNotNull(item, pair.Key);
            Assert.AreEqual(pair.Value.kind, item.Unlock, pair.Key);
            Assert.AreEqual(pair.Value.value, item.UnlockValue, pair.Key);
        }
    }

    [Test]
    public void Catalog_StreakRewardIsNeverSold()
    {
        var astro = TestData.LoadCatalog().Find("ball_astro");

        Assert.IsFalse(astro.IsPurchasable);
    }

    [Test]
    public void Catalog_BallsHaveAllExpressionsAndPaddlesHaveSprites()
    {
        var catalog = TestData.LoadCatalog();

        foreach (var ball in catalog.InCategory(CosmeticCategory.Ball))
        {
            Assert.IsNotNull(ball.Idle, ball.Id);
            Assert.IsNotNull(ball.Happy, ball.Id);
            Assert.IsNotNull(ball.Sad, ball.Id);
        }

        foreach (var paddle in catalog.InCategory(CosmeticCategory.Paddle))
            Assert.IsNotNull(paddle.PaddleSprite, paddle.Id);
    }
}
