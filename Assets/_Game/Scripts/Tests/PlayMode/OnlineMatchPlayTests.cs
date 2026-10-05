using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class OnlineMatchPlayTests : GameSceneFixture
{
    private OnlineMatchController match;
    private FieldLayout layout;
    private Ball ball;
    private Paddle paddle;
    private FakeMatchLink link;

    private IEnumerator Open(bool host, string mode = "portal_duel")
    {
        yield return LoadGame();
        match = Object.FindFirstObjectByType<OnlineMatchController>();
        layout = Object.FindFirstObjectByType<FieldLayout>();
        ball = Game.Ball;
        paddle = Object.FindFirstObjectByType<Paddle>();
        Assert.IsNotNull(match, "OnlineMatchController missing in scene");

        var lobby = Object.FindFirstObjectByType<OnlineLobby>();
        link = new FakeMatchLink(host);
        match.Open(link, lobby.FindMode(mode), "ball_pingi", "Brave Otter", "ball_minty");
        Assert.AreEqual(OnlineMatchController.MatchState.Waiting, match.State);
        Assert.AreEqual(FieldTopology.Portal, layout.Topology);
    }

    private IEnumerator WaitForPlaying()
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (match.State != OnlineMatchController.MatchState.Playing && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(OnlineMatchController.MatchState.Playing, match.State);
    }

    private IEnumerator WaitUntil(Func<bool> condition, float seconds, string because)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), because);
    }

    private void DisablePaddle()
    {
        foreach (var col in paddle.GetComponentsInChildren<Collider2D>()) col.enabled = false;
    }

    private Vector2 PaddleTouch()
    {
        return ToScreen(Camera.main, paddle.transform.position);
    }

    [UnityTest]
    public IEnumerator Guest_ServeLeavesThroughThePortal()
    {
        yield return Open(false);
        link.Deliver(MatchMessage.Start(1, false));
        Assert.AreEqual(OnlineMatchController.MatchState.Countdown, match.State);
        yield return WaitForPlaying();
        Assert.IsTrue(match.MyServe, "The guest serves when the host does not");

        Touch(TouchPhase.Began, PaddleTouch());
        yield return WaitUntil(() => link.Count(MatchMessageType.Handoff) > 0, 8f, "Ball should reach the portal and be handed off");
        Touch(TouchPhase.Ended, PaddleTouch());

        var handoff = link.Last(MatchMessageType.Handoff).Value;
        Assert.Greater(handoff.DirY, 0f, "The ball leaves upward");
        Assert.That(handoff.X, Is.InRange(-1f, 1f));
        Assert.Greater(handoff.Speed, 0f);
        Assert.AreEqual("ball_pingi", handoff.Look);
        Assert.IsFalse(ball.InPlay, "The ball is gone from this court");
        Assert.AreEqual(0, link.Count(MatchMessageType.Miss), "Leaving through the portal is not a miss");
    }

    [UnityTest]
    public IEnumerator Guest_IncomingBallIsMirroredAndAMissIsReported()
    {
        yield return Open(false);
        link.Deliver(MatchMessage.Start(2, true));
        yield return WaitForPlaying();
        Assert.IsFalse(match.MyServe);
        DisablePaddle();

        link.Deliver(MatchMessage.Handoff(0, 0.6f, 0.3f, 0.95f, 6f, 4, "ball_minty"));
        yield return WaitUntil(() => ball.InPlay, 2f, "Ball should arrive after the portal delay");

        Assert.Less(ball.transform.position.x, layout.Field.center.x, "x is mirrored: the sender's right is my left");
        Assert.Less(ball.LastVelocity.y, 0f, "The ball comes down toward my paddle");
        Assert.Less(ball.LastVelocity.x, 0f, "Sideways motion is mirrored too");

        yield return WaitUntil(() => link.Count(MatchMessageType.Miss) > 0, 8f, "Missing the ball tells the host");

        link.Deliver(MatchMessage.Handoff(0, 0.6f, 0.3f, 0.95f, 6f, 4, "ball_minty"));
        yield return null;
        Assert.IsFalse(ball.InPlay, "A repeated handoff is ignored");

        var score = new OnlineMatchRules(false, 5, 3, true);
        score.Miss(false);
        link.Deliver(MatchMessage.Score(score));
        Assert.AreEqual(1, match.OpponentScore);
        Assert.AreEqual(0, match.MyScore);
        Assert.IsTrue(match.MyServe, "The guest lost the point, so the guest serves");
    }

    [UnityTest]
    public IEnumerator Guest_BallSentDuringMyCountdownStillArrives()
    {
        yield return Open(false);
        link.Deliver(MatchMessage.Start(4, true));
        Assert.AreEqual(OnlineMatchController.MatchState.Countdown, match.State);

        link.Deliver(MatchMessage.Handoff(0, -0.2f, 0.1f, 0.98f, 6f, 1, "ball_minty"));
        Assert.IsFalse(ball.InPlay, "The ball waits until my countdown is over");

        yield return WaitForPlaying();
        yield return WaitUntil(() => ball.InPlay, 2f, "A ball that crossed during the countdown lands once play starts");
        Assert.Less(ball.LastVelocity.y, 0f);
    }

    [UnityTest]
    public IEnumerator Host_ScoresGuestMissesOnceAndEndsTheMatch()
    {
        yield return Open(true);
        match.HostStart();
        var start = link.Last(MatchMessageType.Start);
        Assert.IsTrue(start.HasValue, "The host announces the start");
        yield return WaitForPlaying();

        link.Deliver(MatchMessage.Miss(0));
        Assert.AreEqual(1, match.MyScore);
        var score = link.Last(MatchMessageType.Score).Value;
        Assert.AreEqual(1, score.HostScore);
        Assert.IsFalse(score.HostServes, "The guest lost the point and serves next");

        link.Deliver(MatchMessage.Miss(0));
        Assert.AreEqual(1, match.MyScore, "A repeated miss is ignored");

        for (ushort seq = 1; seq < 5; seq++) link.Deliver(MatchMessage.Miss(seq));
        Assert.AreEqual(OnlineMatchController.MatchState.Result, match.State);
        Assert.IsTrue(match.Won);
        Assert.IsTrue(link.Last(MatchMessageType.Score).Value.Ended);

        link.Deliver(MatchMessage.RematchRequest());
        Assert.IsTrue(match.OpponentWantsRematch);
        int starts = link.Count(MatchMessageType.Start);
        match.RequestRematch();
        Assert.AreEqual(starts + 1, link.Count(MatchMessageType.Start), "Both want a rematch, so the host starts again");
        Assert.AreEqual(OnlineMatchController.MatchState.Countdown, match.State);
        Assert.AreEqual(0, match.MyScore);
    }

    [UnityTest]
    public IEnumerator Coop_CountsPassesAndSharesLives()
    {
        yield return Open(true, "coop_online");
        match.HostStart();
        yield return WaitForPlaying();
        DisablePaddle();

        link.Deliver(MatchMessage.Handoff(0, 0f, 0.1f, 1f, 5f, 1, "ball_minty"));
        Assert.AreEqual(1, match.Rules.Passes);

        yield return WaitUntil(() => link.Count(MatchMessageType.Score) > 0, 8f, "Host's own miss is scored");
        Assert.AreEqual(2, match.Rules.Lives);

        link.Deliver(MatchMessage.Miss(0));
        link.Deliver(MatchMessage.Miss(1));
        Assert.AreEqual(OnlineMatchController.MatchState.Result, match.State);
        Assert.AreEqual(0, match.Rules.Lives);
        Assert.AreEqual(1, match.Rules.Passes);
    }

    [UnityTest]
    public IEnumerator PeerDrop_WaitsThenTheOneWhoStayedWins()
    {
        yield return Open(false);
        typeof(OnlineMatchController).GetField("PeerWait", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(match, 0.8f);
        link.Deliver(MatchMessage.Start(3, true));
        yield return WaitForPlaying();

        link.SetPeer(false);
        Assert.IsTrue(match.PeerMissing);
        link.SetPeer(true);
        Assert.IsFalse(match.PeerMissing, "Coming back in time resumes the match");
        Assert.AreEqual(OnlineMatchController.MatchState.Playing, match.State);

        link.SetPeer(false);
        yield return WaitUntil(() => match.State == OnlineMatchController.MatchState.Result, 3f, "The match ends after the wait");
        Assert.IsTrue(match.Won, "The guest stayed, so the guest wins");

        match.Exit();
        Assert.IsTrue(link.Closed);
        Assert.AreEqual(OnlineMatchController.MatchState.Idle, match.State);
        Assert.AreEqual(FieldTopology.Solo, layout.Topology);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
    }
}
