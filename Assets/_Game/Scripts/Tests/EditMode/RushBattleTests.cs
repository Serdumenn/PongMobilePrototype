using System.Collections.Generic;
using NUnit.Framework;

public sealed class RushBattleTests
{
    private static RushBattleRules Rules(byte mySlot, params int[] scores)
    {
        var players = new List<RushBattlePlayer>();
        for (int i = 0; i < scores.Length; i++)
            players.Add(new RushBattlePlayer { Slot = (byte)i, Id = "id" + i, Name = "P" + i, Score = scores[i] });
        return new RushBattleRules(players, mySlot);
    }

    [Test]
    public void Roster_IsSortedUniqueAndRoundTrips()
    {
        string roster = RushBattleRules.BuildRoster(new[] { "zeta", "alpha", "", "mid", "alpha" });
        Assert.AreEqual("alpha,mid,zeta", roster);
        CollectionAssert.AreEqual(new[] { "alpha", "mid", "zeta" }, RushBattleRules.ParseRoster(roster));
        Assert.AreEqual(0, RushBattleRules.ParseRoster(null).Length);
    }

    [Test]
    public void EveryThirdPerfectInARowSendsAnAttack()
    {
        Assert.IsFalse(RushBattleRules.ShouldAttack(0));
        Assert.IsFalse(RushBattleRules.ShouldAttack(2));
        Assert.IsTrue(RushBattleRules.ShouldAttack(3));
        Assert.IsFalse(RushBattleRules.ShouldAttack(4));
        Assert.IsTrue(RushBattleRules.ShouldAttack(6));
    }

    [Test]
    public void AttackKindsCoverAllThree()
    {
        var seen = new HashSet<RushAttack>();
        var random = new MatchRandom(11);
        for (int i = 0; i < 60; i++) seen.Add(RushBattleRules.KindFor(random.NextUInt()));
        Assert.AreEqual(3, seen.Count);
    }

    [Test]
    public void Target_IsTheActiveLeaderButNeverMe()
    {
        var rules = Rules(0, 40, 31, 22, 31);
        Assert.AreEqual(1, rules.PickTarget().Slot, "Highest opponent; ties go to the lower slot");

        rules.SetFinal(1, 31);
        Assert.AreEqual(3, rules.PickTarget().Slot, "Finished players cannot be attacked");

        rules.MarkLeft(3);
        Assert.AreEqual(2, rules.PickTarget().Slot);

        rules.MarkLeft(2);
        Assert.IsNull(rules.PickTarget());
    }

    [Test]
    public void Scores_OnlyGoUpUntilFinal()
    {
        var rules = Rules(0, 0, 0);
        rules.SetScore(1, 12, 40);
        rules.SetScore(1, 9, 39);
        Assert.AreEqual(12, rules.Find(1).Score, "A late tick cannot lower the score");

        rules.SetFinal(1, 15);
        rules.SetScore(1, 30, 1);
        Assert.AreEqual(15, rules.Find(1).Score, "No ticks after the final score");
        Assert.IsTrue(rules.Find(1).Finished);
    }

    [Test]
    public void Ranking_PutsLeaversLastAndSharesPlaces()
    {
        var rules = Rules(2, 20, 35, 35, 50);
        rules.MarkLeft(3);

        var ranked = rules.Ranking();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 0, 3 }, ranked.ConvertAll(p => p.Slot));
        Assert.AreEqual(1, rules.PlaceOf(2), "A tie shares first place");
        Assert.AreEqual(1, rules.PlaceOf(1));
        Assert.AreEqual(3, rules.PlaceOf(0));
    }

    [Test]
    public void DoneWhenEveryoneFinishedOrLeft()
    {
        var rules = Rules(0, 0, 0, 0);
        Assert.IsFalse(rules.AllDone);
        rules.SetFinal(0, 10);
        rules.SetFinal(1, 12);
        Assert.IsFalse(rules.AllDone);
        rules.MarkLeft(2);
        Assert.IsTrue(rules.AllDone);
        Assert.IsFalse(rules.AloneLeft);
    }

    [Test]
    public void RematchNeedsEveryoneStillHere()
    {
        var rules = Rules(0, 0, 0, 0);
        rules.Find(0).WantsRematch = true;
        rules.Find(1).WantsRematch = true;
        Assert.IsFalse(rules.EveryoneWantsRematch);

        rules.MarkLeft(2);
        Assert.IsTrue(rules.EveryoneWantsRematch);

        rules.ClearRematch();
        Assert.IsFalse(rules.EveryoneWantsRematch);
    }

    [Test]
    public void RushMessages_RoundTrip()
    {
        var messages = new[]
        {
            MatchMessage.RushStart(77, "a,b,c"),
            MatchMessage.RushScore(2, 41, 33),
            MatchMessage.AttackOn(5, 1, 3, (byte)RushAttack.Fog),
            MatchMessage.ShieldFrom(6, 3, 1, (byte)RushAttack.Fog),
            MatchMessage.RushFinal(0, 58),
            MatchMessage.ReactionFrom(2, 1),
            MatchMessage.RematchFrom(3)
        };

        foreach (var sent in messages)
        {
            Assert.IsTrue(MatchMessage.TryParse(sent.ToBytes(), out var got), sent.Type.ToString());
            Assert.AreEqual(sent.Type, got.Type);
            Assert.AreEqual(sent.Seq, got.Seq);
            Assert.AreEqual(sent.Seed, got.Seed);
            Assert.AreEqual(sent.Roster ?? (sent.Type == MatchMessageType.RushStart ? string.Empty : null), got.Roster);
            Assert.AreEqual(sent.Slot, got.Slot);
            Assert.AreEqual(sent.Target, got.Target);
            Assert.AreEqual(sent.Kind, got.Kind);
            Assert.AreEqual(sent.Value, got.Value);
            Assert.AreEqual(sent.Reaction, got.Reaction);
        }
    }
}
