using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class FriendsPlayTests : GameSceneFixture
{
    private const string OnlineFriend = "friend-online";
    private const string PlayingFriend = "friend-playing";
    private const string OfflineFriend = "friend-offline";
    private const string Requester = "friend-request";
    private const string Pending = "friend-pending";

    private FakeFriendsBackend fake;

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;
    private static VisualElement FriendsRoot => Root.Q("friends");
    private static OnlineFriends Friends => Object.FindFirstObjectByType<OnlineFriends>();
    private static string ToastText => Root.Q<Label>("toast-label").text;

    private static void Invoke(string method)
    {
        var ui = Object.FindFirstObjectByType<GameUI>();
        typeof(GameUI).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
    }

    private static void Click(Button button)
    {
        Assert.IsNotNull(button, "Button missing");
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    private static IEnumerator Await(Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) throw task.Exception;
    }

    private static IEnumerator WaitUntil(Func<bool> condition, float seconds, string because)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), because);
    }

    private static bool Shown(VisualElement element)
    {
        for (var e = element; e != null; e = e.parent)
            if (e.resolvedStyle.display == DisplayStyle.None) return false;
        return true;
    }

    private IEnumerator StartFriends()
    {
        yield return LoadGame();
        fake = new FakeFriendsBackend();
        fake.FriendList.Add(new FriendRecord(OfflineFriend, FriendStatus.Offline, DateTime.UtcNow.AddHours(-3)));
        fake.FriendList.Add(new FriendRecord(PlayingFriend, FriendStatus.Playing, default));
        fake.FriendList.Add(new FriendRecord(OnlineFriend, FriendStatus.Online, default));
        fake.IncomingList.Add(Requester);
        fake.OutgoingList.Add(Pending);

        Assert.IsNotNull(Friends, "OnlineFriends missing in scene");
        yield return Await(Friends.StartAsync(fake));
        Assert.IsTrue(Friends.IsReady, "Friends start with the fake service");
    }

    private IEnumerator OpenFriends()
    {
        Invoke("OpenFriends");
        yield return null;
        yield return null;
        Assert.IsTrue(Shown(FriendsRoot), "The friends screen is open");
    }

    [UnityTest]
    public IEnumerator FriendsScreen_ListsRequestsFriendsAndSentRequests()
    {
        yield return StartFriends();
        yield return OpenFriends();

        Assert.AreEqual(Friends.MyTag, FriendsRoot.Q<Label>("my-tag").text);
        StringAssert.EndsWith("#1234", Friends.MyTag);

        var names = FriendsRoot.Q("friend-list").Query<Label>(className: "friend-row__name").ToList().Select(l => l.text).ToList();
        CollectionAssert.AreEqual(new[] { PlayerNames.ForId(OnlineFriend), PlayerNames.ForId(PlayingFriend), PlayerNames.ForId(OfflineFriend) }, names, "Online first, then playing, then offline");

        Assert.IsNotNull(FriendsRoot.Q<Button>($"invite-{OnlineFriend}"), "Online friends can be invited");
        Assert.IsNull(FriendsRoot.Q<Button>($"invite-{PlayingFriend}"), "Busy friends can't be invited");
        Assert.IsNull(FriendsRoot.Q<Button>($"invite-{OfflineFriend}"), "Offline friends can't be invited");

        Assert.IsTrue(Shown(FriendsRoot.Q("requests-card")), "Requests are shown");
        Assert.AreEqual("1", FriendsRoot.Q<Label>("requests-count").text);
        Assert.IsTrue(Shown(FriendsRoot.Q("sent-card")), "Sent requests are shown");
        Assert.AreEqual(Loc.T("Online {0} h ago", 3), FriendsRoot.Q("friend-list").Query<Label>(className: "friend-row__status").ToList().Last().text);
    }

    [UnityTest]
    public IEnumerator AcceptingARequest_MakesAFriend()
    {
        yield return StartFriends();
        yield return OpenFriends();

        Click(FriendsRoot.Q<Button>($"accept-{Requester}"));
        yield return WaitUntil(() => FriendsRoot.Q<Button>($"accept-{Requester}") == null, 2f, "The request leaves the list");

        CollectionAssert.Contains(fake.Calls, $"add {Requester}");
        Assert.AreEqual(OnlineFriends.Relation.Friend, Friends.RelationTo(Requester));
        Assert.IsFalse(Shown(FriendsRoot.Q("requests-card")), "No requests left");
        Assert.AreEqual(Loc.T("You and {0} are friends now", PlayerNames.ForId(Requester)), ToastText);
    }

    [UnityTest]
    public IEnumerator AddingByCode_SendsTheCleanCode()
    {
        yield return StartFriends();
        yield return OpenFriends();
        fake.KnownTags.Add("BraveOtter#4821");

        Click(FriendsRoot.Q<Button>("add-open"));
        var field = FriendsRoot.Q<TextField>("tag-field");
        var confirm = FriendsRoot.Q<Button>("add-confirm");
        field.value = "nope";
        yield return null;
        Assert.IsFalse(confirm.enabledSelf, "Not a code yet");

        field.value = "CalmKoala#1111";
        yield return null;
        Click(confirm);
        yield return WaitUntil(() => ToastText == FriendsScreen.ResultText(FriendsResult.NotFound, null), 2f, "Unknown codes are explained");
        Assert.IsFalse(FriendsRoot.Q("add-dialog").ClassListContains("dialog--hidden"), "The dialog stays open to fix the code");

        field.value = "brave otter # 4821";
        yield return null;
        Assert.IsTrue(confirm.enabledSelf, "A messy but valid code can be sent");
        Click(confirm);
        yield return WaitUntil(() => FriendsRoot.Q("add-dialog").ClassListContains("dialog--hidden"), 2f, "The dialog closes after sending");

        CollectionAssert.Contains(fake.Calls, "tag BraveOtter#4821");
        Assert.AreEqual(FriendsScreen.ResultText(FriendsResult.Ok, "BraveOtter"), ToastText);
    }

    [UnityTest]
    public IEnumerator RemovingAFriend_UsesTheMenu()
    {
        yield return StartFriends();
        yield return OpenFriends();

        Click(FriendsRoot.Q<Button>($"more-{OfflineFriend}"));
        Assert.IsFalse(FriendsRoot.Q("friend-menu").ClassListContains("dialog--hidden"), "The menu opens");
        Assert.AreEqual(PlayerNames.ForId(OfflineFriend), FriendsRoot.Q<Label>("menu-name").text);

        Click(FriendsRoot.Q<Button>("menu-remove"));
        yield return WaitUntil(() => FriendsRoot.Q<Button>($"more-{OfflineFriend}") == null, 2f, "The friend leaves the list");
        CollectionAssert.Contains(fake.Calls, $"remove {OfflineFriend}");
        Assert.IsTrue(FriendsRoot.Q("friend-menu").ClassListContains("dialog--hidden"), "The menu closes");
    }

    [UnityTest]
    public IEnumerator Invite_FromAFriend_ShowsABannerThatJoins()
    {
        yield return StartFriends();
        var banner = Root.Q("invite-banner");

        fake.Invite("stranger", "K7M3QX", "portal_duel");
        yield return null;
        yield return null;
        Assert.IsFalse(Shown(banner), "Strangers can't invite");

        fake.Invite(OnlineFriend, "K7M3QX", "portal_duel");
        yield return WaitUntil(() => Shown(banner), 2f, "The invite shows up in the menu");
        Assert.AreEqual(Loc.T("{0} invites you", PlayerNames.ForId(OnlineFriend)), Root.Q<Label>("invite-title").text);

        Click(Root.Q<Button>("invite-join"));
        yield return null;
        Assert.IsFalse(Shown(banner), "The banner closes");
        Assert.IsTrue(Shown(Root.Q("hub")), "Joining opens Play together");
        Assert.IsTrue(Root.Q("hub").Q<Button>("tab-online").ClassListContains("tab--active"), "On the Online tab");
        string expected = OnlineService.HasInternet ? Loc.T("Something went wrong. Please try again.") : Loc.T("You're offline. Check your connection and try again.");
        yield return WaitUntil(() => ToastText == expected, 3f, "The join was attempted with the code");
    }

    [UnityTest]
    public IEnumerator Invite_WaitsUntilTheRunEnds_AndPresenceFollowsPlay()
    {
        yield return StartFriends();
        var banner = Root.Q("invite-banner");
        yield return null;
        Assert.AreEqual(FriendStatus.Online, fake.Presence.Last(), "Online in the menu");

        Game.StartGameFromMenu();
        yield return null;
        yield return null;
        Assert.AreEqual(FriendStatus.Playing, fake.Presence.Last(), "Playing during a run");

        fake.Invite(OnlineFriend, "K7M3QX", "portal_duel");
        for (int i = 0; i < 10; i++) yield return null;
        Assert.IsFalse(Shown(banner), "No invite while playing");

        Game.GameOver();
        Game.ReturnToMenu();
        yield return WaitUntil(() => Shown(banner), 2f, "The invite shows after the run");
        Assert.AreEqual(FriendStatus.Online, fake.Presence.Last(), "Back online in the menu");
    }

    [UnityTest]
    public IEnumerator NewRequest_ShowsAToast()
    {
        yield return StartFriends();
        fake.IncomingList.Add("friend-new");
        fake.Raise();
        yield return null;
        Assert.AreEqual(Loc.T("{0} sent you a friend request", PlayerNames.ForId("friend-new")), ToastText);
        Assert.AreEqual(2, Friends.Incoming.Count);
    }

    [UnityTest]
    public IEnumerator FriendsTab_WithoutFriends_OffersToFindThem()
    {
        yield return LoadGame();
        fake = new FakeFriendsBackend();
        yield return Await(Friends.StartAsync(fake));

        Invoke("OpenScores");
        yield return null;
        var scores = Root.Q("scores");
        Click(scores.Q<Button>("tab-friends"));
        yield return WaitUntil(() => scores.Q<Label>("world-note").text == Loc.T("Add friends to compare your scores."), 3f, "An empty friends board explains itself");

        var find = scores.Q<Button>("world-find");
        Assert.IsTrue(Shown(find), "Find friends is offered");
        Click(find);
        yield return null;
        Assert.IsTrue(Shown(FriendsRoot), "Find friends opens the friends screen");
    }
}
