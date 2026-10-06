using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class FriendsScreen : UIScreen
{
    private const string DialogHiddenClass = "dialog--hidden";

    private readonly OnlineFriends friends;
    private readonly OnlineService online;
    private readonly Action<string> onInvite;
    private readonly Action<string> toast;

    private readonly ScrollView scroll;
    private readonly VisualElement ready;
    private readonly VisualElement status;
    private readonly VisualElement statusIcon;
    private readonly Label statusTitle;
    private readonly Label statusText;
    private readonly Button retryButton;
    private readonly Label myTag;
    private readonly VisualElement requestsCard;
    private readonly Label requestsCount;
    private readonly VisualElement requestsList;
    private readonly Label friendsCount;
    private readonly VisualElement friendList;
    private readonly Label friendsEmpty;
    private readonly VisualElement sentCard;
    private readonly VisualElement sentList;

    private readonly VisualElement addDialog;
    private readonly TextField tagField;
    private readonly Button addConfirm;
    private readonly Label addConfirmLabel;

    private readonly VisualElement menu;
    private readonly Label menuName;
    private readonly Label menuText;
    private readonly Button menuRemove;
    private readonly Button menuBlock;

    private string menuTarget;
    private bool working;

    public FriendsScreen(VisualElement root, OnlineFriends friends, OnlineService online, Action onBack, Action<string> onInvite, Action<string> toast) : base(root)
    {
        this.friends = friends;
        this.online = online;
        this.onInvite = onInvite;
        this.toast = toast;

        scroll = root.Q<ScrollView>("friends-scroll");
        ready = root.Q("friends-ready");
        status = root.Q("friends-status");
        statusIcon = root.Q("friends-status-icon");
        statusTitle = root.Q<Label>("friends-status-title");
        statusText = root.Q<Label>("friends-status-text");
        myTag = root.Q<Label>("my-tag");
        requestsCard = root.Q("requests-card");
        requestsCount = root.Q<Label>("requests-count");
        requestsList = root.Q("requests-list");
        friendsCount = root.Q<Label>("friends-count");
        friendList = root.Q("friend-list");
        friendsEmpty = root.Q<Label>("friends-empty");
        sentCard = root.Q("sent-card");
        sentList = root.Q("sent-list");

        addDialog = root.Q("add-dialog");
        tagField = root.Q<TextField>("tag-field");
        tagField.keyboardType = TouchScreenKeyboardType.ASCIICapable;
        tagField.RegisterValueChangedCallback(_ => RefreshAddButton());
        addConfirmLabel = root.Q<Label>("add-confirm-label");
        root.Q("add-scrim").RegisterCallback<ClickEvent>(_ => CloseAdd());

        menu = root.Q("friend-menu");
        menuName = root.Q<Label>("menu-name");
        menuText = root.Q<Label>("menu-text");
        root.Q("menu-scrim").RegisterCallback<ClickEvent>(_ => CloseMenu());

        Bind("back-button", onBack);
        Bind("copy-tag", CopyTag);
        Bind("share-tag", ShareTag);
        Bind("add-open", OpenAdd);
        Bind("paste-button", Paste);
        addConfirm = Bind("add-confirm", ConfirmAdd);
        Bind("add-cancel", CloseAdd);
        menuRemove = Bind("menu-remove", () => RunMenu(false));
        menuBlock = Bind("menu-block", () => RunMenu(true));
        Bind("menu-cancel", CloseMenu);
        retryButton = Bind("friends-retry", Connect);

        if (friends != null) friends.Changed += OnChanged;
        if (online != null) online.StateChanged += _ => OnChanged();
    }

    public bool IsDialogOpen => !addDialog.ClassListContains(DialogHiddenClass) || !menu.ClassListContains(DialogHiddenClass);

    public void CloseDialogs()
    {
        CloseAdd();
        CloseMenu();
    }

    public static string ResultText(FriendsResult result, string name)
    {
        return result switch
        {
            FriendsResult.NotFound => Loc.T("No player with that code. Check it and try again."),
            FriendsResult.Self => Loc.T("That's your own code."),
            FriendsResult.AlreadyFriends => Loc.T("You're already friends or a request is waiting."),
            FriendsResult.Blocked => Loc.T("You can't add this player."),
            FriendsResult.Full => Loc.T("The friend list is full."),
            FriendsResult.Offline => Loc.T("You're offline. Connect and try again."),
            FriendsResult.Ok => string.IsNullOrEmpty(name) ? Loc.T("Friend request sent") : Loc.T("Friend request sent to {0}", name),
            _ => Loc.T("Something went wrong. Please try again.")
        };
    }

    protected override void OnShow()
    {
        scroll.scrollOffset = Vector2.zero;
        Connect();
        if (friends != null && friends.IsReady) _ = friends.RefreshAsync();
    }

    protected override void OnHide()
    {
        CloseDialogs();
    }

    private void Connect()
    {
        Refresh();
        if (online != null) _ = online.ConnectAsync();
    }

    private void OnChanged()
    {
        if (IsVisible) Refresh();
    }

    private void Refresh()
    {
        bool isReady = friends != null && friends.IsReady;
        ready.style.display = isReady ? DisplayStyle.Flex : DisplayStyle.None;
        status.style.display = isReady ? DisplayStyle.None : DisplayStyle.Flex;

        if (!isReady)
        {
            RefreshStatus();
            return;
        }

        myTag.text = friends.MyTag ?? string.Empty;

        requestsList.Clear();
        foreach (var friend in friends.Incoming) requestsList.Add(RequestRow(friend));
        requestsCount.text = friends.Incoming.Count.ToString();
        requestsCard.style.display = friends.Incoming.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        friendList.Clear();
        foreach (var friend in friends.Friends) friendList.Add(FriendRow(friend));
        friendsCount.text = friends.Friends.Count.ToString();
        friendsEmpty.style.display = friends.Friends.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        sentList.Clear();
        foreach (var friend in friends.Outgoing) sentList.Add(SentRow(friend));
        sentCard.style.display = friends.Outgoing.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        MarkFirst(requestsList);
        MarkFirst(friendList);
        MarkFirst(sentList);
    }

    private static void MarkFirst(VisualElement list)
    {
        if (list.childCount > 0) list[0].AddToClassList("friend-row--first");
    }

    private void RefreshStatus()
    {
        var state = online != null ? online.State : OnlineService.Status.Offline;
        bool connecting = state == OnlineService.Status.Connecting || state == OnlineService.Status.Ready || (friends != null && friends.State == OnlineFriends.Status.Starting);
        bool offline = state == OnlineService.Status.Offline && !OnlineService.HasInternet;

        statusIcon.EnableInClassList("icon--wifi-off", !connecting);
        statusIcon.EnableInClassList("icon--wifi", connecting);
        retryButton.style.display = connecting ? DisplayStyle.None : DisplayStyle.Flex;

        if (connecting)
        {
            statusTitle.text = Loc.T("Connecting…");
            statusText.text = Loc.T("Getting your friends list.");
        }
        else if (offline)
        {
            statusTitle.text = Loc.T("You're offline");
            statusText.text = Loc.T("Friends need the internet. Games on this device still work.");
        }
        else
        {
            statusTitle.text = Loc.T("Can't reach the servers");
            statusText.text = Loc.T("Online play is taking a break. Try again in a little while.");
        }
    }

    private VisualElement FriendRow(OnlineFriends.Friend friend)
    {
        var row = RowBase(friend.Name, StatusText(friend), friend.Status);

        if (friend.Status == FriendStatus.Online)
        {
            bool invited = friends.RecentlyInvited(friend.Id);
            var invite = UiFactory.Button("btn friend-invite", invited ? Loc.T("Invited") : Loc.T("Invite"), null, () => onInvite?.Invoke(friend.Id));
            invite.name = $"invite-{friend.Id}";
            invite.SetEnabled(!invited && !working);
            row.Add(invite);
        }

        var more = UiFactory.Button("btn friend-icon-btn friend-icon-btn--plain", null, "icon--more", () => OpenMenu(friend));
        more.name = $"more-{friend.Id}";
        row.Add(more);
        return row;
    }

    private VisualElement RequestRow(OnlineFriends.Friend friend)
    {
        var row = RowBase(friend.Name, Loc.T("Wants to be friends"), null);
        row.AddToClassList("friend-row--request");

        var accept = UiFactory.Button("btn friend-icon-btn friend-icon-btn--yes", null, "icon--check icon--light", () => Act(friends.AcceptAsync(friend.Id), Loc.T("You and {0} are friends now", friend.Name)));
        accept.name = $"accept-{friend.Id}";
        var decline = UiFactory.Button("btn friend-icon-btn friend-icon-btn--plain", null, "icon--close", () => Act(friends.DeclineAsync(friend.Id), null), true);
        decline.name = $"decline-{friend.Id}";
        accept.SetEnabled(!working);
        decline.SetEnabled(!working);
        row.Add(accept);
        row.Add(decline);
        return row;
    }

    private VisualElement SentRow(OnlineFriends.Friend friend)
    {
        var row = RowBase(friend.Name, Loc.T("Request sent"), null);
        var cancel = UiFactory.Button("btn friend-icon-btn friend-icon-btn--plain", null, "icon--close", () => Act(friends.CancelAsync(friend.Id), null), true);
        cancel.name = $"cancel-{friend.Id}";
        cancel.SetEnabled(!working);
        row.Add(cancel);
        return row;
    }

    private static VisualElement RowBase(string name, string sub, FriendStatus? status)
    {
        var row = UiFactory.Element("friend-row");
        if (status.HasValue)
        {
            var dot = UiFactory.Element("friend-dot");
            dot.EnableInClassList("friend-dot--online", status == FriendStatus.Online);
            dot.EnableInClassList("friend-dot--playing", status == FriendStatus.Playing);
            row.Add(dot);
        }

        var text = UiFactory.Element(status.HasValue ? "friend-row__text" : "friend-row__text friend-row__text--plain");
        text.Add(UiFactory.Text(name, "friend-row__name"));
        text.Add(UiFactory.Text(sub, "friend-row__status"));
        row.Add(text);
        return row;
    }

    public static string StatusText(OnlineFriends.Friend friend)
    {
        switch (friend.Status)
        {
            case FriendStatus.Online:
                return Loc.T("Online");
            case FriendStatus.Playing:
                return Loc.T("Playing#status");
        }

        if (friend.LastSeen == default) return Loc.T("Offline");
        var ago = DateTime.UtcNow - friend.LastSeen.ToUniversalTime();
        if (ago.TotalSeconds < 0 || ago.TotalDays > 365) return Loc.T("Offline");
        if (ago.TotalMinutes < 1) return Loc.T("Online just now");
        if (ago.TotalHours < 1) return Loc.T("Online {0} min ago", (int)ago.TotalMinutes);
        if (ago.TotalDays < 1) return Loc.T("Online {0} h ago", (int)ago.TotalHours);
        return Loc.Plural("Online {0} day ago", "Online {0} days ago", (int)ago.TotalDays);
    }

    private async void Act(System.Threading.Tasks.Task<FriendsResult> action, string success)
    {
        if (working) return;
        working = true;
        Refresh();
        var result = await action;
        working = false;
        if (result == FriendsResult.Ok)
        {
            if (!string.IsNullOrEmpty(success)) toast?.Invoke(success);
        }
        else toast?.Invoke(ResultText(result, null));
        Refresh();
    }

    private void CopyTag()
    {
        if (string.IsNullOrEmpty(friends?.MyTag)) return;
        NativeShare.Copy(friends.MyTag);
        toast?.Invoke(Loc.T("Code copied"));
    }

    private void ShareTag()
    {
        if (string.IsNullOrEmpty(friends?.MyTag)) return;
        if (!NativeShare.Text(Loc.T("Add me as a friend in Pingi Pongi! My code is {0}", friends.MyTag), Loc.T("Share your friend code")))
            toast?.Invoke(Loc.T("Code copied"));
    }

    private void OpenAdd()
    {
        if (friends == null || !friends.IsReady) return;
        tagField.SetValueWithoutNotify(string.Empty);
        addConfirmLabel.text = Loc.T("Send request");
        RefreshAddButton();
        addDialog.RemoveFromClassList(DialogHiddenClass);
        tagField.schedule.Execute(() => tagField.Focus()).StartingIn(50);
    }

    private void CloseAdd()
    {
        tagField.Blur();
        addDialog.AddToClassList(DialogHiddenClass);
    }

    private void Paste()
    {
        string tag = FriendTag.Parse(GUIUtility.systemCopyBuffer);
        if (tag == null)
        {
            toast?.Invoke(Loc.T("No friend code to paste."));
            return;
        }
        tagField.value = tag;
    }

    private void RefreshAddButton()
    {
        addConfirm.SetEnabled(!working && FriendTag.Parse(tagField.value) != null);
    }

    private async void ConfirmAdd()
    {
        string tag = FriendTag.Parse(tagField.value);
        if (tag == null || working || friends == null) return;

        working = true;
        addConfirmLabel.text = Loc.T("Sending…");
        RefreshAddButton();
        var result = await friends.AddByTagAsync(tag);
        working = false;
        addConfirmLabel.text = Loc.T("Send request");
        RefreshAddButton();

        toast?.Invoke(ResultText(result, FriendTag.NameOf(tag)));
        if (result == FriendsResult.Ok || result == FriendsResult.AlreadyFriends) CloseAdd();
        Refresh();
    }

    private void OpenMenu(OnlineFriends.Friend friend)
    {
        menuTarget = friend.Id;
        menuName.text = friend.Name;
        menuText.text = StatusText(friend);
        menuRemove.SetEnabled(true);
        menuBlock.SetEnabled(true);
        menu.RemoveFromClassList(DialogHiddenClass);
    }

    private void CloseMenu()
    {
        menu.AddToClassList(DialogHiddenClass);
        menuTarget = null;
    }

    private async void RunMenu(bool block)
    {
        string target = menuTarget;
        if (string.IsNullOrEmpty(target) || working || friends == null) return;

        string name = PlayerNames.ForId(target);
        working = true;
        menuRemove.SetEnabled(false);
        menuBlock.SetEnabled(false);
        var result = block ? await friends.BlockAsync(target) : await friends.RemoveAsync(target);
        working = false;
        CloseMenu();

        if (result == FriendsResult.Ok) toast?.Invoke(block ? Loc.T("{0} is blocked", name) : Loc.T("{0} was removed from your friends", name));
        else toast?.Invoke(ResultText(result, null));
        Refresh();
    }
}
