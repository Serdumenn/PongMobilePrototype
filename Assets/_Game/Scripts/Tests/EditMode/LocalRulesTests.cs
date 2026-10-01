using System;
using NUnit.Framework;

public sealed class LocalRulesTests
{
    private const string DuelPath = "Assets/_Game/Data/Modes/mode_table_duel.asset";
    private const string CoopPath = "Assets/_Game/Data/Modes/mode_coop_rally.asset";
    private const string PartyPath = "Assets/_Game/Data/Modes/mode_party_table.asset";

    private static Participant[] Players(params FieldSide[] sides)
    {
        var players = new Participant[sides.Length];
        for (int i = 0; i < sides.Length; i++) players[i] = new Participant(i, sides[i], null);
        return players;
    }

    [Test]
    public void ModeAssets_KeepTheirTunedValues()
    {
        var duel = TestData.LoadMode(DuelPath);
        var coop = TestData.LoadMode(CoopPath);
        var party = TestData.LoadMode(PartyPath);

        Assert.AreEqual(GameModeKind.TableDuel, duel.Kind);
        Assert.AreEqual(FieldTopology.TopBottom, duel.Topology);
        Assert.AreEqual(2, duel.MinPlayers);
        Assert.AreEqual(2, duel.MaxPlayers);
        Assert.AreEqual(5, duel.PointsToWin);
        Assert.IsFalse(duel.TabletOnly);

        Assert.AreEqual(GameModeKind.CoopRally, coop.Kind);
        Assert.AreEqual(FieldTopology.TopBottom, coop.Topology);
        Assert.AreEqual(3, coop.Lives);
        Assert.AreEqual(0.06f, coop.PerfectSlowdown, 0.0001f);

        Assert.AreEqual(GameModeKind.PartyTable, party.Kind);
        Assert.AreEqual(FieldTopology.FourSides, party.Topology);
        Assert.AreEqual(3, party.MinPlayers);
        Assert.AreEqual(4, party.MaxPlayers);
        Assert.IsTrue(party.TabletOnly);
        Assert.AreEqual(3, party.Lives);
        Assert.AreEqual(15, party.ExtraBallAtHits);

        Assert.IsFalse(TestData.LoadMode(TestData.ClassicModePath).IsMultiplayer);
        Assert.IsTrue(duel.IsMultiplayer);
    }

    [Test]
    public void Create_PicksLocalRules()
    {
        Assert.IsInstanceOf<TableDuelRules>(MatchRules.Create(TestData.LoadMode(DuelPath)));
        Assert.IsInstanceOf<CoopRallyRules>(MatchRules.Create(TestData.LoadMode(CoopPath)));
        Assert.IsInstanceOf<PartyRules>(MatchRules.Create(TestData.LoadMode(PartyPath)));
    }

