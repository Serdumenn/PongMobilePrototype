using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class LiveDuelPlayTests : GameSceneFixture
{
    private OnlineMatchController match;
    private LiveDuel live;
    private FieldLayout layout;
    private FakeMatchLink link;
    private LiveSession remote;
    private bool touching;

    private IEnumerator Open(bool host, int pointsToWin = 5)
    {
        touching = false;
        remote = null;
        yield return LoadGame();
        match = Object.FindFirstObjectByType<OnlineMatchController>();
        live = Object.FindFirstObjectByType<LiveDuel>();
        layout = Object.FindFirstObjectByType<FieldLayout>();
        Assert.IsNotNull(live, "LiveDuel missing in scene");

        var lobby = Object.FindFirstObjectByType<OnlineLobby>();
        var mode = lobby.FindMode("live_duel");
        Assert.IsNotNull(mode, "Live Duel is in the online mode list");
        Assert.IsFalse(mode.ComingSoon, "Live Duel is playable");
        if (pointsToWin != mode.PointsToWin)
        {
            mode = Object.Instantiate(mode);
            typeof(GameModeDefinition).GetField("<PointsToWin>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(mode, pointsToWin);
        }

        link = new FakeMatchLink(host);
        match.Open(link, mode, "ball_pingi", "Brave Otter", "ball_minty");
        Assert.AreEqual(FieldTopology.Live, layout.Topology);
        Assert.IsTrue(match.IsLive);
    }

    private void StartRemote(int seed, bool hostServes)
    {
        remote = new LiveSession(seed, hostServes, !link.IsHost, match.Mode.PointsToWin, data => link.DeliverLive(data));
    }

    private IEnumerator Drive(float seconds, Func<LiveState, ushort> remoteInput, bool followWithTouch, Func<bool> until = null)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end && (until == null || !until()))
        {
            foreach (var packet in link.SentLive) remote.Receive(packet);
            link.SentLive.Clear();
            remote.Update(Time.unscaledDeltaTime, remoteInput(remote.State));

            if (followWithTouch && live.Session != null)
            {
                var target = ToScreen(Camera.main, new Vector2(live.BallViewPosition.x, live.MyPaddleViewPosition.y));
                Touch(touching ? TouchPhase.Moved : TouchPhase.Began, target);
                touching = true;
            }
            yield return null;
        }
    }

    private void AssertCleanTable()
    {
        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        Assert.AreEqual(DisplayStyle.None, root.Q("menu").resolvedStyle.display, "The main menu never shows under a live duel");
        Assert.IsFalse(Object.FindFirstObjectByType<Paddle>().Visual.enabled, "The solo paddle stays hidden");
        Assert.IsFalse(Game.Ball.GetComponentInChildren<SpriteRenderer>().enabled, "The solo ball stays hidden");
    }

    private static ushort Follow(LiveState s)
    {
        return LiveInput.Pack(LiveInput.FromCourtX(s.BallX.ToFloat()), true);
    }

    private static ushort Away(LiveState s)
    {
        return LiveInput.Pack(LiveInput.FromCourtX(s.BallX.ToFloat() > 0f ? -2.2f : 2.2f), true);
    }

    private IEnumerator WaitForPlaying()
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (match.State != OnlineMatchController.MatchState.Playing && Time.realtimeSinceStartup < deadline)
        {
            foreach (var packet in link.SentLive) remote?.Receive(packet);
            link.SentLive.Clear();
            yield return null;
        }
        Assert.AreEqual(OnlineMatchController.MatchState.Playing, match.State);
    }

    [UnityTest]
    public IEnumerator HidingDuringTheMenuEntrance_Sticks()
    {
        yield return LoadGame();
        var entrance = Game.Ball.GetComponent<GameObjectEntrance>();
        Assert.IsTrue(entrance != null && entrance.Running, "The menu entrance is still animating right after loading");

        Game.HideGameObjects();
        float end = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < end) yield return null;

        Assert.IsFalse(Game.Ball.GetComponentInChildren<SpriteRenderer>().enabled, "A cancelled entrance does not bring the ball back");
        Assert.IsFalse(Object.FindFirstObjectByType<Paddle>().Visual.enabled, "Nor the paddle");

        Game.ShowGameObjects();
        Assert.IsTrue(Game.Ball.GetComponentInChildren<SpriteRenderer>().enabled);
        Assert.AreEqual(1f, Game.Ball.GetComponentInChildren<SpriteRenderer>().color.a, 0.001f, "Showing again is fully opaque");
    }

    [UnityTest]
    public IEnumerator Host_RallyStaysInStepWithTheOtherPhone()
    {
        yield return Open(true);
        match.HostStart();
        var start = link.Last(MatchMessageType.Start).Value;
        StartRemote(start.Seed, start.HostServes);
        Assert.IsFalse(Object.FindFirstObjectByType<Paddle>().Visual.enabled, "The solo paddle hides during a live duel");

        yield return WaitForPlaying();
        yield return Drive(6f, Follow, true);
        AssertCleanTable();

        var session = live.Session;
        Assert.Greater(session.Tick, 250, "The live sim keeps ticking");
        Assert.Greater(session.RemoteConfirmed, 200, "Inputs flow both ways");
        int at = Mathf.Min(session.RemoteConfirmed, remote.RemoteConfirmed) - 2;
        Assert.IsTrue(session.TryGetState(at, out var mine));
        Assert.IsTrue(remote.TryGetState(at, out var theirs));
        Assert.AreEqual(mine.Checksum(), theirs.Checksum(), "Both phones see the same match");
        Assert.IsFalse(session.Desynced);

        Rect field = layout.Field;
        Assert.Less(live.MyPaddleViewPosition.y, field.center.y, "My paddle is at the bottom");
        Assert.Greater(live.OpponentPaddleViewPosition.y, field.center.y, "The opponent plays at the top");
        Assert.That(live.BallViewPosition.x, Is.InRange(field.xMin - 0.1f, field.xMax + 0.1f));
        Touch(TouchPhase.Ended, Vector2.zero);
    }

    [UnityTest]
    public IEnumerator Guest_SeesTheTableTurnedAround()
    {
        yield return Open(false);
        link.Deliver(MatchMessage.Start(77, true));
        StartRemote(77, true);
        yield return WaitForPlaying();

        float startY = 0f;
        bool served = false;
        yield return Drive(4f, Follow, false, () =>
        {
            if (!served && remote.State.Phase == LivePhase.Play && live.BallShown)
            {
                served = true;
                startY = live.BallViewPosition.y;
            }
            return served && remote.State.Tick > 80 && live.BallViewPosition.y < startY - 0.5f;
        });

        Assert.IsTrue(served, "The host served");
        AssertCleanTable();
        Assert.Less(live.BallViewPosition.y, startY - 0.5f, "The host's serve comes down toward the guest");
        Assert.Less(live.MyPaddleViewPosition.y, layout.Field.center.y, "The guest also plays at the bottom of their screen");
        Assert.Greater(live.OpponentPaddleViewPosition.y, layout.Field.center.y);
        Assert.IsFalse(match.AwaitingMyServe, "The host serves first");
    }

    [UnityTest]
    public IEnumerator QuickMatch_EndsWithTheResult()
    {
        yield return Open(true, 2);
        match.HostStart();
        var start = link.Last(MatchMessageType.Start).Value;
        StartRemote(start.Seed, start.HostServes);
        yield return WaitForPlaying();

        yield return Drive(25f, Away, true, () => match.State == OnlineMatchController.MatchState.Result);
        Touch(TouchPhase.Ended, Vector2.zero);

        Assert.AreEqual(OnlineMatchController.MatchState.Result, match.State, "The match finished");
        Assert.AreEqual(2, match.MyScore);
        Assert.IsTrue(match.Won);
        Assert.IsFalse(live.Running);
        Assert.IsFalse(live.BallShown);

        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
        yield return null;
        Assert.AreEqual("You win!", root.Q("online-result").Q<Label>("title").text);
    }

    [UnityTest]
    public IEnumerator Leaving_RestoresTheSoloTable()
    {
        yield return Open(true);
        match.HostStart();
        var start = link.Last(MatchMessageType.Start).Value;
        StartRemote(start.Seed, start.HostServes);
        yield return WaitForPlaying();
        yield return Drive(1f, Follow, false);

        match.OpponentLeft();
        Assert.AreEqual(OnlineMatchController.MatchState.Result, match.State);
        Assert.IsTrue(match.Won, "The one who stays wins");

        match.Exit();
        yield return null;
        Assert.AreEqual(FieldTopology.Solo, layout.Topology);
        Assert.IsNull(live.Session);
        Assert.IsFalse(live.BallShown);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
    }
}
