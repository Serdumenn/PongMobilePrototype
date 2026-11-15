using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class ShopExpansionPlayTests : GameSceneFixture
{
    private static readonly DateTime Autumn = new DateTime(2026, 11, 20, 10, 0, 0);
    private static readonly DateTime Winter = new DateTime(2026, 12, 20, 10, 0, 0);

    private CosmeticsService cosmetics;

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    private static VisualElement Shop => Root.Q("shop");

    private static void Invoke(string method)
    {
        var ui = Object.FindFirstObjectByType<GameUI>();
        typeof(GameUI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
    }

    private static void Click(VisualElement button)
    {
        Assert.IsNotNull(button, "Button missing");
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    private static bool Shown(VisualElement element)
    {
        return element != null && element.resolvedStyle.display != DisplayStyle.None;
    }

    private IEnumerator Open(DateTime now)
    {
        yield return LoadGame();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        cosmetics.Clock = () => now;
        Invoke("OpenShop");
        yield return null;
        yield return null;
    }

    private IEnumerator Reopen()
    {
        Invoke("CloseShop");
        yield return null;
        Invoke("OpenShop");
        yield return null;
        yield return null;
    }

    private static Button Card(VisualElement scope, CosmeticItem item)
    {
        return scope.Query<Button>(className: "item").ToList().FirstOrDefault(c => c.Q<Label>(className: "item__name")?.text == item.Title);
    }

    private static string[] GridNames()
    {
        return Shop.Q("grid").Query<Label>(className: "item__name").ToList().Select(l => l.text).ToArray();
    }

    private IEnumerator EffectTab(string filter)
    {
        Click(Shop.Q<Button>("tab-effect"));
        yield return null;
        if (filter != null)
        {
            Click(Shop.Q<Button>(filter));
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator EffectTab_ListsTrailsFirstAndFiltersSwitch()
    {
        yield return Open(Autumn);
        yield return EffectTab(null);

        Assert.IsTrue(Shown(Shop.Q("effect-filters")));
        CollectionAssert.AreEquivalent(cosmetics.CatalogAsset.InCategory(CosmeticCategory.Trail).Select(i => i.Title).ToArray(), GridNames());

        Click(Shop.Q<Button>("filter-hit"));
        yield return null;
        CollectionAssert.AreEquivalent(cosmetics.CatalogAsset.InCategory(CosmeticCategory.Hit).Select(i => i.Title).ToArray(), GridNames());

        Click(Shop.Q<Button>("filter-sets"));
        yield return null;
        Assert.IsTrue(Shown(Shop.Q("sets")));
        Assert.IsFalse(Shown(Shop.Q("grid")));
        Assert.AreEqual(5, Shop.Q("sets").Query(className: "set-panel").ToList().Count);

        Click(Shop.Q<Button>("filter-box"));
        yield return null;
        Assert.IsTrue(Shown(Shop.Q("box")));
        Assert.IsFalse(Shown(Shop.Q("sets")));

        Click(Shop.Q<Button>("tab-ball"));
        yield return null;
        Assert.IsFalse(Shown(Shop.Q("effect-filters")));
        Assert.IsTrue(Shown(Shop.Q("grid")));
    }

    [UnityTest]
    public IEnumerator Showcase_ShowsThreePicksAndACountdown()
    {
        yield return Open(Autumn);
        var showcase = Shop.Q("showcase");
        Assert.IsTrue(Shown(showcase));
        Assert.AreEqual(3, showcase.Query<Button>(className: "item").ToList().Count);
        StringAssert.IsMatch(@"^\d\d:\d\d:\d\d$", showcase.Q<Label>("showcase-timer").text);

        var picks = cosmetics.ShowcaseToday();
        Assert.AreEqual(UnlockKind.Showcase, picks[0].Unlock, "The first pick is a showcase-only item");
        Assert.IsNotNull(Card(showcase, picks[0]));
    }

    [UnityTest]
    public IEnumerator BuyingAnEffectWithGold_EquipsIt()
    {
        yield return Open(Autumn);
        var leaf = cosmetics.CatalogAsset.Find("trail_leaf");
        cosmetics.EarnGold(1000);
        yield return EffectTab(null);

        Click(Card(Shop.Q("grid"), leaf));
        yield return null;
        Assert.IsTrue(Shop.ShopScreenOpen(), "The item dialog opens");
        Assert.IsFalse(Shown(Shop.Q("dialog-pass")), "Effects are not part of Pingi Pass");

        Click(Shop.Q<Button>("dialog-gold"));
        yield return null;
        Assert.IsTrue(cosmetics.IsUnlocked(leaf));
        Assert.IsTrue(cosmetics.IsEquipped(leaf));
        Assert.AreEqual(700, cosmetics.Gold);
    }

    [UnityTest]
    public IEnumerator SeasonCard_ShowsOnlyDuringWinterFest()
    {
        yield return Open(Autumn);
        Assert.IsFalse(Shown(Shop.Q("season")));
        Assert.IsFalse(cosmetics.CanBuyWithGold(cosmetics.CatalogAsset.Find("trail_snow")));

        cosmetics.Clock = () => Winter;
        yield return Reopen();
        var season = Shop.Q("season");
        Assert.IsTrue(Shown(season));
        Assert.AreEqual(3, season.Query<Button>(className: "item").ToList().Count);
        cosmetics.EarnGold(1000);
        Assert.IsTrue(cosmetics.CanBuyWithGold(cosmetics.CatalogAsset.Find("trail_snow")));
    }

    [UnityTest]
    public IEnumerator Box_OpensForGoldAndRevealsANewItem()
    {
        yield return Open(Autumn);
        cosmetics.EarnGold(cosmetics.BoxPrice + 100);
        yield return EffectTab("filter-box");

        StringAssert.Contains("%", Shop.Q<Label>("box-odds").text);
        Click(Shop.Q<Button>("box-open"));
        yield return null;

        Assert.AreEqual(100, cosmetics.Gold);
        string prize = Shop.Q<Label>("box-prize-name").text;
        Assert.IsFalse(string.IsNullOrEmpty(prize));
        var item = cosmetics.CatalogAsset.Items.First(i => i.Title == prize);
        Assert.IsTrue(cosmetics.IsUnlocked(item));
        Assert.IsFalse(Shop.Q<Button>("box-open").enabledSelf, "Not enough gold for another box");
    }

    [UnityTest]
    public IEnumerator ShowcaseDiscount_WorksOncePerDay()
    {
        yield return Open(Autumn);
        cosmetics.Inventory.KeepShowcase(cosmetics.Today, new[] { "trail_comet", "trail_leaf", "hit_heart" });
        cosmetics.EarnGold(10000);
        yield return Reopen();
        var first = cosmetics.CatalogAsset.Find("trail_leaf");
        var second = cosmetics.CatalogAsset.Find("hit_heart");

        Click(Card(Shop.Q("showcase"), first));
        yield return null;
        Assert.IsTrue(Shown(Shop.Q("dialog-primary")), "The discount offer is shown");
        Click(Shop.Q<Button>("dialog-primary"));
        for (int i = 0; i < 600 && !cosmetics.IsUnlocked(first); i++) yield return null;

        Assert.IsTrue(cosmetics.IsUnlocked(first), "The discounted buy went through");
        Assert.AreEqual(10000 - cosmetics.DiscountedPrice(first), cosmetics.Gold);
        Assert.IsFalse(cosmetics.DiscountReady);

        Click(Card(Shop.Q("showcase"), second));
        yield return null;
        Assert.IsFalse(Shown(Shop.Q("dialog-primary")), "Only one discount a day");
        Assert.IsTrue(Shown(Shop.Q("dialog-gold")), "Full price is still offered");
    }

    [UnityTest]
    public IEnumerator FinishingASet_GivesItsReward()
    {
        yield return Open(Autumn);
        var forest = cosmetics.Sets.Single(s => s.Id == "forest");
        foreach (var part in forest.Items.Take(3)) cosmetics.Inventory.Grant(part.Id);
        Assert.IsFalse(cosmetics.IsUnlocked(forest.Reward));

        cosmetics.EarnGold(1000);
        Assert.IsTrue(cosmetics.BuyWithGold(forest.Items[3]));
        Assert.IsTrue(cosmetics.IsUnlocked(forest.Reward), "Completing the set unlocks its reward");
    }
}

internal static class ShopTestExtensions
{
    public static bool ShopScreenOpen(this VisualElement shop)
    {
        var dialog = shop.Q("dialog");
        return dialog != null && !dialog.ClassListContains("dialog--hidden");
    }
}