    [Test]
    public void Duel_MissGivesOpponentAPoint()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top);
        var rules = MatchRules.Create(TestData.LoadMode(DuelPath));
        rules.Begin(players);

        Assert.AreEqual(0, rules.ScoreHit(players[0], 0f).Points);
        Assert.IsFalse(rules.OnMiss(players[0]));

        Assert.AreEqual(0, players[0].Score);
        Assert.AreEqual(1, players[1].Score);
        Assert.IsNull(rules.Winner);
    }

    [Test]
    public void Duel_FirstToFiveWins()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top);
        var rules = MatchRules.Create(TestData.LoadMode(DuelPath));
        rules.Begin(players);

        for (int i = 0; i < 4; i++) Assert.IsFalse(rules.OnMiss(players[1]));
        for (int i = 0; i < 3; i++) Assert.IsFalse(rules.OnMiss(players[0]));
        Assert.IsTrue(rules.OnMiss(players[1]));

        Assert.AreSame(players[0], rules.Winner);
        Assert.AreEqual(5, players[0].Score);
        Assert.AreEqual(3, players[1].Score);
        Assert.IsTrue(rules.OnMiss(players[0]));
        Assert.AreEqual(3, players[1].Score);
    }

    [Test]
    public void Duel_BeginResetsScores()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top);
        var rules = MatchRules.Create(TestData.LoadMode(DuelPath));
        rules.Begin(players);
        for (int i = 0; i < 5; i++) rules.OnMiss(players[1]);

        rules.Begin(players);

        Assert.AreEqual(0, players[0].Score);
        Assert.IsNull(rules.Winner);
    }

    [Test]
    public void Coop_CountsPassesAndSharesLives()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top);
        var rules = MatchRules.Create(TestData.LoadMode(CoopPath));
        rules.Begin(players);

        Assert.IsTrue(rules.IsTeamMatch);
        Assert.AreEqual(3, rules.TeamLives);

        rules.ScoreHit(players[0], 0.9f);
        var perfect = rules.ScoreHit(players[1], 0.1f);
        Assert.AreEqual(2, rules.TeamScore);
        Assert.IsTrue(perfect.Perfect);

        Assert.IsFalse(rules.OnMiss(players[0]));
        Assert.IsFalse(rules.OnMiss(players[1]));
        Assert.AreEqual(1, rules.TeamLives);
        Assert.IsTrue(rules.OnMiss(players[0]));
        Assert.AreEqual(0, rules.TeamLives);
        Assert.AreEqual(2, rules.TeamScore);
    }

    [Test]
    public void Party_LivesEliminateUntilOneIsLeft()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top, FieldSide.Left);
        var rules = MatchRules.Create(TestData.LoadMode(PartyPath));
        rules.Begin(players);

        foreach (var p in players) Assert.AreEqual(3, p.Lives);

        Assert.IsFalse(rules.OnMiss(players[2]));
        Assert.IsFalse(rules.OnMiss(players[2]));
        Assert.IsFalse(rules.OnMiss(players[2]));
        Assert.IsTrue(players[2].Eliminated);
        Assert.IsFalse(rules.OnMiss(players[2]));
        Assert.AreEqual(0, players[2].Lives);

        Assert.IsFalse(rules.OnMiss(players[1]));
        Assert.IsFalse(rules.OnMiss(players[1]));
        Assert.IsTrue(rules.OnMiss(players[1]));

        Assert.AreSame(players[0], rules.Winner);
        Assert.AreEqual(3, players[0].Lives);
    }

    [Test]
    public void Party_SecondBallArrivesAfterFifteenHits()
    {
        var players = Players(FieldSide.Bottom, FieldSide.Top, FieldSide.Left, FieldSide.Right);
        var rules = MatchRules.Create(TestData.LoadMode(PartyPath));
        rules.Begin(players);

        for (int i = 0; i < 14; i++) rules.ScoreHit(players[i % 4], 0f);
        Assert.AreEqual(1, rules.BallsInPlay);

        rules.ScoreHit(players[0], 0f);
        Assert.AreEqual(2, rules.BallsInPlay);
        Assert.AreEqual(15, rules.TeamScore);
    }

    [Test]
    public void Records_MatchesCountTowardsStreakAndBestRally()
    {
        string path = TestData.TempFile("records_match_test.json");
        try
        {
            var records = PlayerRecords.Load(path);
            var day = new DateTime(2026, 10, 1, 12, 0, 0);

            Assert.IsTrue(records.RecordMatch(12, 90f, day));
            Assert.IsFalse(records.RecordMatch(8, 30f, day.AddDays(1)));
            Assert.IsTrue(records.RecordMatch(20, 60f, day.AddDays(1)));

            Assert.AreEqual(3, records.MatchesPlayed);
            Assert.AreEqual(20, records.BestCoopRally);
            Assert.AreEqual(180f, records.PlayTimeSeconds, 0.001f);
            Assert.AreEqual(0, records.GamesPlayed);
            Assert.AreEqual(2, records.CurrentDayStreak(day.AddDays(1)));

            Assert.IsTrue(records.Save());
            var loaded = PlayerRecords.Load(path);
            Assert.AreEqual(3, loaded.MatchesPlayed);
            Assert.AreEqual(20, loaded.BestCoopRally);
        }
        finally
        {
            TestData.Delete(path);
        }
    }
}
