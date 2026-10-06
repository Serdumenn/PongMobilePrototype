using NUnit.Framework;
using UnityEngine;

public sealed class FriendTagTests
{
    [Test]
    public void Parse_AcceptsTypedAndPastedCodes()
    {
        Assert.AreEqual("BraveOtter#4821", FriendTag.Parse("BraveOtter#4821"));
        Assert.AreEqual("BraveOtter#4821", FriendTag.Parse("braveotter#4821"), "Case does not matter");
        Assert.AreEqual("BraveOtter#4821", FriendTag.Parse("Brave Otter # 4821"), "Spaces do not matter");
        Assert.AreEqual("WittyPenguin#35049", FriendTag.Parse("Add me as a friend in Pingi Pongi! My code is WittyPenguin#35049"));
        Assert.AreEqual("BraveOtter#4821", FriendTag.Parse("Pingi Pongi BraveOtter#4821"), "Words before the code are not part of it");
        Assert.AreEqual("Someone#123456", FriendTag.Parse("Someone#123456"), "Names we did not generate are kept as typed");
    }

    [Test]
    public void Parse_RejectsWhatIsNotACode()
    {
        Assert.IsNull(FriendTag.Parse(null));
        Assert.IsNull(FriendTag.Parse(""));
        Assert.IsNull(FriendTag.Parse("BraveOtter"));
        Assert.IsNull(FriendTag.Parse("#4821"));
        Assert.IsNull(FriendTag.Parse("BraveOtter#12"), "Too few digits");
        Assert.IsNull(FriendTag.Parse("BraveOtter#123456789"), "Too many digits");
    }

    [Test]
    public void SharedMessage_ParsesInEveryLanguage()
    {
        const string tag = "WittyPenguin#35049";
        foreach (string code in Loc.Codes)
        {
            string pattern = Loc.Lookup(code, "Add me as a friend in Pingi Pongi! My code is {0}");
            Assert.IsNotNull(pattern, code);
            Assert.AreEqual(tag, FriendTag.Parse(string.Format(pattern, tag)), code);
        }
    }

    [Test]
    public void BaseName_IsTheShownNameWithoutSpaces()
    {
        const string id = "EB18mM7JILRj4dYptzOY4AMbFYfI";
        string shown = PlayerNames.ForId(id);
        Assert.AreEqual(shown.Replace(" ", ""), FriendTag.BaseName(id));
        Assert.IsTrue(FriendTag.HasBase(FriendTag.BaseName(id) + "#35049", FriendTag.BaseName(id)));
        Assert.IsFalse(FriendTag.HasBase("Other#35049", FriendTag.BaseName(id)));
        Assert.IsFalse(FriendTag.HasBase(null, FriendTag.BaseName(id)));
        Assert.AreEqual(FriendTag.BaseName(id), FriendTag.Canonical(FriendTag.BaseName(id).ToLowerInvariant()));
    }

    [Test]
    public void Invite_KeepsOnlyAValidCodeAndMode()
    {
        var invite = FriendInvite.For(" k7m-3qx ", "portal_duel");
        Assert.AreEqual("K7M3QX", invite.code);
        Assert.IsTrue(invite.IsValid);

        var copy = JsonUtility.FromJson<FriendInvite>(JsonUtility.ToJson(invite));
        Assert.IsTrue(copy.IsValid, "The invite survives a round trip");
        Assert.AreEqual(invite.code, copy.code);
        Assert.AreEqual(invite.mode, copy.mode);

        Assert.IsFalse(FriendInvite.For("ABC", "portal_duel").IsValid, "Short code");
        Assert.IsFalse(FriendInvite.For("K7M3QX", "").IsValid, "No mode");
        Assert.IsFalse(new FriendInvite { kind = "chat", v = FriendInvite.Version, code = "K7M3QX", mode = "portal_duel" }.IsValid, "Only invites are accepted");
        Assert.IsFalse(new FriendInvite { kind = FriendInvite.InviteKind, v = 99, code = "K7M3QX", mode = "portal_duel" }.IsValid, "Unknown version");
    }
}
