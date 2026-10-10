using System;
using NUnit.Framework;

public sealed class ReviewGateTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Asks_AfterNewBest_WhenPlayerIsEngaged()
    {
        Assert.IsTrue(ReviewGate.ShouldAsk(true, ReviewGate.MinDays, ReviewGate.MinGames, default, Now));
    }

    [Test]
    public void DoesNotAsk_WithoutNewBest()
    {
        Assert.IsFalse(ReviewGate.ShouldAsk(false, 30, 200, default, Now));
    }

    [Test]
    public void DoesNotAsk_BeforeEnoughDays()
    {
        Assert.IsFalse(ReviewGate.ShouldAsk(true, ReviewGate.MinDays - 1, 200, default, Now));
    }

    [Test]
    public void DoesNotAsk_BeforeEnoughGames()
    {
        Assert.IsFalse(ReviewGate.ShouldAsk(true, 30, ReviewGate.MinGames - 1, default, Now));
    }

    [Test]
    public void DoesNotAsk_DuringCooldown()
    {
        var asked = Now.AddDays(-(ReviewGate.CooldownDays - 1));
        Assert.IsFalse(ReviewGate.ShouldAsk(true, 30, 200, asked, Now));
    }

    [Test]
    public void AsksAgain_AfterCooldown()
    {
        var asked = Now.AddDays(-ReviewGate.CooldownDays);
        Assert.IsTrue(ReviewGate.ShouldAsk(true, 30, 200, asked, Now));
    }

    [Test]
    public void DoesNotAsk_WhenClockWentBackwards()
    {
        Assert.IsFalse(ReviewGate.ShouldAsk(true, 30, 200, Now.AddDays(5), Now));
    }

    [Test]
    public void CountDay_AddsOnlyNewDays()
    {
        Assert.AreEqual(1, ReviewGate.CountDay(0, string.Empty, "2026-10-10"));
        Assert.AreEqual(1, ReviewGate.CountDay(1, "2026-10-10", "2026-10-10"));
        Assert.AreEqual(2, ReviewGate.CountDay(1, "2026-10-10", "2026-10-11"));
    }
}
