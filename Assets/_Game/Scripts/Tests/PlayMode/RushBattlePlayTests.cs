using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class RushBattlePlayTests : GameSceneFixture
{
    private OnlineRushController rush;
    private FakeMatchLink link;
    private Paddle paddle;

    private IEnumerator Open(bool host, string myId, params string[] others)
    {
        yield return LoadGame();
        rush = Object.FindFirstObjectByType<OnlineRushController>();
        paddle = Object.FindFirstObjectByType<Paddle>();
        Assert.IsNotNull(rush, "OnlineRushController missing in scene");

        var lobby = Object.FindFirstObjectByType<OnlineLobby>();
        var entrants = new List<OnlineRushController.Entrant> { new OnlineRushController.Entrant(myId, "Me", "ball_pingi") };
        foreach (var id in others) entrants.Add(new OnlineRushController.Entrant(id, "Rival " + id, "ball_minty"));

        link = new FakeMatchLink(host);
        rush.Open(link, lobby.FindMode("rush_battle"), myId, "ball_pingi", entrants);
        Assert.AreEqual(OnlineRushController.RushState.Waiting, rush.State);

        Set("WarningSeconds", 0.4f);
        Set("AttackSeconds", 0.5f);
    }

    private void Set(string field, float value)
    {
        typeof(OnlineRushController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(rush, value);
    }

    private void Hit(bool perfect, int streak)
    {
        typeof(OnlineRushController).GetMethod("OnHit", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(rush, new object[] { new HitResult(perfect ? 2 : 1, perfect, streak) });
    }

    private IEnumerator WaitUntil(Func<bool> condition, float seconds, string because)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), because);
    }

    private IEnumerator StartAsGuest()
    {
        link.Deliver(MatchMessage.RushStart(9, "id-a,id-b"));
        Assert.AreEqual(OnlineRushController.RushState.Countdown, rush.State);
        yield return WaitUntil(() => rush.State == OnlineRushController.RushState.Playing, 5f, "The countdown ends");
        Assert.IsTrue(Game.BattleRun);
    }

    [UnityTest]
    public IEnumerator Guest_PlaysAnUnrankedRunAndReportsScores()
    {
        yield return Open(false, "id-b", "id-a");
        yield return StartAsGuest();

        Assert.AreEqual(1, rush.Rules.MySlot, "Slots follow the sorted roster");
        Assert.AreEqual(SoloGameManager.GameState.Playing, Game.State);
        Assert.IsTrue(Game.ScoreManager.Unranked, "Battle runs never touch the solo best");
        yield return WaitUntil(() => link.Count(MatchMessageType.RushScore) >= 2, 2f, "Scores are sent a few times a second");
        Assert.AreEqual(1, link.Last(MatchMessageType.RushScore).Value.Slot);

        link.Deliver(MatchMessage.RushScore(0, 17, 40));
        Assert.AreEqual(17, rush.Rules.Find(0).Score);
    }

    [UnityTest]
    public IEnumerator Attack_WarnsThenShrinksThePaddleForAWhile()
    {
        yield return Open(false, "id-b", "id-a");
        yield return StartAsGuest();

        link.Deliver(MatchMessage.AttackOn(0, 0, 1, (byte)RushAttack.MiniPaddle));
        Assert.AreEqual(RushAttack.MiniPaddle, rush.Incoming);
        Assert.AreEqual(1f, paddle.LengthScale, "Nothing happens during the warning");

        yield return WaitUntil(() => paddle.LengthScale < 0.99f, 1.5f, "The paddle shrinks after the warning");
        Assert.AreEqual(0.6f, paddle.LengthScale, 0.001f);
        yield return WaitUntil(() => paddle.LengthScale > 0.99f, 2f, "The paddle grows back");

        link.Deliver(MatchMessage.AttackOn(1, 0, 1, (byte)RushAttack.FastBall));
        yield return WaitUntil(() => Game.Ball.SpeedBoost > 1.01f, 1.5f, "Fast ball speeds the ball up");
        yield return WaitUntil(() => Mathf.Approximately(Game.Ball.SpeedBoost, 1f), 2f, "And wears off");
    }

    [UnityTest]
    public IEnumerator Attack_PerfectDuringTheWarningBlocksIt()
    {
        yield return Open(false, "id-b", "id-a");
        yield return StartAsGuest();

        link.Deliver(MatchMessage.AttackOn(0, 0, 1, (byte)RushAttack.MiniPaddle));
        Hit(true, 1);

        var shield = link.Last(MatchMessageType.Shield);
        Assert.IsTrue(shield.HasValue, "Blocking tells the attacker");
        Assert.AreEqual(0, shield.Value.Target);
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(1f, paddle.LengthScale, "A blocked attack never lands");
    }

    [UnityTest]
    public IEnumerator ThreePerfectsAttackTheLeader()
    {
        yield return Open(true, "id-a", "id-b", "id-c");
        rush.HostStart();
        var start = link.Last(MatchMessageType.RushStart);
        Assert.IsTrue(start.HasValue);
        Assert.AreEqual("id-a,id-b,id-c", start.Value.Roster);
        yield return WaitUntil(() => rush.State == OnlineRushController.RushState.Playing, 5f, "The countdown ends");

        link.Deliver(MatchMessage.RushScore(1, 12, 50));
        link.Deliver(MatchMessage.RushScore(2, 30, 50));
        Assert.AreEqual(2, link.Count(MatchMessageType.RushScore) - CountMine(), "The host relays client scores");

        Hit(true, 2);
        Assert.IsFalse(link.Last(MatchMessageType.Attack).HasValue);
        Hit(true, 3);
        var attack = link.Last(MatchMessageType.Attack);
        Assert.IsTrue(attack.HasValue, "Three Perfects in a row send an attack");
        Assert.AreEqual(2, attack.Value.Target, "It goes to the leader");
        Assert.AreEqual(0, attack.Value.Slot);
    }

    private int CountMine()
    {
        int mine = 0;
        foreach (var message in link.Sent)
            if (message.Type == MatchMessageType.RushScore && message.Slot == rush.Rules.MySlot) mine++;
        return mine;
    }

    [UnityTest]
    public IEnumerator Battle_EndsWithARankingOnceEveryoneIsDone()
    {
        yield return Open(false, "id-b", "id-a");
        yield return StartAsGuest();

        Game.ScoreManager.AddPoints(9);
        Game.GameOver();
        Assert.AreEqual(OnlineRushController.RushState.Finished, rush.State);
        var final = link.Last(MatchMessageType.RushFinal);
        Assert.IsTrue(final.HasValue);
        Assert.AreEqual(9, final.Value.Value);

        link.Deliver(MatchMessage.RushFinal(0, 14));
        Assert.AreEqual(OnlineRushController.RushState.Result, rush.State);
        Assert.AreEqual(2, rush.Rules.PlaceOf(rush.Rules.MySlot));

        link.Deliver(MatchMessage.RematchFrom(0));
        rush.RequestRematch();
        Assert.IsTrue(rush.WantsRematch);
        link.Deliver(MatchMessage.RushStart(10, "id-a,id-b"));
        Assert.AreEqual(OnlineRushController.RushState.Countdown, rush.State, "The host's new start begins the rematch");
        Assert.AreEqual(0, rush.Rules.Find(0).Score);
    }

    [UnityTest]
    public IEnumerator Battle_WhenEveryoneElseLeavesYouWin()
    {
        yield return Open(true, "id-a", "id-b");
        rush.HostStart();
        yield return WaitUntil(() => rush.State == OnlineRushController.RushState.Playing, 5f, "The countdown ends");

        rush.UpdateEntrants(new[] { new OnlineRushController.Entrant("id-a", "Me", "ball_pingi") });
        Assert.AreEqual(OnlineRushController.RushState.Result, rush.State);
        Assert.AreEqual(1, rush.Rules.PlaceOf(rush.Rules.MySlot));
        Assert.IsTrue(rush.Rules.Find(1).Left);

        rush.Exit();
        Assert.IsTrue(link.Closed);
        Assert.IsFalse(Game.BattleRun);
        Assert.IsFalse(Game.ScoreManager.Unranked);
        Assert.AreEqual(SoloGameManager.GameState.Menu, Game.State);
        Assert.AreEqual(1f, paddle.LengthScale);
    }
}
