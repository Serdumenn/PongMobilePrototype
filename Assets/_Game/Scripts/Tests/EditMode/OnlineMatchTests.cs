using NUnit.Framework;

public sealed class OnlineMatchTests
{
    [Test]
    public void Messages_RoundTripEveryType()
    {
        var rules = new OnlineMatchRules(false, 5, 3, true);
        rules.Miss(false);
        rules.Miss(true);

        var messages = new[]
        {
            MatchMessage.Start(123456, true),
            MatchMessage.Handoff(42, -0.75f, 0.3f, 0.95f, 7.5f, 9, "ball_minty"),
            MatchMessage.Miss(7),
            MatchMessage.Score(rules),
            MatchMessage.ReactionOf(3),
            MatchMessage.RematchRequest()
        };

        foreach (var sent in messages)
        {
            Assert.IsTrue(MatchMessage.TryParse(sent.ToBytes(), out var got), sent.Type.ToString());
            Assert.AreEqual(sent.Type, got.Type);
            Assert.AreEqual(sent.Seq, got.Seq);
            Assert.AreEqual(sent.Seed, got.Seed);
            Assert.AreEqual(sent.HostServes, got.HostServes);
            Assert.AreEqual(sent.X, got.X);
            Assert.AreEqual(sent.DirX, got.DirX);
            Assert.AreEqual(sent.DirY, got.DirY);
            Assert.AreEqual(sent.Speed, got.Speed);
            Assert.AreEqual(sent.Rally, got.Rally);
            Assert.AreEqual(sent.Look ?? (sent.Type == MatchMessageType.Handoff ? string.Empty : null), got.Look);
            Assert.AreEqual(sent.HostScore, got.HostScore);
            Assert.AreEqual(sent.GuestScore, got.GuestScore);
            Assert.AreEqual(sent.Lives, got.Lives);
            Assert.AreEqual(sent.Ended, got.Ended);
            Assert.AreEqual(sent.Reaction, got.Reaction);
        }
    }

    [Test]
    public void Messages_RejectBrokenData()
    {
        Assert.IsFalse(MatchMessage.TryParse(null, out _));
        Assert.IsFalse(MatchMessage.TryParse(new byte[] { 1, 2 }, out _));
        Assert.IsFalse(MatchMessage.TryParse(new byte[] { 99, 1, 0, 0, 0, 0, 0, 0 }, out _));
        Assert.IsFalse(MatchMessage.TryParse(new byte[] { MatchMessage.Version, 200, 0, 0 }, out _));

        var handoff = MatchMessage.Handoff(1, 0f, 0f, 1f, 5f, 1, "ball_pingi").ToBytes();
        var cut = new byte[handoff.Length - 6];
        System.Array.Copy(handoff, cut, cut.Length);
        Assert.IsFalse(MatchMessage.TryParse(cut, out _));
    }

    [Test]
    public void Duel_FirstToFiveWinsAndLoserServes()
    {
        var rules = new OnlineMatchRules(false, 5, 3, true);

        rules.Miss(true);
        Assert.AreEqual(0, rules.HostScore);
        Assert.AreEqual(1, rules.GuestScore);
        Assert.IsTrue(rules.HostServes, "The player who lost the point serves");

        rules.Miss(false);
        Assert.AreEqual(1, rules.HostScore);
        Assert.IsFalse(rules.HostServes);

        for (int i = 0; i < 3; i++) rules.Miss(false);
        Assert.IsFalse(rules.Ended);
        rules.Miss(false);

        Assert.IsTrue(rules.Ended);
        Assert.IsTrue(rules.HostWon);
        Assert.AreEqual(5, rules.HostScore);

        rules.Miss(true);
        Assert.AreEqual(1, rules.GuestScore, "No scoring after the match ended");
    }

    [Test]
    public void Coop_SharedLivesAndPasses()
    {
        var rules = new OnlineMatchRules(true, 5, 3, false);

        for (int i = 0; i < 12; i++) rules.Pass();
        rules.Miss(true);
        rules.Miss(false);
        Assert.AreEqual(1, rules.Lives);
        Assert.IsFalse(rules.Ended);
        Assert.IsFalse(rules.HostServes, "The guest missed last, so the guest serves");

        rules.Miss(true);
        Assert.IsTrue(rules.Ended);
        Assert.AreEqual(0, rules.Lives);
        Assert.AreEqual(12, rules.Passes);
        Assert.AreEqual(0, rules.HostScore);
        Assert.AreEqual(0, rules.GuestScore);

        rules.Pass();
        Assert.AreEqual(12, rules.Passes);
    }

    [Test]
    public void Forfeit_TheOneWhoStaysWins()
    {
        var hostLeft = new OnlineMatchRules(false, 5, 3, true);
        hostLeft.Forfeit(true);
        Assert.IsTrue(hostLeft.Ended);
        Assert.IsFalse(hostLeft.HostWon);

        var guestLeft = new OnlineMatchRules(false, 5, 3, true);
        guestLeft.Forfeit(false);
        Assert.IsTrue(guestLeft.HostWon);
    }

    [Test]
    public void ScoreMessage_SyncsTheGuest()
    {
        var host = new OnlineMatchRules(false, 5, 3, true);
        host.Miss(true);
        host.Miss(true);
        host.Miss(false);

        var guest = new OnlineMatchRules(false, 5, 3, true);
        Assert.IsTrue(MatchMessage.TryParse(MatchMessage.Score(host).ToBytes(), out var message));
        guest.Apply(message);

        Assert.AreEqual(host.HostScore, guest.HostScore);
        Assert.AreEqual(host.GuestScore, guest.GuestScore);
        Assert.AreEqual(host.HostServes, guest.HostServes);
        Assert.AreEqual(2, guest.ScoreFor(false));
        Assert.AreEqual(1, guest.ScoreFor(true));
    }
}
