using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class EventsPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private ArcadeEvents events;
    private RunPurse purse;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private static VisualElement Hud => Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("hud");

    private IEnumerator StartArcade()
    {
        yield return LoadGame();
        events = Object.FindFirstObjectByType<ArcadeEvents>();
        purse = Object.FindFirstObjectByType<RunPurse>();
        Assert.IsNotNull(events, "The scene has no ArcadeEvents");
        Object.FindFirstObjectByType<DailyMissions>().enabled = false;

        Game.SelectMode(Game.FindMode("arcade"));
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        Game.Ball.LaunchNow();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Events_RunOnlyInArcade()
    {
        yield return LoadGame();
        events = Object.FindFirstObjectByType<ArcadeEvents>();
        Game.SelectMode(Game.FindMode("classic"));
        Game.StartGameFromMenu();
        Assert.IsFalse(events.Running);
        Game.ReturnToMenu();

        Game.SelectMode(Game.FindMode("arcade"));
        Game.StartGameFromMenu();
        Assert.IsTrue(events.Running);
        Assert.Greater(events.NextAt, 5f, "The first event should leave time to warm up");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Storm_SplitsTheBallAndForgivesMisses()
    {
        yield return StartArcade();

        events.Begin(EventKind.Storm);
        Assert.IsTrue(events.Active);
        Assert.AreEqual(4, events.StormBalls);
        Assert.AreEqual(5, events.Progress);
        Assert.IsTrue(Game.Sheltered);
        Assert.IsTrue(Hud.Q("event-card").ClassListContains("event-card--hidden") == false, "No event card");

        yield return null;
        yield return new WaitForFixedUpdate();
        int moving = 0;
        foreach (var ball in Object.FindObjectsByType<Ball>(FindObjectsSortMode.None))
            if (ball.InPlay) moving++;
        Assert.AreEqual(5, moving, "Storm balls must fly, not wait at the serve point");

        Game.OnBallMissed();
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State, "Misses inside the storm must not end the run");

        int score = Game.ScoreManager.Score;
        events.End();
        Assert.IsFalse(events.Active);
        Assert.AreEqual(0, events.StormBalls);
        Assert.IsFalse(Game.Sheltered);
        Assert.Greater(Game.ScoreManager.Score, score, "Surviving balls pay points");
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<Ball>(FindObjectsSortMode.None).Length, "Storm balls were left behind");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Storm_GivesTheBallBackWhenTheMainOneFell()
    {
        yield return StartArcade();

        events.Begin(EventKind.Storm);
        var ball = Game.Ball;
        Vector2 home = Game.Player.Paddle.transform.position;
        ball.ServeToward(new Vector2(home.x + 1.6f, home.y + 0.6f), Vector2.down);
        yield return Until(() => !ball.InPlay);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.AreEqual(4, events.Progress);

        events.End();
        yield return null;
        Assert.IsTrue(ball.InPlay || ball.WaitingForServe, "The main ball never came back");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator GoldRain_PaysWhenTheGoalIsMet()
    {
        yield return StartArcade();
        var coins = Object.FindFirstObjectByType<GoldCoins>();

        events.Begin(EventKind.GoldRain);
        Assert.IsTrue(coins.Raining);
        int goal = events.Target;
        int before = purse.Collected;

        for (int i = 0; i < goal; i++)
        {
            Assert.IsTrue(coins.Spawn(new Vector2(0f, i * 0.1f), false));
            coins.TakeAll();
        }

        Assert.IsFalse(events.Active, "Reaching the goal should finish the event");
        Assert.IsFalse(coins.Raining);
        Assert.AreEqual(before + goal * 5 + events.GoldOf(EventKind.GoldRain), purse.Collected);
        Assert.AreEqual(1, events.CompletedThisRun);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Targets_PayPointsAndGoAway()
    {
        yield return StartArcade();
        var board = Object.FindFirstObjectByType<TargetBoard>();

        events.Begin(EventKind.Targets);
        Assert.AreEqual(events.Target, board.Remaining);
        int score = Game.ScoreManager.Score;

        board.TakeAll();
        Assert.IsFalse(events.Active);
        Assert.AreEqual(0, board.Remaining);
        Assert.GreaterOrEqual(Game.ScoreManager.Score, score + 15);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Giant_GrowsTheBallAndPutsItBack()
    {
        yield return StartArcade();
        var ball = Game.Ball;
        Vector3 rest = ball.transform.localScale;

        events.Begin(EventKind.Giant);
        Assert.Greater(ball.transform.localScale.x, rest.x * 1.5f);
        Assert.Greater(ball.SpeedBoost, 1f);

        for (int i = 0; i < events.Target; i++) Game.RegisterPaddleHit(0.6f);
        Assert.IsFalse(events.Active);
        Assert.AreEqual(rest, ball.transform.localScale);
        Assert.AreEqual(1f, ball.SpeedBoost, 0.0001f);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator TimeRunningOut_FailsWithoutGold()
    {
        yield return StartArcade();

        bool? success = null;
        events.Finished += (kind, ok, gold) => success = ok;
        events.Begin(EventKind.Targets);
        int before = purse.Collected;

        events.End();
        yield return null;
        Assert.AreEqual(false, success);
        Assert.AreEqual(before, purse.Collected);
        Assert.AreEqual("Time's up!", Hud.Q<Label>("banner").text);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator GameOver_ClearsARunningEvent()
    {
        yield return StartArcade();
        var ball = Game.Ball;
        Vector3 rest = ball.transform.localScale;

        events.Begin(EventKind.Giant);
        Game.GameOver();
        Assert.IsFalse(events.Active);
        Assert.AreEqual(rest, ball.transform.localScale);
        Game.ReturnToMenu();
    }
}
