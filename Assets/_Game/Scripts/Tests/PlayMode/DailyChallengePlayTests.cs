using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class DailyChallengePlayTests : GameSceneFixture
{
    private IEnumerator WaitUntil(Func<bool> condition, float seconds, string because)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), because);
    }

    private IEnumerator LaunchDirection(Action<Vector2> found)
    {
        yield return WaitUntil(() => Game.Ball.WaitingForServe, 3f, "The ball waits for the serve");
        Game.Ball.LaunchNow();
        yield return new WaitForFixedUpdate();
        found(Game.Ball.LastVelocity.normalized);
    }

    [UnityTest]
    public IEnumerator Daily_EveryoneGetsTheSameServes()
    {
        yield return LoadGame();
        var rush = Game.FindMode("rush");
        int seed = DailyChallenge.SeedFor(DateTime.UtcNow);

        Vector2 first = default, second = default;
        Game.StartDailyRun(rush, seed);
        yield return LaunchDirection(v => first = v);

        Game.ReturnToMenu();
        yield return null;
        Game.StartDailyRun(rush, seed);
        yield return LaunchDirection(v => second = v);

        Assert.AreEqual(first.x, second.x, 0.0001f);
        Assert.AreEqual(first.y, second.y, 0.0001f);
        Assert.IsTrue(Game.DailyRun);
    }

    [UnityTest]
    public IEnumerator Daily_KeepsTodaysBestAndLeavesTheSoloBestAlone()
    {
        yield return LoadGame();
        int rushBest = Game.BestFor(Game.FindMode("rush"));

        Game.StartDailyRun(Game.FindMode("rush"), DailyChallenge.SeedFor(DateTime.UtcNow));
        Assert.IsTrue(Game.ScoreManager.Unranked);
        Game.ScoreManager.AddPoints(rushBest + 7);
        Game.GameOver();
        yield return null;

        Assert.AreEqual(rushBest + 7, DailyChallenge.BestToday(DateTime.UtcNow));
        Assert.AreEqual(rushBest, Game.BestFor(Game.FindMode("rush")), "Daily runs never change the Rush best");

        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        Assert.AreEqual("New best today!", root.Q("game-over").Q<Label>("title").text);
        StringAssert.StartsWith("Daily Challenge", root.Q("game-over").Q<Label>("score-caption").text);

        var ui = Object.FindFirstObjectByType<GameUI>();
        typeof(GameUI).GetMethod("RetryRun", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        Assert.IsTrue(Game.DailyRun, "Play again starts today's run again");
        Assert.AreEqual(DailyChallenge.SeedFor(DateTime.UtcNow), Game.RunSeed);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
    }
}
