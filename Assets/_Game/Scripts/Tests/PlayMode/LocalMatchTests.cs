using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class LocalMatchTests : GameSceneFixture
{
    private LocalMatchController match;
    private FieldLayout layout;
    private CosmeticsService cosmetics;

    private IEnumerator LoadMatch()
    {
        yield return LoadGame();
        match = Object.FindFirstObjectByType<LocalMatchController>();
        layout = Object.FindFirstObjectByType<FieldLayout>();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(match, "LocalMatchController missing in scene");
        Assert.AreEqual(LocalMatchController.MatchState.Idle, match.State);
    }

    private LocalMatchController.PlayerEntry Entry(FieldSide side, string ball)
    {
        return new LocalMatchController.PlayerEntry(side, cosmetics.CatalogAsset.Find(ball));
    }

    private IEnumerator StartMatch(string modeId, params LocalMatchController.PlayerEntry[] players)
    {
        match.Open(match.FindMode(modeId));
        Assert.AreEqual(LocalMatchController.MatchState.Setup, match.State);

        match.Begin(players);
        Assert.AreEqual(LocalMatchController.MatchState.Countdown, match.State);

        float deadline = Time.realtimeSinceStartup + 6f;
        while (match.State == LocalMatchController.MatchState.Countdown && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(LocalMatchController.MatchState.Playing, match.State);
    }

    private Participant Player(FieldSide side)
    {
        foreach (var p in match.Participants) if (p.Side == side) return p;
        return null;
    }

    private static Vector2 ZonePoint(FieldSide side, float worldX)
    {
        var cam = Camera.main;
        float x = cam.WorldToScreenPoint(new Vector3(worldX, 0f, 0f)).x;
        x = Mathf.Clamp(x, 1f, Screen.width - 2f);
        return new Vector2(x, side == FieldSide.Top ? Screen.height * 0.85f : Screen.height * 0.12f);
    }

    [UnityTest]
    public IEnumerator Duel_BuildsMirroredCourt()
    {
        yield return LoadMatch();
        yield return StartMatch("table_duel", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"));

        Assert.AreEqual(FieldTopology.TopBottom, layout.Topology);
        Assert.IsTrue(layout.IsOpen(FieldSide.Bottom));
        Assert.IsTrue(layout.IsOpen(FieldSide.Top));
        Assert.IsFalse(layout.IsOpen(FieldSide.Left));
        Assert.IsFalse(layout.IsOpen(FieldSide.Right));

        var bottom = Player(FieldSide.Bottom).Paddle;
        var top = Player(FieldSide.Top).Paddle;
        Assert.AreNotSame(bottom, top);
        Assert.AreEqual(FieldSide.Top, top.Side);
        Assert.AreEqual(180f, top.transform.eulerAngles.z, 1f);
        Assert.AreEqual(layout.Field.center.y * 2f - bottom.Home.y, top.Home.y, 0.05f);
        Assert.AreEqual(match.PaddleSpriteFor(FieldSide.Top), top.Visual.sprite);

        var ball = match.Balls[0];
        Assert.IsTrue(ball.Server == bottom || ball.Server == top);
        Assert.IsFalse(ball.InPlay);
    }

    [UnityTest]
    public IEnumerator Duel_ServerScoresWhenReceiverMisses()
    {
        yield return LoadMatch();
        yield return StartMatch("table_duel", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"));

        var ball = match.Balls[0];
        var server = Player(ball.Server.Side);
        var receiver = Player(server.Side == FieldSide.Bottom ? FieldSide.Top : FieldSide.Bottom);
        foreach (var col in receiver.Paddle.GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Touch(1, TouchPhase.Began, ZonePoint(server.Side, 0f));
        Time.timeScale = 3f;

        float deadline = Time.realtimeSinceStartup + 15f;
        while (server.Score == 0 && receiver.Score == 0 && Time.realtimeSinceStartup < deadline) yield return null;

        Touch(1, TouchPhase.Ended, ZonePoint(server.Side, 0f));

        Assert.AreEqual(1, server.Score, "Server should score when the receiver misses");
        Assert.AreEqual(0, receiver.Score);
        Assert.AreEqual(LocalMatchController.MatchState.Playing, match.State);

        yield return new WaitForSeconds(1.2f);
        Assert.AreSame(receiver.Paddle, ball.Server, "The player who lost the point serves next");
    }

    [UnityTest]
    public IEnumerator Duel_FingerStaysWithItsOwnPaddle()
    {
        yield return LoadMatch();
        yield return StartMatch("table_duel", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"));

        var bottom = Player(FieldSide.Bottom).Paddle;
        var top = Player(FieldSide.Top).Paddle;
        float topStart = top.transform.position.x;

        Touch(1, TouchPhase.Began, ZonePoint(FieldSide.Bottom, layout.Field.xMin));
        for (int i = 0; i < 20; i++) yield return null;

        Vector2 crossed = ZonePoint(FieldSide.Top, layout.Field.xMax);
        Touch(1, TouchPhase.Moved, crossed);
        for (int i = 0; i < 60; i++)
        {
            Touch(1, TouchPhase.Moved, crossed);
            yield return null;
        }

        Assert.Greater(bottom.transform.position.x, layout.Field.center.x + 0.5f, "Bottom paddle should follow its finger into the other half");
        Assert.AreEqual(topStart, top.transform.position.x, 0.05f, "Top paddle must ignore a finger that started in the other half");
        Touch(1, TouchPhase.Ended, crossed);
    }

    [UnityTest]
    public IEnumerator Coop_SharedLivesEndTheMatch()
    {
        yield return LoadMatch();
        yield return StartMatch("coop_rally", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"));

        Assert.AreEqual(3, match.Rules.TeamLives);
        foreach (var p in match.Participants)
            foreach (var col in p.Paddle.GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Touch(1, TouchPhase.Began, ZonePoint(FieldSide.Bottom, 0f));
        Touch(2, TouchPhase.Began, ZonePoint(FieldSide.Top, 0f));
        Time.timeScale = 4f;

        float deadline = Time.realtimeSinceStartup + 40f;
        while (match.State == LocalMatchController.MatchState.Playing && Time.realtimeSinceStartup < deadline) yield return null;

        Touch(1, TouchPhase.Ended, ZonePoint(FieldSide.Bottom, 0f));
        Touch(2, TouchPhase.Ended, ZonePoint(FieldSide.Top, 0f));
        Assert.AreEqual(LocalMatchController.MatchState.Result, match.State);
        Assert.AreEqual(0, match.Rules.TeamLives);
        Assert.AreEqual(1, Object.FindFirstObjectByType<RecordsService>().Records.MatchesPlayed);
    }

    [UnityTest]
    public IEnumerator Party_EliminatedSidesCloseUntilOneRemains()
    {
        yield return LoadMatch();
        yield return StartMatch("party_table", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"), Entry(FieldSide.Left, "ball_kitty"));

        Assert.AreEqual(FieldTopology.FourSides, layout.Topology);
        Assert.AreEqual(layout.Field.width, layout.Field.height, 0.001f);
        Assert.IsFalse(layout.IsOpen(FieldSide.Right), "Empty seat must be a wall");
        Assert.IsTrue(layout.IsOpen(FieldSide.Left));
        Assert.AreEqual(-90f, Mathf.DeltaAngle(0f, Player(FieldSide.Left).Paddle.transform.eulerAngles.z), 1f);

        foreach (var p in match.Participants)
            foreach (var col in p.Paddle.GetComponentsInChildren<Collider2D>()) col.enabled = false;

        Time.timeScale = 4f;
        float deadline = Time.realtimeSinceStartup + 60f;
        while (match.State == LocalMatchController.MatchState.Playing && Time.realtimeSinceStartup < deadline) yield return null;

        Assert.AreEqual(LocalMatchController.MatchState.Result, match.State);
        var winner = match.Rules.Winner;
        Assert.IsNotNull(winner);
        foreach (var p in match.Participants)
        {
            if (p == winner) continue;
            Assert.IsTrue(p.Eliminated);
            Assert.IsFalse(layout.IsOpen(p.Side), $"{p.Side} should be closed after elimination");
        }
    }

    [UnityTest]
    public IEnumerator Party_SecondBallJoinsAfterFifteenHits()
    {
        yield return LoadMatch();
        yield return StartMatch("party_table", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"), Entry(FieldSide.Left, "ball_kitty"));

        var struck = typeof(LocalMatchController).GetMethod("OnPaddleStruck", BindingFlags.Instance | BindingFlags.NonPublic);
        var ball = match.Balls[0];
        for (int i = 0; i < 15; i++)
        {
            var hitter = match.Participants[i % match.Participants.Count];
            struck.Invoke(match, new object[] { ball, hitter.Paddle, 0f });
        }

        Assert.AreEqual(2, match.Balls.Count);
        yield return null;
        yield return null;
        yield return null;

        var extra = match.Balls[1];
        Assert.AreNotSame(ball, extra);
        Assert.IsTrue(extra.InPlay, "Second ball should be served automatically");
        Assert.Greater(extra.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude, 1f);
    }

    [UnityTest]
    public IEnumerator Exit_RestoresSoloGame()
    {
        yield return LoadMatch();
        yield return StartMatch("party_table", Entry(FieldSide.Bottom, "ball_pingi"), Entry(FieldSide.Top, "ball_minty"), Entry(FieldSide.Left, "ball_kitty"));

        match.Exit();
        yield return null;
        yield return null;

        Assert.AreEqual(LocalMatchController.MatchState.Idle, match.State);
        Assert.AreEqual(FieldTopology.Solo, layout.Topology);
        Assert.IsTrue(layout.IsOpen(FieldSide.Bottom));
        Assert.IsFalse(layout.IsOpen(FieldSide.Top));
        Assert.AreEqual(1, Object.FindObjectsByType<Paddle>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(1, Object.FindObjectsByType<Ball>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);

        var paddle = Object.FindFirstObjectByType<Paddle>();
        Assert.AreEqual(FieldSide.Bottom, paddle.Side);
        Assert.AreEqual(0f, Mathf.DeltaAngle(0f, paddle.transform.eulerAngles.z), 1f);
        Assert.AreEqual(paddle.DefaultHome.y, paddle.Home.y, 0.001f);

        Game.StartGameFromMenu();
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Game.RegisterPaddleHit(0f);
        Game.OnBallMissed();
        Assert.AreEqual(SoloGameManager.GameState.GameOver, Game.State);
        Assert.AreEqual(1, PlayerPrefs.GetInt("bestScore"));
    }

    [UnityTest]
    public IEnumerator Ui_TogetherCardLeadsToADuelAndBackHome()
    {
        yield return LoadMatch();
        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

        Click(root.Q("menu").Q<Button>("mode-prev"));
        yield return null;
        Assert.AreEqual("Together", root.Q("menu").Q<Label>("mode-name").text);

        Click(root.Q("menu").Q<Button>("play-button"));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(DisplayStyle.Flex, root.Q("hub").resolvedStyle.display);

        Click(root.Q("hub").Q<Button>("tile-table_duel"));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(LocalMatchController.MatchState.Setup, match.State);

        Click(root.Q("match-setup").Q<Button>("ready-bottom"));
        Assert.AreEqual(LocalMatchController.MatchState.Setup, match.State);
        Click(root.Q("match-setup").Q<Button>("ready-top"));
        Assert.AreEqual(LocalMatchController.MatchState.Countdown, match.State);

        float deadline = Time.realtimeSinceStartup + 6f;
        while (match.State != LocalMatchController.MatchState.Playing && Time.realtimeSinceStartup < deadline) yield return null;

        Click(root.Q("match-hud").Q<Button>("pause-button"));
        Assert.AreEqual(LocalMatchController.MatchState.Paused, match.State);
        yield return null;

        Click(root.Q("pause").Q<Button>("home-button"));
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(LocalMatchController.MatchState.Idle, match.State);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
        Assert.AreEqual(DisplayStyle.Flex, root.Q("menu").resolvedStyle.display);
    }

    private static void Click(Button button)
    {
        Assert.IsNotNull(button);
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }
}
