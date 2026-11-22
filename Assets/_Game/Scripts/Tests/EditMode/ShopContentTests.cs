using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

public sealed class ShopContentTests
{
    private const string ContentPath = "Assets/_Game/Data/ShopContent.asset";

    private static ShopContent Content()
    {
        var content = AssetDatabase.LoadAssetAtPath<ShopContent>(ContentPath);
        Assert.IsNotNull(content, ContentPath);
        return content;
    }

    [Test]
    public void Catalog_HasUniqueIdsAndAFreeDefaultForEveryKind()
    {
        var catalog = TestData.LoadCatalog();
        var ids = catalog.Items.Where(i => i != null).Select(i => i.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Duplicate ids: " + string.Join(", ", ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key)));

        foreach (CosmeticCategory category in System.Enum.GetValues(typeof(CosmeticCategory)))
            Assert.IsNotNull(catalog.DefaultFor(category), "No free default for " + category);
    }

    [Test]
    public void EveryItem_HasItsArt()
    {
        foreach (var item in TestData.LoadCatalog().Items)
        {
            switch (item.Category)
            {
                case CosmeticCategory.Ball:
                    Assert.IsNotNull(item.Idle, item.Id);
                    Assert.IsNotNull(item.Happy, item.Id);
                    Assert.IsNotNull(item.Sad, item.Id);
                    Assert.IsNotNull(item.Wow, item.Id);
                    break;
                case CosmeticCategory.Paddle:
                    Assert.IsNotNull(item.PaddleSprite, item.Id);
                    break;
                case CosmeticCategory.Theme:
                    break;
                default:
                    Assert.IsNotNull(item.Icon, item.Id);
                    break;
            }
        }
    }

    [Test]
    public void SkinSprites_MatchTheDefaultSize()
    {
        var catalog = TestData.LoadCatalog();
        var paddleSize = catalog.DefaultFor(CosmeticCategory.Paddle).PaddleSprite.bounds.size;
        var ballSize = catalog.DefaultFor(CosmeticCategory.Ball).Idle.bounds.size;
        var wrong = new List<string>();

        foreach (var item in catalog.Items)
        {
            if (item.Category == CosmeticCategory.Paddle) Check(item.Id, item.PaddleSprite, paddleSize, wrong);
            if (item.Category != CosmeticCategory.Ball) continue;
            Check(item.Id + " idle", item.Idle, ballSize, wrong);
            Check(item.Id + " happy", item.Happy, ballSize, wrong);
            Check(item.Id + " sad", item.Sad, ballSize, wrong);
            Check(item.Id + " wow", item.Wow, ballSize, wrong);
        }

        Assert.IsEmpty(wrong, string.Join(", ", wrong));
    }

    private static void Check(string name, UnityEngine.Sprite sprite, UnityEngine.Vector3 expected, List<string> wrong)
    {
        if (sprite == null) return;
        var size = sprite.bounds.size;
        if (UnityEngine.Mathf.Abs(size.x - expected.x) > 0.005f || UnityEngine.Mathf.Abs(size.y - expected.y) > 0.005f)
            wrong.Add(name + " " + size.x.ToString("0.000") + "x" + size.y.ToString("0.000"));
    }

    [Test]
    public void GoldItems_UseTheFourTiers()
    {
        var prices = new Dictionary<CosmeticTier, int> { { CosmeticTier.Common, 300 }, { CosmeticTier.Rare, 700 }, { CosmeticTier.Epic, 1500 }, { CosmeticTier.Legendary, 3000 } };
        int total = 0;
        foreach (var item in TestData.LoadCatalog().Items)
        {
            if (item.Unlock != UnlockKind.Gold && item.Unlock != UnlockKind.Showcase) continue;
            Assert.AreNotEqual(CosmeticTier.None, item.Tier, item.Id);
            Assert.AreEqual(prices[item.Tier], item.GoldPrice, item.Id);
            total += item.GoldPrice;
        }
        Assert.AreEqual(31500, total, "First batch value");
    }

    [Test]
    public void EffectsAreNeverSoldForMoneyOrCoveredByPass()
    {
        foreach (var item in TestData.LoadCatalog().Items.Where(i => i.IsEffect))
        {
            Assert.IsTrue(string.IsNullOrEmpty(item.ProductId), item.Id);
            Assert.IsFalse(item.PassCovers, item.Id);
        }
    }

    [Test]
    public void Sets_HaveFourPartsAndARewardOnlyTheSetGives()
    {
        var content = Content();
        Assert.AreEqual(5, content.Sets.Count);
        foreach (var set in content.Sets)
        {
            Assert.AreEqual(4, set.Items.Count, set.Id);
            Assert.IsTrue(set.Items.All(i => i != null), set.Id);
            Assert.IsNotNull(set.Reward, set.Id);
            Assert.AreEqual(UnlockKind.SetReward, set.Reward.Unlock, set.Id);
            Assert.IsFalse(set.Items.Any(i => i.Unlock == UnlockKind.SetReward || i.Unlock == UnlockKind.Season), set.Id);
        }
    }

    [Test]
    public void WinterSeason_SellsItsThreeItems()
    {
        var content = Content();
        var winter = content.Seasons.Single(s => s.Id == "winter");
        Assert.AreEqual(12, winter.StartMonth);
        Assert.AreEqual(15, winter.StartDay);
        Assert.AreEqual(1, winter.EndMonth);
        Assert.AreEqual(15, winter.EndDay);
        Assert.AreEqual(3, winter.Items.Count);
        foreach (var item in winter.Items)
        {
            Assert.AreEqual(UnlockKind.Season, item.Unlock, item.Id);
            Assert.AreEqual("winter", item.SeasonId, item.Id);
            Assert.Greater(item.GoldPrice, 0, item.Id);
        }
    }

    [Test]
    public void Themes_HaveAStyleClass()
    {
        var themes = System.IO.File.ReadAllText("Assets/_Game/UI/Theme/Themes.uss");
        foreach (var item in TestData.LoadCatalog().InCategory(CosmeticCategory.Theme))
            if (!string.IsNullOrEmpty(item.UiClass)) StringAssert.Contains(".root." + item.UiClass + " ", themes, item.Id);
    }
}
