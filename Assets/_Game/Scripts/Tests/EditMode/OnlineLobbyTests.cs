using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Services.Multiplayer;
using UnityEditor;

public sealed class OnlineLobbyTests
{
    [Test]
    public void SessionCode_NormalizesTypedInput()
    {
        Assert.AreEqual("K7M3QX", SessionCode.Normalize(" k7m 3qx "));
        Assert.AreEqual("K7M3QX", SessionCode.Normalize("K7M-3QX"));
        Assert.AreEqual(string.Empty, SessionCode.Normalize(null));
    }

    [Test]
    public void SessionCode_AcceptsOnlySixLettersOrDigits()
    {
        Assert.IsTrue(SessionCode.IsValid("K7M3QX"));
        Assert.IsFalse(SessionCode.IsValid("K7M3Q"));
        Assert.IsFalse(SessionCode.IsValid("K7M3QXZ"));
        Assert.IsFalse(SessionCode.IsValid("k7m3qx"));
        Assert.IsFalse(SessionCode.IsValid("K7M3Q!"));
        Assert.IsFalse(SessionCode.IsValid("K7M3QÇ"));
        Assert.IsFalse(SessionCode.IsValid(null));
    }

    [Test]
    public void AllReady_NeedsAFullRoomOfReadyPlayers()
    {
        var one = new List<LobbyPlayer> { Player("a", true) };
        var twoWaiting = new List<LobbyPlayer> { Player("a", true), Player("b", false) };
        var twoReady = new List<LobbyPlayer> { Player("a", true), Player("b", true) };

        Assert.IsFalse(LobbyPlayer.AllReady(one, 2));
        Assert.IsFalse(LobbyPlayer.AllReady(twoWaiting, 2));
        Assert.IsTrue(LobbyPlayer.AllReady(twoReady, 2));
        Assert.IsFalse(LobbyPlayer.AllReady(twoReady, 3));
        Assert.IsFalse(LobbyPlayer.AllReady(twoReady, 0));
        Assert.IsFalse(LobbyPlayer.AllReady(null, 2));
    }

    [Test]
    public void Order_PutsYouFirstThenHost()
    {
        var players = new List<LobbyPlayer>
        {
            Player("c", false),
            new LobbyPlayer("b", "Host", "", false, true, false),
            new LobbyPlayer("z", "Me", "", false, false, true),
            Player("a", false)
        };

        LobbyPlayer.Order(players);

        CollectionAssert.AreEqual(new[] { "z", "b", "a", "c" }, players.ConvertAll(p => p.Id));
    }

    [Test]
    public void Classify_MapsServiceErrors()
    {
        Assert.AreEqual(OnlineLobby.Failure.NotFound, OnlineLobby.Classify(new SessionException("missing", SessionError.SessionNotFound, null)));
        Assert.AreEqual(OnlineLobby.Failure.Full, OnlineLobby.Classify(new SessionException("lobby is full", SessionError.Unknown, null)));
        Assert.AreEqual(OnlineLobby.Failure.NotFound, OnlineLobby.Classify(new SessionException("lobby code 'ZZ9Q7X' contains an invalid character 'Z' (U+005A) at index 0", SessionError.Unknown, null)));
        Assert.AreNotEqual(OnlineLobby.Failure.None, OnlineLobby.Classify(new Exception("boom")));
    }

    [Test]
    public void OnlineModes_AreConfiguredForLobbies()
    {
        var modes = new List<GameModeDefinition>();
        foreach (var guid in AssetDatabase.FindAssets("t:GameModeDefinition", new[] { "Assets/_Game/Data/Modes" }))
        {
            var mode = AssetDatabase.LoadAssetAtPath<GameModeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (mode != null && mode.Online) modes.Add(mode);
        }

        CollectionAssert.AreEquivalent(new[] { "portal_duel", "coop_online", "rush_battle", "live_duel" }, modes.ConvertAll(m => m.Id));
        foreach (var mode in modes)
        {
            Assert.GreaterOrEqual(mode.MinPlayers, 2, mode.Id);
            Assert.GreaterOrEqual(mode.MaxPlayers, mode.MinPlayers, mode.Id);
            Assert.IsFalse(string.IsNullOrEmpty(mode.DisplayName), mode.Id);
            Assert.IsFalse(string.IsNullOrEmpty(mode.Tagline), mode.Id);
        }

        Assert.IsTrue(modes.Exists(m => m.Id == "portal_duel" && !m.ComingSoon));
        Assert.IsTrue(modes.Exists(m => m.Id == "coop_online" && !m.ComingSoon && m.Kind == GameModeKind.CoopRally));
    }

    private static LobbyPlayer Player(string id, bool ready)
    {
        return new LobbyPlayer(id, id, "", ready, false, false);
    }
}
