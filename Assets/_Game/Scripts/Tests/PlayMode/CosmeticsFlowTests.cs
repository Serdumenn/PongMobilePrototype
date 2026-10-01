using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class CosmeticsFlowTests : GameSceneFixture
{
    private CosmeticsService cosmetics;

    private IEnumerator LoadWithCosmetics()
    {
        yield return LoadGame();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(cosmetics, "CosmeticsService missing in scene");
    }

    private CosmeticItem Item(string id)
    {
        var item = cosmetics.CatalogAsset.Find(id);
        Assert.IsNotNull(item, id);
        return item;
    }

    [UnityTest]
    public IEnumerator FreshProfile_WearsFreeStartersInWorld()
    {
        yield return LoadWithCosmetics();

        Assert.AreEqual("ball_pingi", cosmetics.Equipped(CosmeticCategory.Ball).Id);
        Assert.AreEqual("paddle_teal", cosmetics.Equipped(CosmeticCategory.Paddle).Id);
        Assert.AreEqual("theme_softpop", cosmetics.Equipped(CosmeticCategory.Theme).Id);

        Assert.AreEqual(Item("ball_pingi").Idle, BallSprite());
        Assert.AreEqual(Item("paddle_teal").PaddleSprite, PaddleSprite());
        Assert.AreEqual(Item("theme_softpop").BackgroundColor, Camera.main.backgroundColor);
    }

    [UnityTest]
    public IEnumerator Equip_ChangesWorldAndPersists()
    {
        yield return LoadWithCosmetics();

        var golden = Item("ball_golden");
        Assert.IsFalse(cosmetics.Equip(golden));
        Assert.AreEqual(Item("ball_pingi").Idle, BallSprite());

        var minty = Item("ball_minty");
        var mint = Item("theme_mint");
        cosmetics.Inventory.Grant(minty.Id);
        cosmetics.Inventory.Grant(mint.Id);

        Assert.IsTrue(cosmetics.Equip(minty));
        Assert.IsTrue(cosmetics.Equip(mint));
        yield return null;

        Assert.AreEqual(minty.Idle, BallSprite());
        Assert.AreEqual(mint.BackgroundColor, Camera.main.backgroundColor);
        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("root");
        Assert.IsTrue(string.IsNullOrEmpty(mint.UiClass) || root.ClassListContains(mint.UiClass));

        var saved = CosmeticInventory.Load(SaveLocation.PathFor("cosmetics.json"));
        Assert.AreEqual(minty.Id, saved.GetEquipped(CosmeticCategory.Ball));
        Assert.AreEqual(mint.Id, saved.GetEquipped(CosmeticCategory.Theme));
    }

    [UnityTest]
    public IEnumerator ClassicBest_UnlocksScoreRewardsAtGameOver()
    {
        yield return LoadWithCosmetics();

        Game.StartGameFromMenu();
        for (int i = 0; i < 10; i++) Game.RegisterPaddleHit(0f);
        Game.OnBallMissed();

        Assert.IsTrue(cosmetics.IsUnlocked(Item("ball_minty")));
        Assert.IsFalse(cosmetics.IsUnlocked(Item("paddle_coral")));
        CollectionAssert.Contains(cosmetics.PendingRewards, Item("ball_minty"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator RushScore_NeverUnlocksScoreRewards()
    {
        yield return LoadWithCosmetics();

        Game.SelectMode(Game.FindMode("rush"));
        Game.StartGameFromMenu();
        for (int i = 0; i < 12; i++) Game.RegisterPaddleHit(0f);
        Assert.GreaterOrEqual(Game.ScoreManager.Score, 30);
        Game.GameOver();

        Assert.IsFalse(cosmetics.IsUnlocked(Item("ball_minty")));
        Assert.IsEmpty(cosmetics.PendingRewards);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SeventhDay_UnlocksStreakReward()
    {
        var records = PlayerRecords.Load(SaveLocation.PathFor("records.json"));
        var today = DateTime.Now;
        for (int day = 6; day >= 1; day--) records.RecordRun("classic", false, 1, 10f, today.AddDays(-day));
        Assert.IsTrue(records.Save());

        yield return LoadWithCosmetics();
        Assert.AreEqual(6, cosmetics.CurrentDayStreak);
        Assert.IsFalse(cosmetics.IsUnlocked(Item("ball_astro")));

        Game.StartGameFromMenu();
        Game.OnBallMissed();

        Assert.AreEqual(7, cosmetics.CurrentDayStreak);
        Assert.IsTrue(cosmetics.IsUnlocked(Item("ball_astro")));
        CollectionAssert.Contains(cosmetics.PendingRewards, Item("ball_astro"));
        yield return null;
    }

    private Sprite BallSprite()
    {
        return Game.Ball.GetComponentInChildren<SpriteRenderer>().sprite;
    }

    private static Sprite PaddleSprite()
    {
        return Object.FindFirstObjectByType<Paddle>().GetComponentInChildren<SpriteRenderer>().sprite;
    }
}
