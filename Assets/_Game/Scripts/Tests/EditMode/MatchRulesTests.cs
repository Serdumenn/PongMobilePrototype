using NUnit.Framework;

public sealed class MatchRulesTests
{
    private GameModeDefinition classic;
    private GameModeDefinition rush;
    private Participant player;
    private Participant[] players;

    [SetUp]
    public void SetUp()
    {
        classic = TestData.LoadMode(TestData.ClassicModePath);
        rush = TestData.LoadMode(TestData.RushModePath);
        player = new Participant(0, FieldSide.Bottom, null);
        players = new[] { player };
    }

    private MatchRules Begin(GameModeDefinition mode)
    {
        var rules = MatchRules.Create(mode);
        rules.Begin(players);
        return rules;
    }

    [Test]
    public void Create_PicksRulesByKind()
    {
        Assert.IsInstanceOf<ClassicRules>(MatchRules.Create(classic));
        Assert.IsInstanceOf<RushRules>(MatchRules.Create(rush));
        Assert.IsInstanceOf<ClassicRules>(MatchRules.Create(null));
    }

    [TestCase(0f)]
    [TestCase(0.1f)]
    [TestCase(-1f)]
    [TestCase(1f)]
    public void Classic_EveryHitIsOnePoint(float offset)
    {
        var rules = Begin(classic);

        var hit = rules.ScoreHit(player, offset);

        Assert.AreEqual(1, hit.Points);
        Assert.IsFalse(hit.Perfect);
        Assert.AreEqual(0, hit.PerfectStreak);
    }

    [Test]
    public void Classic_IsUntimedAndOneMissEndsTheRun()
    {
        var rules = Begin(classic);
        rules.OnLaunched();

        Assert.IsFalse(rules.IsTimed);
        Assert.IsFalse(rules.Tick(1000f));
        Assert.IsTrue(rules.OnMiss(player));
    }

    [Test]
    public void Rush_StartsWithFullClockThatWaitsForLaunch()
    {
        var rules = Begin(rush);

        Assert.IsTrue(rules.IsTimed);
        Assert.AreEqual(60f, rules.TimeLimit);
        Assert.AreEqual(60f, rules.TimeRemaining);

        Assert.IsFalse(rules.Tick(10f));
        Assert.AreEqual(60f, rules.TimeRemaining);

        rules.OnLaunched();
        Assert.IsFalse(rules.Tick(10f));
        Assert.AreEqual(50f, rules.TimeRemaining, 0.0001f);
    }

    [Test]
    public void Rush_EndsWhenClockRunsOut()
    {
        var rules = Begin(rush);
        rules.OnLaunched();

        Assert.IsFalse(rules.Tick(59.9f));
        Assert.IsTrue(rules.Tick(0.2f));
        Assert.AreEqual(0f, rules.TimeRemaining);
    }

    [TestCase(0f, true)]
    [TestCase(0.25f, true)]
    [TestCase(-0.25f, true)]
    [TestCase(0.2501f, false)]
    [TestCase(-0.5f, false)]
    [TestCase(1f, false)]
    public void Rush_PerfectZoneIsCentralQuarter(float offset, bool perfect)
    {
        var rules = Begin(rush);

        Assert.AreEqual(perfect, rules.ScoreHit(player, offset).Perfect);
    }

    [Test]
    public void Rush_PointsFollowPerfectStreak()
    {
        var rules = Begin(rush);

        var points = new[]
        {
            rules.ScoreHit(player, 0f).Points,
            rules.ScoreHit(player, 0f).Points,
            rules.ScoreHit(player, 0f).Points,
            rules.ScoreHit(player, 0f).Points,
            rules.ScoreHit(player, 0.9f).Points,
            rules.ScoreHit(player, 0f).Points
        };

        CollectionAssert.AreEqual(new[] { 2, 2, 3, 3, 1, 2 }, points);
    }

    [Test]
    public void Rush_ReportsPerfectStreak()
    {
        var rules = Begin(rush);

        Assert.AreEqual(1, rules.ScoreHit(player, 0f).PerfectStreak);
        Assert.AreEqual(2, rules.ScoreHit(player, 0.1f).PerfectStreak);
        Assert.AreEqual(0, rules.ScoreHit(player, 0.8f).PerfectStreak);
    }

    [Test]
    public void Rush_PerfectStreakIsTrackedPerParticipant()
    {
        var other = new Participant(1, FieldSide.Top, null);
        var rules = MatchRules.Create(rush);
        rules.Begin(new[] { player, other });

        rules.ScoreHit(player, 0f);
        rules.ScoreHit(player, 0f);
        var first = rules.ScoreHit(other, 0f);
        var third = rules.ScoreHit(player, 0f);

        Assert.AreEqual(1, first.PerfectStreak);
        Assert.AreEqual(2, first.Points);
        Assert.AreEqual(3, third.PerfectStreak);
        Assert.AreEqual(3, third.Points);
    }

    [Test]
    public void Rush_MissCostsFiveSecondsAndBreaksStreak()
    {
        var rules = Begin(rush);
        rules.OnLaunched();

        rules.ScoreHit(player, 0f);
        rules.ScoreHit(player, 0f);
        Assert.IsFalse(rules.OnMiss(player));
        Assert.AreEqual(55f, rules.TimeRemaining, 0.0001f);

        var hit = rules.ScoreHit(player, 0f);
        Assert.AreEqual(1, hit.PerfectStreak);
        Assert.AreEqual(2, hit.Points);
    }

    [Test]
    public void Rush_MissThatEmptiesClockEndsRun()
    {
        var rules = Begin(rush);
        rules.OnLaunched();
        rules.Tick(56f);

        Assert.IsTrue(rules.OnMiss(player));
        Assert.AreEqual(0f, rules.TimeRemaining);
    }

    [Test]
    public void Rush_BeginResetsState()
    {
        var rules = Begin(rush);
        rules.OnLaunched();
        rules.Tick(30f);
        rules.ScoreHit(player, 0f);
        rules.ScoreHit(player, 0f);

        rules.Begin(players);

        Assert.AreEqual(60f, rules.TimeRemaining);
        Assert.IsFalse(rules.Tick(5f));
        Assert.AreEqual(60f, rules.TimeRemaining);
        Assert.AreEqual(1, rules.ScoreHit(player, 0f).PerfectStreak);
    }
}
