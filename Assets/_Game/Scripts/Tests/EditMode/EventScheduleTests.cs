using NUnit.Framework;

public sealed class EventScheduleTests
{
    [Test]
    public void SameSeed_GivesTheSameEvents()
    {
        var a = new EventSchedule(42, 20f, 30f, 40f);
        var b = new EventSchedule(42, 20f, 30f, 40f);

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(a.NextAt, b.NextAt);
            Assert.AreEqual(a.TakeKind(), b.TakeKind());
            a.Advance(a.NextAt + 10f);
            b.Advance(b.NextAt + 10f);
        }
    }

    [Test]
    public void Events_ComeAfterTheGapWithoutRepeats()
    {
        var schedule = new EventSchedule(9, 20f, 30f, 40f);
        Assert.AreEqual(20f, schedule.NextAt, 0.0001f);

        EventKind? last = null;
        var seen = new bool[EventSchedule.KindCount];
        float now = 20f;
        for (int i = 0; i < 200; i++)
        {
            var kind = schedule.TakeKind();
            Assert.AreNotEqual(last, kind);
            last = kind;
            seen[(int)kind] = true;

            now += 10f;
            schedule.Advance(now);
            float gap = schedule.NextAt - now;
            Assert.GreaterOrEqual(gap, 30f - 0.0001f);
            Assert.LessOrEqual(gap, 40f + 0.0001f);
            now = schedule.NextAt;
        }

        foreach (bool kind in seen) Assert.IsTrue(kind);
    }

    [Test]
    public void Success_MeansReachingTheGoal()
    {
        Assert.IsTrue(EventSchedule.Succeeded(EventKind.Storm, 3, 3));
        Assert.IsFalse(EventSchedule.Succeeded(EventKind.Storm, 2, 3));
        Assert.IsTrue(EventSchedule.Succeeded(EventKind.GoldRain, 6, 6));
        Assert.IsFalse(EventSchedule.Succeeded(EventKind.Targets, 4, 5));
        Assert.IsFalse(EventSchedule.Succeeded(EventKind.Giant, 0, 0));
    }
}
