using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class GameFlowTests : GameSceneFixture
{
    private const float LaunchSpeed = 6f;
    private const float MaxSpeed = 16f;
    private const float SpeedGrowth = 1.015f;

    [UnityTest]
    public IEnumerator Classic_RunScoresAndEndsOnFirstMiss()
    {
        yield return LoadGame();
        Assert.AreEqual("classic", Game.CurrentMode.Id);

        int hitEvents = 0;
        Game.HitScored += _ => hitEvents++;

        Game.StartGameFromMenu();
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.IsInstanceOf<ClassicRules>(Game.Rules);

        Game.RegisterPaddleHit(0f);
        Game.RegisterPaddleHit(0.5f);
        Game.RegisterPaddleHit(-1f);
        Assert.AreEqual(3, Game.ScoreManager.Score);
        Assert.AreEqual(3, hitEvents);

        Game.OnBallMissed();
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(Game.ScoreManager.IsNewBest);
        Assert.AreEqual(3, PlayerPrefs.GetInt("bestScore"));

        var records = Game.RecordsSource.Records;
        Assert.AreEqual(1, records.GamesPlayed);
        Assert.AreEqual(3, records.TotalHits);
        Assert.AreEqual(3, records.LongestStreak);
        Assert.AreEqual(1, records.CurrentDayStreak(DateTime.Now));
        Assert.IsNotNull(records.BestDate("classic"));
        Assert.IsTrue(File.Exists(SaveLocation.PathFor("records.json")));

        Game.ReturnToMenu();
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
        Assert.AreEqual(1f, Time.timeScale);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Rush_MissCostsTimeAndRespawnsBall()
    {
        yield return LoadGame();

        Game.SelectMode(Game.FindMode("rush"));
        Assert.AreEqual("rush", Game.CurrentMode.Id);
        Assert.AreEqual("rush", PlayerPrefs.GetString("SelectedMode"));

        Game.StartGameFromMenu();
        Assert.IsInstanceOf<RushRules>(Game.Rules);
        Assert.AreEqual(60f, Game.Rules.TimeRemaining);

        Game.RegisterPaddleHit(0f);
        Game.RegisterPaddleHit(0f);
        Game.RegisterPaddleHit(0f);
        Assert.AreEqual(7, Game.ScoreManager.Score);

        int roundResets = 0;
        Game.Ball.RoundReset += () => roundResets++;

        Game.OnBallMissed();
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.AreEqual(55f, Game.Rules.TimeRemaining, 0.0001f);

        yield return new WaitForSeconds(1.25f);
        Assert.AreEqual(1, roundResets);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);

        Game.GameOver();
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.AreEqual(7, PlayerPrefs.GetInt("bestScore_rush"));
        Assert.IsFalse(PlayerPrefs.HasKey("bestScore"));
        Assert.IsNotNull(Game.RecordsSource.Records.BestDate("rush"));
        Assert.IsNull(Game.RecordsSource.Records.BestDate("classic"));
    }

    [UnityTest]
    public IEnumerator Pause_FreezesAndAbandonedRunIsNotRecorded()
    {
        yield return LoadGame();

        Game.StartGameFromMenu();
        Game.SetPaused(true);
        Assert.AreEqual(SoloGameManager.GameState.Paused, Game.State);
        Assert.AreEqual(0f, Time.timeScale);

        Game.RegisterPaddleHit(0f);
        Assert.AreEqual(0, Game.ScoreManager.Score);

        Game.SetPaused(false);
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.AreEqual(1f, Time.timeScale);

        Game.SetPaused(true);
        Game.ReturnToMenu();
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
        Assert.AreEqual(0, Game.RecordsSource.Records.GamesPlayed);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Rally_FollowingPaddleScoresAndMissEndsRun()
    {
        yield return LoadGame();

        var ball = Game.Ball;
        var body = ball.GetComponent<Rigidbody2D>();
        var paddle = Object.FindFirstObjectByType<Paddle>();
        var cam = Camera.main;
        float paddleY = paddle.transform.position.y;

        int launches = 0, paddleHits = 0, wallHits = 0, misses = 0;
        ball.Launched += () => launches++;
        ball.PaddleHit += () => paddleHits++;
        ball.WallHit += () => wallHits++;
        ball.Missed += () => misses++;

        Game.SetSeedForNextRun(20261001);
        Game.StartGameFromMenu();
        Assert.AreEqual(20261001, Game.RunSeed);
        Touch(TouchPhase.Began, ToScreen(cam, new Vector2(0f, paddleY)));

        float deadline = Time.realtimeSinceStartup + 5f;
        while (launches == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(1, launches, $"Ball did not launch on touch (active={paddle.HasActiveInput}, screen={Screen.width}x{Screen.height})");
        Assert.Greater(body.linearVelocity.y, 0f);
        Assert.AreEqual(LaunchSpeed, body.linearVelocity.magnitude, 0.01f);

        Time.timeScale = 3f;
        deadline = Time.realtimeSinceStartup + 60f;
        while (paddleHits < 8 && misses == 0 && Time.realtimeSinceStartup < deadline)
        {
            Touch(TouchPhase.Moved, ToScreen(cam, new Vector2(body.position.x, paddleY)));
            yield return null;
        }

        Assert.AreEqual(0, misses, "Following paddle missed the ball");
        Assert.AreEqual(8, paddleHits);
        Assert.AreEqual(paddleHits, Game.ScoreManager.Score);
        Assert.Greater(wallHits, 0);
        float expectedSpeed = Mathf.Min(LaunchSpeed * Mathf.Pow(SpeedGrowth, paddleHits), MaxSpeed);
        Assert.AreEqual(expectedSpeed, body.linearVelocity.magnitude, expectedSpeed * 0.05f);

        deadline = Time.realtimeSinceStartup + 60f;
        while (misses == 0 && Time.realtimeSinceStartup < deadline)
        {
            float away = body.position.x > 0f ? -20f : 20f;
            Touch(TouchPhase.Moved, ToScreen(cam, new Vector2(away, paddleY)));
            yield return null;
        }

        Touch(TouchPhase.Ended, ToScreen(cam, new Vector2(0f, paddleY)));
        Assert.AreEqual(1, misses);
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.AreEqual(1, Game.RecordsSource.Records.GamesPlayed);
        Assert.AreEqual(Game.ScoreManager.Score, PlayerPrefs.GetInt("bestScore"));
    }

    [UnityTest]
    public IEnumerator SameSeed_ServesTheSameWay()
    {
        yield return LoadGame();
        var paddle = Object.FindFirstObjectByType<Paddle>();
        var body = Game.Ball.GetComponent<Rigidbody2D>();
        var cam = Camera.main;
        Vector2 press = ToScreen(cam, new Vector2(0f, paddle.transform.position.y));

        var serves = new Vector2[3];
        int[] seeds = { 7, 7, 8 };
        for (int i = 0; i < seeds.Length; i++)
        {
            int launches = 0;
            void OnLaunch() => launches++;
            Game.Ball.Launched += OnLaunch;

            Game.SetSeedForNextRun(seeds[i]);
            Game.StartGameFromMenu();
            Touch(TouchPhase.Began, press);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (launches == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            Game.Ball.Launched -= OnLaunch;
            Assert.AreEqual(1, launches, $"Serve {i} did not launch");

            serves[i] = body.linearVelocity;
            Touch(TouchPhase.Ended, press);
            Game.ReturnToMenu();
            yield return null;
        }

        Assert.AreEqual(serves[0].x, serves[1].x, 0.0001f);
        Assert.AreEqual(serves[0].y, serves[1].y, 0.0001f);
        Assert.Greater(serves[0].y, 0f);
        Assert.AreNotEqual(serves[0], serves[2]);
    }
}
