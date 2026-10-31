using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class AdventurePlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private AdventureRun adventure;
    private CosmeticsService cosmetics;
    private bool started;

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private static void Press(Button button)
    {
        Assert.IsNotNull(button);
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    private IEnumerator Load()
    {
        yield return LoadGame();
        adventure = Object.FindFirstObjectByType<AdventureRun>();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(adventure, "The scene has no AdventureRun");
        Assert.IsNotNull(adventure.BookAsset, "AdventureRun has no book");
        Object.FindFirstObjectByType<DailyMissions>().enabled = false;
        adventure.Started += () => started = true;
    }

    private void OpenUpTo(int index)
    {
        for (int i = 0; i < index; i++) cosmetics.RecordLevel(i, 1, 0);
    }

    private IEnumerator Begin(int index)
    {
        started = false;
        Assert.IsTrue(adventure.Play(index), "Stage " + index + " did not start");
        yield return Until(() => started && Game.Ball.WaitingForServe);
        Assert.IsTrue(started, "The stage never set up its field");
        Game.Ball.LaunchNow();
        yield return null;
    }

    private void Hits(int count)
    {
        for (int i = 0; i < count; i++) Game.RegisterPaddleHit(0.6f);
    }

    [UnityTest]
    public IEnumerator Menu_AdventureCardOpensTheMap()
    {
        yield return Load();
        Game.SelectMode(Game.FindMode(AdventureRun.ModeId));
        yield return null;

        Press(Root.Q("menu").Q<Button>("play-button"));
        yield return null;
        var map = Root.Q("adventure");
        Assert.AreEqual(DisplayStyle.Flex, map.resolvedStyle.display);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);

        var path = map.Q("adv-path");
        Assert.AreEqual(10, path.childCount);
        Assert.IsTrue(map.Q<Button>("adv-node-0").ClassListContains("adv-node--next"));
        Assert.IsTrue(map.Q<Button>("adv-node-1").ClassListContains("adv-node--locked"));
        Assert.IsTrue(map.Q<Button>("adv-node-9").ClassListContains("adv-node--boss"));

        Press(map.Q<Button>("adv-node-0"));
        yield return null;
        Assert.AreEqual(DisplayStyle.Flex, map.Q("adv-dialog").resolvedStyle.display);
        Assert.AreEqual("Stage 1-1", map.Q<Label>("adv-dialog-title").text);

        Press(map.Q<Button>("adv-dialog-play"));
        yield return null;
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.IsTrue(Game.InAdventure);
        Assert.AreEqual(0, adventure.Index);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator HitsStage_WinsWithThreeStarsAndPaysOnce()
    {
        yield return Load();
        var level = adventure.BookAsset.Level(0);
        int gold = cosmetics.Gold;

        yield return Begin(0);
        Assert.AreEqual(level.Target, adventure.Target);
        Assert.IsTrue(Root.Q("hud").Q("event-card").ClassListContains("event-card--hidden") == false, "The goal card is missing");
        Hits(level.Target);

        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(3, adventure.StarsEarned);
        Assert.IsTrue(adventure.FirstClear);
        Assert.AreEqual(3, cosmetics.LevelStarsAt(0));
        Assert.IsTrue(adventure.IsOpen(1));
        var purse = Object.FindFirstObjectByType<RunPurse>();
        Assert.AreEqual(gold + level.Gold + purse.LastRunGold, cosmetics.Gold);

        yield return Until(() => Root.Q("adventure-result").resolvedStyle.display == DisplayStyle.Flex);
        Assert.AreEqual("Stage clear!", Root.Q<Label>("adv-result-title").text);
        Assert.AreEqual("Next", Root.Q<Label>("adv-next-label").text);

        yield return Begin(0);
        Hits(level.Target);
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(0, adventure.GoldEarned, "Gold is paid on the first clear only");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Misses_CostTriesAndStars()
    {
        yield return Load();
        yield return Begin(0);

        Game.OnBallMissed();
        Assert.AreEqual(2, adventure.Rules.Lives);
        yield return null;
        Assert.IsTrue(Root.Q("hud").Q("life-2").ClassListContains("life--lost"));
        Assert.IsFalse(Root.Q("hud").Q("life-1").ClassListContains("life--lost"));

        Hits(adventure.Target);
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(2, adventure.StarsEarned);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator LosingAllTries_FailsAndKeepsTheNextStageLocked()
    {
        yield return Load();
        yield return Begin(0);

        for (int i = 0; i < 3; i++) Game.OnBallMissed();
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.IsFalse(adventure.Won);
        Assert.AreEqual(0, cosmetics.LevelStarsAt(0));
        Assert.IsFalse(adventure.IsOpen(1));

        yield return Until(() => Root.Q("adventure-result").resolvedStyle.display == DisplayStyle.Flex);
        Assert.AreEqual("So close!", Root.Q<Label>("adv-result-title").text);
        Assert.AreEqual("Play again", Root.Q<Label>("adv-next-label").text);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Revive_GivesOneMoreTry()
    {
        yield return Load();
        Game.ReviveAvailable = () => true;
        yield return Begin(0);

        for (int i = 0; i < 3; i++) Game.OnBallMissed();
        Assert.IsTrue(Game.AwaitingRevive);
        Game.AcceptRevive();
        Assert.AreEqual(1, adventure.Rules.Lives);

        Hits(adventure.Target);
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(1, adventure.StarsEarned);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator BeachStage_PutsTargetsAndBumpersOnTheField()
    {
        yield return Load();
        OpenUpTo(11);
        var board = Object.FindFirstObjectByType<TargetBoard>();
        var obstacles = Object.FindFirstObjectByType<FieldObstacles>();

        yield return Begin(11);
        var level = adventure.BookAsset.Level(11);
        Assert.AreEqual(level.TargetSpots.Count, board.Remaining);
        Assert.AreEqual(level.Bumpers.Count, obstacles.Count);

        board.TakeAll();
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(0, obstacles.Count, "Bumpers stay behind after the stage");
        Assert.AreEqual(0, board.Remaining);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Bumpers_BounceTheBall()
    {
        yield return Load();
        OpenUpTo(10);
        var obstacles = Object.FindFirstObjectByType<FieldObstacles>();

        yield return Begin(10);
        Assert.AreEqual(1, obstacles.Count);
        bool bumped = false;
        obstacles.Bumped += _ => bumped = true;

        var ball = Game.Ball;
        Vector2 bumper = obstacles.PositionOf(0);
        Vector2 from = bumper + Vector2.down * 2.5f;
        ball.ServeToward(from, Vector2.up);
        Vector2 before = ball.LastVelocity;
        yield return Until(() => bumped || !ball.InPlay);
        Assert.IsTrue(bumped, "The ball passed through the bumper");
        yield return new WaitForFixedUpdate();
        Assert.Greater(Vector2.Angle(before, ball.LastVelocity), 10f, "The bumper should turn the ball");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator SurviveStage_WinsWhenTheClockRunsOut()
    {
        yield return Load();
        OpenUpTo(6);
        yield return Begin(6);
        Assert.AreEqual(AdventureGoal.Survive, adventure.Level.Goal);

        adventure.Rules.Tick(adventure.Level.Target);
        Game.GameOver();
        Assert.IsTrue(adventure.Won);
        Assert.AreEqual(3, adventure.StarsEarned);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator CoinStage_CountsCoins()
    {
        yield return Load();
        OpenUpTo(3);
        var coins = Object.FindFirstObjectByType<GoldCoins>();

        yield return Begin(3);
        Assert.AreEqual(AdventureGoal.Coins, adventure.Level.Goal);
        Assert.IsTrue(coins.Running, "Coin stages should drop coins");
        for (int i = 0; i < adventure.Target; i++)
        {
            Assert.IsTrue(coins.Spawn(new Vector2(0f, i * 0.1f), false));
            coins.TakeAll();
        }

        Assert.IsTrue(adventure.Won);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator NextButton_StartsTheNextStage()
    {
        yield return Load();
        yield return Begin(0);
        Hits(adventure.Target);
        yield return Until(() => Root.Q("adventure-result").resolvedStyle.display == DisplayStyle.Flex);

        Press(Root.Q<Button>("adv-next-button"));
        yield return Until(() => Game.State == SoloGameManager.GameState.Playing && adventure.Index == 1);
        Assert.AreEqual(1, adventure.Index);
        Assert.IsTrue(Game.InAdventure);
        Game.ReturnToMenu();
    }
}
