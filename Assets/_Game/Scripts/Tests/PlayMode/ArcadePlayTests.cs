using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class ArcadePlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static CosmeticsService Cosmetics => Object.FindFirstObjectByType<CosmeticsService>();

    private IEnumerator StartMode(string id)
    {
        Game.SelectMode(Game.FindMode(id));
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
    }

    [UnityTest]
    public IEnumerator FreshPlayers_StartInArcade()
    {
        PlayerPrefs.DeleteKey("SelectedMode");
        yield return LoadGame();
        Assert.AreEqual("arcade", Game.CurrentMode.Id);
        Assert.AreEqual("arcade", Game.ModeList[0].Id);
    }

    [UnityTest]
    public IEnumerator Paddle_SitsAboveTheThumb()
    {
        yield return LoadGame();

        var layout = Object.FindFirstObjectByType<FieldLayout>();
        var paddle = Game.Player.Paddle;
        float lift = (paddle.Home.y - layout.Field.yMin) / layout.Field.height;
        Assert.GreaterOrEqual(lift, 0.17f, "The paddle sits where the thumb rests");
        Assert.LessOrEqual(lift, 0.25f);
    }

    [UnityTest]
    public IEnumerator EdgeHit_SendsTheBallThatWay()
    {
        yield return LoadGame();
        yield return StartMode("classic");

        var ball = Game.Ball;
        var paddle = Game.Player.Paddle;
        Vector2 home = paddle.transform.position;
        float drift = 1.5f / 10f * 1.4f;
        ball.ServeToward(new Vector2(home.x + paddle.HalfWidth * 0.7f - drift, home.y + 1.6f), Vector2.down);

        yield return Until(() => ball.LastVelocity.y > 0f || !ball.InPlay);
        Assert.Greater(ball.LastVelocity.y, 0f, "The ball never bounced");
        Assert.Greater(ball.LastVelocity.x, 2f, "A right edge hit should send the ball right");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Coins_OnlyFallInArcade()
    {
        yield return LoadGame();
        var coins = Object.FindFirstObjectByType<GoldCoins>();

        yield return StartMode("classic");
        Assert.IsFalse(coins.Running);
        Game.ReturnToMenu();

        yield return StartMode("arcade");
        Assert.IsTrue(coins.Running);
        Assert.IsTrue(Object.FindFirstObjectByType<PowerUps>().Running, "Arcade also brings power-ups");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Gold_IsCollectedPaidAndDoubled()
    {
        yield return LoadGame();
        var coins = Object.FindFirstObjectByType<GoldCoins>();
        var purse = Object.FindFirstObjectByType<RunPurse>();
        Object.FindFirstObjectByType<DailyMissions>().enabled = false;
        int before = Cosmetics.Gold;

        yield return StartMode("arcade");
        var hud = Root.Q("hud");
        Assert.IsTrue(Root.Q("hud").Q("gold").ClassListContains("gold-chip--hidden") == false, "No gold counter in Arcade");

        Assert.IsTrue(coins.Spawn(Vector2.zero, false));
        coins.TakeAll();
        Assert.AreEqual(5, purse.Collected);
        yield return null;
        Assert.AreEqual("5", hud.Q<Label>("gold-label").text);

        for (int i = 0; i < 10; i++) Game.RegisterPaddleHit(0.6f);
        int beforeEnd = Cosmetics.Gold;
        Game.GameOver();
        Assert.AreEqual(5 + 2, purse.LastRunGold);
        Assert.AreEqual(beforeEnd + 7, Cosmetics.Gold);
        Assert.GreaterOrEqual(Cosmetics.Gold, before + 7);

        Assert.IsTrue(purse.CanDouble);
        purse.ApplyDouble();
        Assert.AreEqual(beforeEnd + 14, Cosmetics.Gold);
        Assert.IsFalse(purse.CanDouble);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Classic_HasNoGoldCounter()
    {
        yield return LoadGame();
        yield return StartMode("classic");
        Assert.IsTrue(Root.Q("hud").Q("gold").ClassListContains("gold-chip--hidden"));
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Revive_LetsTheRunGoOnOnce()
    {
        yield return LoadGame();
        Game.ReviveAvailable = () => true;
        yield return StartMode("classic");

        Game.RegisterPaddleHit(0.5f);
        Game.RegisterPaddleHit(0.5f);
        Game.OnBallMissed();

        Assert.IsTrue(Game.AwaitingRevive);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        Assert.AreEqual(DisplayStyle.Flex, Root.Q("revive").style.display.value);

        Game.AcceptRevive();
        Assert.IsFalse(Game.AwaitingRevive);
        Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        Assert.AreEqual(2, Game.ScoreManager.Score, "The score must survive a revive");
        Assert.IsTrue(Game.Ball.WaitingForServe);

        Assert.IsTrue(Root.Q("revive").ClassListContains("screen--hidden"), "The continue card stayed open");

        Game.OnBallMissed();
        Assert.IsFalse(Game.AwaitingRevive, "Only one revive per run");
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
    }

    [UnityTest]
    public IEnumerator Revive_CanBeDeclined()
    {
        yield return LoadGame();
        Game.ReviveAvailable = () => true;
        yield return StartMode("arcade");

        Game.OnBallMissed();
        Assert.IsTrue(Game.AwaitingRevive);

        Game.DeclineRevive();
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
    }

    [UnityTest]
    public IEnumerator Revive_IsNeverOfferedInRush()
    {
        yield return LoadGame();
        Game.ReviveAvailable = () => true;
        yield return StartMode("rush");

        Game.OnBallMissed();
        Assert.IsFalse(Game.AwaitingRevive);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Shop_SellsACostumeForGold()
    {
        yield return LoadGame();
        var minty = Cosmetics.CatalogAsset.Find("ball_minty");
        Assert.IsFalse(Cosmetics.IsUnlocked(minty));
        Assert.IsFalse(Cosmetics.BuyWithGold(minty), "Bought without gold");

        Cosmetics.EarnGold(minty.GoldPrice + 40);
        Assert.IsTrue(Cosmetics.BuyWithGold(minty));
        Assert.IsTrue(Cosmetics.IsUnlocked(minty));
        Assert.AreEqual(40, Cosmetics.Gold);
        Assert.IsFalse(Cosmetics.BuyWithGold(minty), "Bought twice");
    }
}
