using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class MatchCoreTests
{
    [Test]
    public void MatchRandom_SameSeedGivesSameSequence()
    {
        var a = new MatchRandom(1234);
        var b = new MatchRandom(1234);

        for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
    }

    [Test]
    public void MatchRandom_DifferentSeedsDiverge()
    {
        var a = new MatchRandom(1);
        var b = new MatchRandom(2);
        int same = 0;

        for (int i = 0; i < 100; i++) if (a.NextUInt() == b.NextUInt()) same++;

        Assert.Less(same, 3);
    }

    [Test]
    public void MatchRandom_KnownSequenceIsStable()
    {
        var random = new MatchRandom(20261001);
        var first = new[] { random.NextUInt(), random.NextUInt(), random.NextUInt() };
        var again = new MatchRandom(20261001);

        CollectionAssert.AreEqual(first, new[] { again.NextUInt(), again.NextUInt(), again.NextUInt() });
        Assert.AreEqual(20261001, random.Seed);
    }

    [Test]
    public void MatchRandom_ValuesStayInRange()
    {
        var random = new MatchRandom(99);
        bool sawNegative = false;
        bool sawPositive = false;

        for (int i = 0; i < 10000; i++)
        {
            float value = random.Value();
            Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));

            float ranged = random.Range(0.6f, 1f);
            Assert.That(ranged, Is.GreaterThanOrEqualTo(0.6f).And.LessThanOrEqualTo(1f));

            float sign = random.Sign();
            Assert.That(sign == 1f || sign == -1f);
            sawNegative |= sign < 0f;
            sawPositive |= sign > 0f;
        }

        Assert.IsTrue(sawNegative && sawPositive);
    }

    [Test]
    public void MatchRandom_SignIsBalanced()
    {
        var random = new MatchRandom(7);
        int positive = 0;

        for (int i = 0; i < 10000; i++) if (random.Sign() > 0f) positive++;

        Assert.That(positive, Is.InRange(4800, 5200));
    }

    [Test]
    public void Participant_TracksStreaks()
    {
        var participant = new Participant(0, FieldSide.Bottom, null);

        participant.RegisterHit();
        participant.RegisterHit();
        participant.RegisterHit();
        participant.BreakStreak();
        participant.RegisterHit();

        Assert.AreEqual(1, participant.HitStreak);
        Assert.AreEqual(3, participant.LongestStreak);

        participant.PerfectStreak = 4;
        participant.BeginRun();

        Assert.AreEqual(0, participant.HitStreak);
        Assert.AreEqual(0, participant.LongestStreak);
        Assert.AreEqual(0, participant.PerfectStreak);
    }

    [TestCase(FieldSide.Bottom, 0f, 1f)]
    [TestCase(FieldSide.Top, 0f, -1f)]
    [TestCase(FieldSide.Left, 1f, 0f)]
    [TestCase(FieldSide.Right, -1f, 0f)]
    public void FieldSide_InwardPointsIntoField(FieldSide side, float x, float y)
    {
        Assert.AreEqual(new Vector2(x, y), side.Inward());
    }

    [Test]
    public void FieldSide_LaunchFrameAlwaysPointsInward()
    {
        var serve = new Vector2(0.6f, 0.8f);

        foreach (FieldSide side in System.Enum.GetValues(typeof(FieldSide)))
            Assert.Greater(Vector2.Dot(side.FromBottomFrame(serve), side.Inward()), 0f, side.ToString());

        Assert.AreEqual(serve, FieldSide.Bottom.FromBottomFrame(serve));
    }

    private static readonly Vector2 ScreenSize = new Vector2(1080f, 2400f);

    private static IEnumerable<TestCaseData> ZoneCases()
    {
        yield return new TestCaseData(FieldSide.Bottom, new Vector2(540f, 100f), true);
        yield return new TestCaseData(FieldSide.Bottom, new Vector2(540f, 959f), true);
        yield return new TestCaseData(FieldSide.Bottom, new Vector2(540f, 962f), false);
        yield return new TestCaseData(FieldSide.Bottom, new Vector2(540f, 2300f), false);
        yield return new TestCaseData(FieldSide.Top, new Vector2(540f, 2300f), true);
        yield return new TestCaseData(FieldSide.Top, new Vector2(540f, 1438f), false);
        yield return new TestCaseData(FieldSide.Top, new Vector2(540f, 100f), false);
        yield return new TestCaseData(FieldSide.Left, new Vector2(100f, 1200f), true);
        yield return new TestCaseData(FieldSide.Left, new Vector2(900f, 1200f), false);
        yield return new TestCaseData(FieldSide.Right, new Vector2(900f, 1200f), true);
        yield return new TestCaseData(FieldSide.Right, new Vector2(100f, 1200f), false);
    }

    [TestCaseSource(nameof(ZoneCases))]
    public void TouchZone_UsesTheEdgeOfItsSide(FieldSide side, Vector2 position, bool inside)
    {
        Assert.AreEqual(inside, TouchZoneController.InZone(side, 0.4f, ScreenSize, position));
    }
}
