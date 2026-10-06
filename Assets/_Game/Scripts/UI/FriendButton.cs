using System;
using UnityEngine.UIElements;

public sealed class FriendButton
{
    private readonly Button button;
    private readonly Label label;
    private readonly VisualElement icon;
    private readonly OnlineFriends friends;
    private readonly Action<string> toast;
    private string playerId;
    private bool working;

    public FriendButton(Button button, OnlineFriends friends, Action<string> toast)
    {
        this.button = button;
        this.friends = friends;
        this.toast = toast;
        label = button?.Q<Label>(className: "ghost-share__label");
        icon = button?.Q(className: "icon");
        if (button == null) return;

        button.focusable = false;
        button.clicked += OnClick;
        if (friends != null) friends.Changed += Refresh;
    }

    public void SetPlayer(string id)
    {
        playerId = id;
        Refresh();
    }

    public void Refresh()
    {
        if (button == null) return;

        var relation = friends != null && friends.IsReady ? friends.RelationTo(playerId) : OnlineFriends.Relation.Self;
        bool show = friends != null && friends.IsReady && !string.IsNullOrEmpty(playerId) && relation != OnlineFriends.Relation.Self;
        button.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (!show) return;

        bool done = relation == OnlineFriends.Relation.Friend || relation == OnlineFriends.Relation.Sent;
        label.text = relation switch
        {
            OnlineFriends.Relation.Friend => Loc.T("You're friends"),
            OnlineFriends.Relation.Sent => Loc.T("Friend request sent"),
            OnlineFriends.Relation.Received => Loc.T("Accept friend request"),
            _ => Loc.T("Add friend")
        };
        icon?.EnableInClassList("icon--person-add", !done);
        icon?.EnableInClassList("icon--check", done);
        button.EnableInClassList("ghost-share--off", done);
        button.SetEnabled(!done && !working);
    }

    private async void OnClick()
    {
        if (friends == null || string.IsNullOrEmpty(playerId) || working) return;

        UiFeedback.Tap();
        var relation = friends.RelationTo(playerId);
        string name = PlayerNames.ForId(playerId);
        working = true;
        Refresh();
        var result = relation == OnlineFriends.Relation.Received ? await friends.AcceptAsync(playerId) : await friends.AddAsync(playerId);
        working = false;
        Refresh();

        if (result == FriendsResult.Ok && relation == OnlineFriends.Relation.Received) toast?.Invoke(Loc.T("You and {0} are friends now", name));
        else toast?.Invoke(FriendsScreen.ResultText(result, name));
    }
}
