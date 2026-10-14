using System.Collections.Generic;
using NUnit.Framework;

public sealed class RunMarksTests
{
    private static readonly int[] Marks = { 10, 25, 50, 100 };

    [Test]
    public void Milestones_FireOnTheMarksAndEveryFiftyAfter()
    {
        foreach (int streak in new[] { 10, 25, 50, 100, 150, 200, 450 })
            Assert.IsTrue(RunMarks.IsMilestone(streak, Marks, 50), streak.ToString());

        foreach (int streak in new[] { -1, 0, 1, 9, 11, 24, 49, 99, 101, 125, 149 })
            Assert.IsFalse(RunMarks.IsMilestone(streak, Marks, 50), streak.ToString());
    }

    [Test]
    public void Milestones_WithoutMarksOrStepStayQuiet()
    {
        Assert.IsFalse(RunMarks.IsMilestone(10, null, 50));
        Assert.IsFalse(RunMarks.IsMilestone(10, new int[0], 50));
        Assert.IsFalse(RunMarks.IsMilestone(150, Marks, 0));
        Assert.IsTrue(RunMarks.IsMilestone(100, Marks, 0));
    }

    [Test]
    public void BeatsBest_OnlyWhenARealRecordFalls()
    {
        Assert.IsTrue(RunMarks.BeatsBest(6, 5, true));
        Assert.IsFalse(RunMarks.BeatsBest(5, 5, true));
        Assert.IsFalse(RunMarks.BeatsBest(4, 5, true));
        Assert.IsFalse(RunMarks.BeatsBest(6, 0, true), "The first run ever has nothing to beat");
        Assert.IsFalse(RunMarks.BeatsBest(6, 5, false), "Unranked runs never celebrate a best");
    }

    [Test]
    public void NearMiss_NeedsTheEdgeAndAWarmRally()
    {
        Assert.IsTrue(RunMarks.NearMiss(0.9f, 0.88f, 3, 3));
        Assert.IsTrue(RunMarks.NearMiss(-1f, 0.88f, 8, 3));
        Assert.IsFalse(RunMarks.NearMiss(0.5f, 0.88f, 8, 3));
        Assert.IsFalse(RunMarks.NearMiss(0.95f, 0.88f, 2, 3));
    }

    [Test]
    public void MissWouldEnd_MatchesWhatTheMissDoes()
    {
        var player = new Participant(0, FieldSide.Bottom, null);
        var players = new List<Participant> { player };

        var classic = MatchRules.Create(TestData.LoadMode(TestData.ClassicModePath));
        classic.Begin(players);
        Assert.IsTrue(classic.MissWouldEnd(player));

        var rushMode = TestData.LoadMode(TestData.RushModePath);
        var rush = MatchRules.Create(rushMode);
        rush.Begin(players);
        rush.OnLaunched();
        Assert.IsFalse(rush.MissWouldEnd(player));

        rush.Tick(rushMode.DurationSeconds - rushMode.MissPenaltySeconds + 0.1f);
        Assert.IsTrue(rush.MissWouldEnd(player));
        Assert.IsTrue(rush.OnMiss(player), "The prediction and the miss disagree");
    }
}
