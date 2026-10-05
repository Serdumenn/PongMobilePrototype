using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class GhostPlayTests : GameSceneFixture
{
    private IEnumerator WaitUntil(Func<bool> condition, float seconds, string because)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), because);
    }

    private static GhostRun Recording(int seed)
    {
        var run = new GhostRun { Seed = seed, BallSkin = "ball_kitty", PaddleSkin = string.Empty };
        run.AddBall(0f, new Vector2(0f, -0.6f), GhostRun.KeyKind.Launch);
        run.AddBall(2f, new Vector2(0.6f, 1f), GhostRun.KeyKind.Wall);
        run.AddBall(4f, new Vector2(0f, -0.8f), GhostRun.KeyKind.Paddle);
        run.AddScore(0.2f, 2);
        run.AddScore(0.4f, 5);
        run.AddBall(6f, new Vector2(-0.6f, 1f), GhostRun.KeyKind.Wall);
        run.AddBall(8f, new Vector2(0f, -0.8f), GhostRun.KeyKind.Paddle);
        run.AddBall(9f, new Vector2(0.2f, 0f), GhostRun.KeyKind.End);
        for (int i = 0; i <= 90; i++) run.AddPaddle(0.5f);
        run.Finish(9f, 5);
        return GhostRun.FromBytes(run.ToBytes());
    }

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static void Invoke(string method)
    {
        var ui = Object.FindFirstObjectByType<GameUI>();
        typeof(GameUI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
    }

    [UnityTest]
    public IEnumerator RushRun_IsKeptAsTheLastGhost()
    {
        yield return LoadGame();
        var recorder = Object.FindFirstObjectByType<GhostRecorder>();
        Assert.IsNotNull(recorder, "GhostRecorder missing in scene");
        Assert.IsNull(recorder.LastRun, "A clean profile has no ghost yet");

        Game.SelectMode(Game.FindMode("rush"));
        Game.StartGameFromMenu();
        Game.Player.Paddle.SetLengthScale(20f);
        yield return WaitUntil(() => Game.Ball.WaitingForServe, 3f, "The ball waits for the serve");
        Game.Ball.LaunchNow();
        Time.timeScale = 3f;
        yield return WaitUntil(() => recorder.Current != null && recorder.Current.PaddleHits >= 2, 20f, "The wide paddle returns the ball");
        Time.timeScale = 1f;
        Game.GameOver();
        yield return null;

        var run = recorder.LastRun;
        Assert.IsNotNull(run, "The finished Rush run is kept");
        Assert.IsNull(run.Problem());
        Assert.AreEqual(Game.ScoreManager.Score, run.Score);
        Assert.AreEqual(Game.RunSeed, run.Seed);
        Assert.AreEqual(GhostRun.KeyKind.Launch, run.Ball[0].Kind);
        Assert.GreaterOrEqual(run.PaddleHits, 2);
        Assert.Greater(run.Paddle.Count, 5);
        Assert.IsTrue(File.Exists(SaveLocation.PathFor(GhostRecorder.FileName)), "The ghost is saved for later");

        recorder.Forget();
        Assert.AreEqual(run.Score, recorder.LastRun.Score, "It loads back from disk");
    }

    [UnityTest]
    public IEnumerator Race_ReplaysTheGhostAndComparesAtTheEnd()
    {
        yield return LoadGame();
        var race = Object.FindFirstObjectByType<GhostRace>();
        Assert.IsNotNull(race, "GhostRace missing in scene");
        int rushBest = Game.BestFor(Game.FindMode("rush"));

        var ghost = Recording(777);
        Assert.IsTrue(race.Begin(ghost, "Brave Otter"));
        Assert.IsTrue(Game.GhostRun);
        Assert.IsTrue(Game.ScoreManager.Unranked, "Ghost races never change the Rush best");
        Assert.AreEqual(777, Game.RunSeed, "Same seed, same serves");

        yield return WaitUntil(() => Game.Ball.WaitingForServe, 3f, "The ball waits for the serve");
        Assert.IsFalse(race.BallShown, "The ghost waits for your first serve");
        Game.Ball.LaunchNow();
        yield return new WaitForSeconds(1f);

        Assert.IsTrue(race.BallShown);
        Assert.AreEqual(5, race.GhostScore);
        var root = Root;
        Assert.AreEqual("Brave Otter", root.Q("ghost-race").Q<Label>("ghost-name").text);
        Assert.AreEqual("5", root.Q("ghost-race").Q<Label>("ghost-score").text);
        var delta = root.Q("hud").Q<Label>("ghost-delta");
        Assert.IsFalse(delta.ClassListContains("ghost-delta--hidden"));
        Assert.AreEqual("5 behind", delta.text);

        Game.ScoreManager.AddPoints(7);
        yield return null;
        Assert.AreEqual("+2 ahead", delta.text);

        Game.GameOver();
        yield return null;
        var result = root.Q("ghost-result");
        Assert.AreEqual(DisplayStyle.Flex, result.resolvedStyle.display);
        Assert.AreEqual("You beat the ghost!", result.Q<Label>("title").text);
        Assert.AreEqual("7", result.Q<Label>("my-score").text);
        Assert.AreEqual("5", result.Q<Label>("ghost-score").text);
        Assert.IsFalse(race.BallShown);
        Assert.AreEqual(rushBest, Game.BestFor(Game.FindMode("rush")));

        Invoke("RetryGhost");
        Assert.IsTrue(Game.GhostRun, "Try again races the same ghost");
        Assert.AreEqual(777, Game.RunSeed);
        Assert.AreEqual(0, race.GhostScore);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);

        Invoke("HomeFromGhost");
        yield return null;
        Assert.IsFalse(race.Active);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
        Assert.AreEqual(DisplayStyle.Flex, root.Q("hub").resolvedStyle.display, "Home goes back to Challenges");
    }

    [UnityTest]
    public IEnumerator Race_LostToTheGhostSaysSo()
    {
        yield return LoadGame();
        var race = Object.FindFirstObjectByType<GhostRace>();
        race.Begin(Recording(31), "Calm Koala");
        yield return WaitUntil(() => Game.Ball.WaitingForServe, 3f, "The ball waits for the serve");
        Game.Ball.LaunchNow();
        Game.ScoreManager.AddPoints(2);
        yield return null;
        Game.GameOver();
        yield return null;

        var result = Root.Q("ghost-result");
        Assert.AreEqual("The ghost wins", result.Q<Label>("title").text);
        Assert.AreEqual("Ghost Challenge · Calm Koala", result.Q<Label>("caption").text);
        Assert.IsFalse(result.Q<Button>("send-button").enabledSelf, "Without a recorded hit there is nothing to send");
    }
}
