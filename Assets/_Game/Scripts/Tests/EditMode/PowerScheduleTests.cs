using NUnit.Framework;
using UnityEngine;

public sealed class PowerScheduleTests
{
    [Test]
    public void SameSeed_GivesTheSameRun()
    {
        var a = new PowerSchedule(1234, 8f, 10f, 14f);
        var b = new PowerSchedule(1234, 8f, 10f, 14f);
        var area = new Rect(-1.5f, 0f, 3f, 2f);

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(a.NextAt, b.NextAt);
            Assert.AreEqual(a.TakeKind(), b.TakeKind());
            Assert.AreEqual(a.TakeSpot(area), b.TakeSpot(area));
            a.Advance();
            b.Advance();
        }
    }

    [Test]
    public void Tokens_ComeOnTimeInsideTheAreaWithoutRepeats()
    {
        var schedule = new PowerSchedule(77, 8f, 10f, 14f);
        var area = new Rect(-1.5f, 0f, 3f, 2f);
        Assert.AreEqual(8f, schedule.NextAt, 0.0001f);

        PowerKind? last = null;
        var seen = new bool[PowerSchedule.KindCount];
        for (int i = 0; i < 200; i++)
        {
            float before = schedule.NextAt;
            var kind = schedule.TakeKind();
            Assert.AreNotEqual(last, kind, "The same power twice in a row");
            last = kind;
            seen[(int)kind] = true;

            var spot = schedule.TakeSpot(area);
            Assert.IsTrue(area.Contains(spot), spot.ToString());

            schedule.Advance();
            float gap = schedule.NextAt - before;
            Assert.GreaterOrEqual(gap, 10f - 0.0001f);
            Assert.LessOrEqual(gap, 14f + 0.0001f);
        }

        foreach (bool kind in seen) Assert.IsTrue(kind);
    }
}
