using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class MomentsPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private RunMoments moments;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private IEnumerator Load()
    {
        yield return LoadGame();
        moments = Object.FindFirstObjectByType<RunMoments>();
        Assert.IsNotNull(moments, "The scene has no RunMoments");
    }

    private Label Banner()
    {
        return Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("hud").Q<Label>("banner");
    }

    private Paddle PlayerPaddle()
    {
        return Game.Player.Paddle;
    }

    private IEnumerator StartAndWaitForServe()
    {
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        Assert.IsTrue(Game.Ball.WaitingForServe, "The ball never got ready to serve");
    }

    private void Hits(int count)
    {
        for (int i = 0; i < count; i++) Game.RegisterPaddleHit(0.5f);
    }

    [UnityTest]
    public IEnumerator NewBest_CelebratesOnceWhenTheOldBestFalls()
    {
        yield return Load();
        PlayerPrefs.SetInt(Game.CurrentMode.BestScoreKey, 3);
        Game.RefreshBest();

        int events = 0;
        moments.NewBestReached += () => events++;
        Game.StartGameFromMenu();
        GameFeel.Instance.Clear();

        Hits(3);
        Assert.AreEqual(0, events, "Tying the best is not a new best");
        Assert.IsFalse(Banner().ClassListContains("banner--show"));

        Hits(1);
        Assert.AreEqual(1, events);
        Assert.IsTrue(moments.BestCelebrated);
        Assert.IsTrue(Banner().ClassListContains("banner--show"));
        Assert.IsTrue(Banner().ClassListContains("banner--record"));
        Assert.AreEqual("New best!", Banner().text);
        Assert.GreaterOrEqual(GameFeel.Instance.ActiveCount, 20, "No confetti");

        Hits(3);
        Assert.AreEqual(1, events, "A best is celebrated once per run");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator FirstRunEver_HasNoBestToCelebrate()
    {
        yield return Load();

        int events = 0;
        moments.NewBestReached += () => events++;
        Game.StartGameFromMenu();
        Hits(6);

        Assert.AreEqual(0, events);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Milestone_ShowsAtTenInARow()
    {
        yield return Load();

        var reached = new List<int>();
        moments.MilestoneReached += streak => reached.Add(streak);
        Game.StartGameFromMenu();

        Hits(9);
        Assert.IsEmpty(reached);

        Hits(1);
        CollectionAssert.AreEqual(new[] { 10 }, reached);
        Assert.IsTrue(Banner().ClassListContains("banner--show"));
        Assert.IsFalse(Banner().ClassListContains("banner--record"));
        Assert.AreEqual("10 in a row!", Banner().text);

        Hits(14);
        CollectionAssert.AreEqual(new[] { 10 }, reached);

        Hits(1);
        CollectionAssert.AreEqual(new[] { 10, 25 }, reached);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator NearMiss_AnEdgeSaveSlowsTimeBriefly()
    {
        yield return Load();
        Time.timeScale = 1f;

        int events = 0;
        moments.NearMissed += () => events++;
        yield return StartAndWaitForServe();
        Hits(3);

        var ball = Game.Ball;
        var paddle = PlayerPaddle();
        Vector2 home = paddle.transform.position;
        ball.ServeToward(new Vector2(home.x + paddle.HalfWidth - 0.1f, home.y + 1.4f), Vector2.down);

        yield return Until(() => events > 0 || !ball.InPlay);
        Assert.AreEqual(1, events, "The edge save was not noticed");
        Assert.IsTrue(GameFeel.Instance.Slowed);
        Assert.Less(Time.timeScale, 0.9f);

        yield return Until(() => !GameFeel.Instance.Slowed);
        Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator LastMiss_SlowsTheFallThenEndsTheRun()
    {
        yield return Load();
        Time.timeScale = 1f;

        yield return StartAndWaitForServe();
        var ball = Game.Ball;
        var look = Object.FindFirstObjectByType<CosmeticsService>().Equipped(CosmeticCategory.Ball);
        var view = ball.GetComponentInChildren<SpriteRenderer>();
        Vector2 home = PlayerPaddle().transform.position;
        ball.ServeToward(new Vector2(home.x + 1.5f, home.y + 1.4f), Vector2.down);

        yield return Until(() => moments.Falling || Game.State != SoloGameManager.GameState.Playing);
        Assert.IsTrue(moments.Falling, "The last miss did not slow down");
        Assert.Less(Time.timeScale, 0.5f);
        Assert.AreEqual(look.Sad, view.sprite);

        yield return Until(() => Game.State == SoloGameManager.GameState.GameOver);
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        Assert.IsFalse(GameFeel.Instance.Slowed);
    }

    [UnityTest]
    public IEnumerator RushMiss_WithTimeLeftKeepsFullSpeed()
    {
        yield return Load();
        Time.timeScale = 1f;

        Game.SelectMode(Game.FindMode("rush"));
        yield return StartAndWaitForServe();
        var ball = Game.Ball;
        Vector2 home = PlayerPaddle().transform.position;
        ball.ServeToward(new Vector2(home.x + 1.5f, home.y + 1.4f), Vector2.down);

        bool slowed = false;
        for (int i = 0; i < WaitFrames && ball.InPlay; i++)
        {
            slowed |= moments.Falling || Time.timeScale < 0.99f;
            yield return null;
        }

        Assert.IsFalse(ball.InPlay, "The ball never left");
        Assert.IsFalse(slowed, "A miss that does not end the run slowed time");
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Game.ReturnToMenu();
    }
}
