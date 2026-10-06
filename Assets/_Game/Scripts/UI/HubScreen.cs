using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HubScreen : UIScreen
{
    private const string DialogHiddenClass = "dialog--hidden";

    private readonly LocalMatchController match;
    private readonly CosmeticsService cosmetics;
    private readonly OnlineService online;
    private readonly OnlineLobby lobby;
    private readonly OnlineScores scores;
    private readonly Action<GameModeDefinition> onPick;
    private readonly Action<GameModeDefinition> onQuick;
    private readonly Action<GameModeDefinition> onCreate;
    private readonly Action<string> onJoin;
    private readonly Func<bool> hasGhost;
    private readonly Action onShareGhost;
    private readonly Action<string> onRaceGhost;
    private readonly Action<string> toast;
    private readonly OnlineFriends friends;

    private readonly ScrollView local;
    private readonly ScrollView onlinePanel;
    private readonly VisualElement challenges;
    private readonly VisualElement note;
    private readonly Button[] tabs;

    private readonly VisualElement onlineReady;
    private readonly VisualElement onlineStatus;
    private readonly Label statusTitle;
    private readonly Label statusText;
    private readonly VisualElement meBall;
    private readonly Label meName;
    private readonly Button lookButton;
    private readonly Button quickButton;
    private readonly Label quickLabel;
    private readonly Button createButton;
    private readonly Button joinButton;
    private readonly VisualElement modeList;
    private readonly Button friendsButton;
    private readonly Label friendsSummary;
    private readonly Label friendsBadge;

    private readonly Label dailyDate;
    private readonly Label dailyBest;
    private readonly Label dailyRank;
    private readonly Label dailyReset;

    private readonly Button ghostEnter;
    private readonly Label ghostEnterLabel;
    private readonly Button ghostShare;
    private readonly Label ghostShareLabel;

    private readonly VisualElement joinDialog;
    private readonly Label joinTitle;
    private readonly Label joinText;
    private readonly VisualElement joinIcon;
    private readonly Label joinLabel;
    private readonly TextField codeField;
    private readonly Button joinConfirm;

    private readonly List<CosmeticItem> looks = new List<CosmeticItem>();
    private GameModeDefinition selectedMode;
    private int tab;
    private bool busy;
    private bool ghostBusy;
    private bool joinForGhost;

    public HubScreen(VisualElement root, LocalMatchController match, CosmeticsService cosmetics, OnlineService online, OnlineLobby lobby,
        OnlineScores scores, Action onBack, Action<GameModeDefinition> onPick, Action<GameModeDefinition> onQuick, Action<GameModeDefinition> onCreate,
        Action<string> onJoin, Action onDaily, Func<bool> hasGhost, Action onShareGhost, Action<string> onRaceGhost, Action<string> toast,
        OnlineFriends friends, Action onFriends) : base(root)
    {
        this.friends = friends;
        this.hasGhost = hasGhost;
        this.onShareGhost = onShareGhost;
        this.onRaceGhost = onRaceGhost;
        this.scores = scores;
        this.match = match;
        this.cosmetics = cosmetics;
        this.online = online;
        this.lobby = lobby;
        this.onPick = onPick;
        this.onQuick = onQuick;
        this.onCreate = onCreate;
        this.onJoin = onJoin;
        this.toast = toast;

        local = root.Q<ScrollView>("hub-local");
        onlinePanel = root.Q<ScrollView>("hub-online");
        challenges = root.Q("hub-challenges");
        note = root.Q("hub-note");

        onlineReady = root.Q("online-ready");
        onlineStatus = root.Q("online-status");
        statusTitle = root.Q<Label>("status-title");
        statusText = root.Q<Label>("status-text");
        meBall = root.Q("me-ball");
        meName = root.Q<Label>("me-name");
        quickLabel = root.Q<Label>("quick-label");
        modeList = root.Q("online-modes");
        friendsSummary = root.Q<Label>("friends-summary");
        friendsBadge = root.Q<Label>("friends-badge");
        friendsButton = Bind("friends-button", onFriends);

        dailyDate = root.Q<Label>("daily-date");
        dailyBest = root.Q<Label>("daily-best");
        dailyRank = root.Q<Label>("daily-rank");
        dailyReset = root.Q<Label>("daily-reset");
        Bind("daily-button", onDaily);

        ghostEnter = Bind("ghost-enter", OpenGhostJoin);
        ghostEnterLabel = root.Q<Label>("ghost-enter-label");
        ghostShare = Bind("ghost-share-last", () => onShareGhost?.Invoke());
        ghostShareLabel = root.Q<Label>("ghost-share-label");

        joinDialog = root.Q("join-dialog");
        joinTitle = root.Q<Label>("join-title");
        joinText = root.Q<Label>("join-text");
        joinIcon = root.Q("join-confirm-icon");
        joinLabel = root.Q<Label>("join-confirm-label");
        codeField = root.Q<TextField>("code-field");
        codeField.keyboardType = TouchScreenKeyboardType.ASCIICapable;
        codeField.RegisterValueChangedCallback(OnCodeChanged);
        root.Q("join-scrim").RegisterCallback<ClickEvent>(_ => CloseJoin());

        Bind("back-button", onBack);
        tabs = new[]
        {
            Bind("tab-local", () => SelectTab(0)),
            Bind("tab-online", () => SelectTab(1)),
            Bind("tab-challenges", () => SelectTab(2))
        };
        lookButton = Bind("look-button", NextLook);
        quickButton = Bind("quick-button", () => onQuick?.Invoke(selectedMode));
        createButton = Bind("create-button", () => onCreate?.Invoke(selectedMode));
        joinButton = Bind("join-button", OpenJoin);
        joinConfirm = Bind("join-confirm", ConfirmJoin);
        Bind("join-cancel", CloseJoin);
        Bind("retry-button", Connect);

        if (online != null) online.StateChanged += _ => RefreshOnline();
        if (friends != null) friends.Changed += () =>
        {
            if (IsVisible) RefreshFriends();
        };
    }

    public GameModeDefinition SelectedMode => selectedMode;

    public bool IsJoinOpen => !joinDialog.ClassListContains(DialogHiddenClass);

    public string LookId
    {
        get
        {
            var look = CurrentLook();
            return look != null ? look.Id : string.Empty;
        }
    }

    public void Present(int tabIndex)
    {
        tab = tabIndex;
        if (IsVisible) SelectTab(tab);
        else Show();
    }

    public void SetBusy(bool value, string label)
    {
        busy = value;
        quickLabel.text = value && !string.IsNullOrEmpty(label) ? label : Loc.T("Quick match");
        RefreshOnline();
    }

    public bool GhostBusy => ghostBusy;

    public void SetGhostBusy(bool value, string enterLabel = null, string shareLabel = null)
    {
        ghostBusy = value;
        RefreshGhost();
        if (!value) return;
        if (!string.IsNullOrEmpty(enterLabel)) ghostEnterLabel.text = enterLabel;
        if (!string.IsNullOrEmpty(shareLabel)) ghostShareLabel.text = shareLabel;
    }

    public void RefreshGhost()
    {
        bool has = hasGhost != null && hasGhost();
        ghostEnter.SetEnabled(!ghostBusy);
        ghostShare.SetEnabled(!ghostBusy && has);
        ghostShare.EnableInClassList("ghost-share--off", !has);
        ghostEnterLabel.text = Loc.T("Enter code");
        ghostShareLabel.text = has ? Loc.T("Share my last Rush run") : Loc.T("Play a Rush run to share it");
    }

    public void CloseJoin()
    {
        codeField.Blur();
        joinDialog.AddToClassList(DialogHiddenClass);
    }

    protected override void OnShow()
    {
        BuildTiles();
        CollectLooks();
        BuildModes();
        SelectTab(tab);
    }

    protected override void OnHide()
    {
        CloseJoin();
    }

    private void SelectTab(int index)
    {
        tab = index;
        for (int i = 0; i < tabs.Length; i++) tabs[i]?.EnableInClassList("tab--active", i == index);

        local.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        onlinePanel.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        challenges.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        note.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        if (index == 1) Connect();
        if (index == 2)
        {
            RefreshDaily();
            RefreshGhost();
        }
    }

    private void RefreshDaily()
    {
        var now = DateTime.UtcNow;
        int best = DailyChallenge.BestToday(now);
        dailyDate.text = DailyChallenge.Label(now);
        dailyBest.text = best > 0 ? best.ToString() : "–";
        dailyReset.text = DailyChallenge.FormatResetsIn(DailyChallenge.ResetsIn(now));
        dailyRank.text = "–";
        if (best > 0) _ = LoadDailyRank();
    }

    private async System.Threading.Tasks.Task LoadDailyRank()
    {
        if (online == null || scores == null) return;

        await online.ConnectAsync();
        int rank = await scores.RankAsync(OnlineScores.DailyBoard);
        if (rank > 0 && IsVisible && tab == 2) dailyRank.text = $"#{rank}";
    }

    private void Connect()
    {
        RefreshOnline();
        if (online != null) _ = online.ConnectAsync();
    }

    private void RefreshOnline()
    {
        if (online == null) return;

        var state = online.State;
        bool usable = state == OnlineService.Status.Ready || state == OnlineService.Status.Connecting;
        onlineReady.style.display = usable ? DisplayStyle.Flex : DisplayStyle.None;
        onlineStatus.style.display = usable ? DisplayStyle.None : DisplayStyle.Flex;

        if (!usable)
        {
            bool offline = state == OnlineService.Status.Offline;
            statusTitle.text = offline ? Loc.T("You're offline") : Loc.T("Can't reach the servers");
            statusText.text = offline
                ? Loc.T("Online play needs the internet. Games on this device still work.")
                : Loc.T("Online play is taking a break. Try again in a little while.");
            return;
        }

        bool ready = state == OnlineService.Status.Ready;
        meName.text = ready ? online.PlayerName : Loc.T("Connecting…");
        UiFactory.SetPicture(meBall, CurrentLook()?.Happy);

        RefreshFriends();

        bool canPlay = ready && !busy && selectedMode != null;
        quickButton.SetEnabled(canPlay);
        createButton.SetEnabled(canPlay);
        joinButton.SetEnabled(ready && !busy);
        lookButton.SetEnabled(!busy && looks.Count > 1);
    }

    private void RefreshFriends()
    {
        if (friendsButton == null) return;

        bool usable = friends != null && friends.IsReady;
        friendsButton.SetEnabled(online != null && online.IsReady);
        int requests = usable ? friends.Incoming.Count : 0;
        friendsBadge.text = requests.ToString();
        friendsBadge.style.display = requests > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        if (!usable) friendsSummary.text = Loc.T("Connecting…");
        else if (requests > 0) friendsSummary.text = Loc.Plural("{0} friend request", "{0} friend requests", requests);
        else if (friends.Friends.Count == 0) friendsSummary.text = Loc.T("Add friends to invite them");
        else friendsSummary.text = Loc.T("{0} online · {1} in total", friends.OnlineCount, friends.Friends.Count);
    }

    private void CollectLooks()
    {
        looks.Clear();
        if (cosmetics == null) return;

        var equipped = cosmetics.Equipped(CosmeticCategory.Ball);
        if (equipped != null) looks.Add(equipped);

        foreach (var item in cosmetics.CatalogAsset.InCategory(CosmeticCategory.Ball))
            if (item != equipped && cosmetics.IsUnlocked(item)) looks.Add(item);
    }

    private CosmeticItem CurrentLook()
    {
        if (looks.Count == 0) return null;

        string saved = GameSettings.OnlineLook;
        foreach (var look in looks)
            if (look.Id == saved) return look;
        return looks[0];
    }

    private void NextLook()
    {
        if (looks.Count < 2) return;

        int index = looks.IndexOf(CurrentLook());
        var next = looks[(index + 1) % looks.Count];
        GameSettings.OnlineLook = next.Id;
        UiFactory.SetPicture(meBall, next.Happy);
    }

    private void BuildModes()
    {
        modeList.Clear();
        if (lobby == null) return;

        if (selectedMode != null && selectedMode.ComingSoon) selectedMode = null;

        var modes = lobby.ModeList;
        for (int i = 0; i < modes.Count; i++)
        {
            var mode = modes[i];
            if (mode == null) continue;
            if (selectedMode == null && !mode.ComingSoon) selectedMode = mode;
            if (modeList.childCount > 0) modeList.Add(UiFactory.Element("online-mode-divider"));
            modeList.Add(ModeRow(mode));
        }
    }

    private VisualElement ModeRow(GameModeDefinition mode)
    {
        var row = new Button { focusable = false, name = $"online-{mode.Id}" };
        row.RemoveFromClassList(Button.ussClassName);
        row.AddToClassList("online-mode");
        row.EnableInClassList("online-mode--soon", mode.ComingSoon);
        row.EnableInClassList("online-mode--selected", mode == selectedMode);
        row.clicked += () => PickMode(mode);

        row.Add(UiFactory.Element("online-mode__pick"));

        var text = UiFactory.Element("online-mode__text");
        text.Add(UiFactory.Text(mode.Title, "online-mode__name"));
        text.Add(UiFactory.Text(mode.Blurb, "online-mode__sub"));
        row.Add(text);

        if (mode.ComingSoon)
        {
            row.Add(UiFactory.Text(Loc.T("Soon"), "soon-chip"));
            return row;
        }

        string players = mode.MinPlayers == mode.MaxPlayers ? $"{mode.MinPlayers}" : $"{mode.MinPlayers}–{mode.MaxPlayers}";
        row.Add(UiFactory.Text(players, mode.Kind == GameModeKind.CoopRally ? "players-chip players-chip--team" : "players-chip"));
        return row;
    }

    private void PickMode(GameModeDefinition mode)
    {
        if (mode.ComingSoon)
        {
            UiFeedback.Back();
            toast?.Invoke(Loc.T("{0} is coming soon.", mode.Title));
            return;
        }

        UiFeedback.Tap();
        selectedMode = mode;
        BuildModes();
        RefreshOnline();
    }

    private void OpenJoin()
    {
        SetJoinTarget(false);
        ShowJoin();
    }

    private void OpenGhostJoin()
    {
        if (ghostBusy) return;
        SetJoinTarget(true);
        ShowJoin();
    }

    private void SetJoinTarget(bool ghost)
    {
        joinForGhost = ghost;
        joinTitle.text = ghost ? Loc.T("Race a ghost") : Loc.T("Join a friend");
        joinText.text = ghost ? Loc.T("Type the ghost code your friend shared.") : Loc.T("Type the code on your friend's screen.");
        joinLabel.text = ghost ? Loc.T("Race") : Loc.T("Join");
        joinIcon.EnableInClassList("icon--join", !ghost);
        joinIcon.EnableInClassList("icon--play", ghost);
    }

    private bool CodeValid(string code)
    {
        return joinForGhost ? GhostCode.IsValid(code) : SessionCode.IsValid(code);
    }

    private void ShowJoin()
    {
        codeField.SetValueWithoutNotify(string.Empty);
        joinConfirm.SetEnabled(false);
        joinDialog.RemoveFromClassList(DialogHiddenClass);
        codeField.schedule.Execute(() => codeField.Focus()).StartingIn(50);
    }

    private void OnCodeChanged(ChangeEvent<string> evt)
    {
        string code = SessionCode.Normalize(evt.newValue);
        if (code.Length > SessionCode.Length) code = code.Substring(0, SessionCode.Length);
        if (code != evt.newValue) codeField.SetValueWithoutNotify(code);
        joinConfirm.SetEnabled(CodeValid(code));
    }

    private void ConfirmJoin()
    {
        string code = SessionCode.Normalize(codeField.value);
        if (!CodeValid(code)) return;

        CloseJoin();
        if (joinForGhost) onRaceGhost?.Invoke(code);
        else onJoin?.Invoke(code);
    }

    private void BuildTiles()
    {
        local.Clear();
        if (match == null) return;

        foreach (var mode in match.ModeList)
            if (mode != null) local.Add(Tile(mode));
    }

    private VisualElement Tile(GameModeDefinition mode)
    {
        bool fits = LocalMatchController.FitsScreen(mode);

        var tile = new Button { focusable = false, name = $"tile-{mode.Id}" };
        tile.RemoveFromClassList(Button.ussClassName);
        tile.AddToClassList("hub-tile");
        tile.EnableInClassList("hub-tile--locked", !fits);
        tile.clicked += () =>
        {
            UiFeedback.Tap();
            onPick?.Invoke(mode);
        };

        var art = UiFactory.Element("hub-tile__art");
        art.Add(Art(mode));
        tile.Add(art);

        var text = UiFactory.Element("hub-tile__text");
        text.Add(UiFactory.Text(mode.Title, "hub-tile__name"));
        text.Add(UiFactory.Text(mode.Blurb, "hub-tile__sub"));
        if (!fits)
        {
            var lockRow = UiFactory.Element("hub-tile__lock");
            lockRow.Add(UiFactory.Element("icon icon--lock"));
            lockRow.Add(UiFactory.Text(Loc.T("Tablet only"), "hub-tile__lock-label"));
            text.Add(lockRow);
        }
        tile.Add(text);

        string players = mode.MinPlayers == mode.MaxPlayers ? $"{mode.MinPlayers}" : $"{mode.MinPlayers}–{mode.MaxPlayers}";
        tile.Add(UiFactory.Text(players, mode.Kind == GameModeKind.CoopRally ? "players-chip players-chip--team" : "players-chip"));
        return tile;
    }

    private VisualElement Art(GameModeDefinition mode)
    {
        switch (mode.Kind)
        {
            case GameModeKind.TableDuel:
                var duel = UiFactory.Element("");
                duel.style.alignItems = Align.Center;
                duel.Add(UiFactory.Picture(match.PaddleSpriteFor(FieldSide.Top), "hub-art-paddle hub-art-paddle--flip"));
                duel.Add(UiFactory.Text(Loc.T("vs"), "hub-art-vs"));
                duel.Add(UiFactory.Picture(match.PaddleSpriteFor(FieldSide.Bottom), "hub-art-paddle"));
                return duel;

            case GameModeKind.CoopRally:
                var pair = UiFactory.Element("hub-art-row");
                pair.Add(UiFactory.Picture(Ball("ball_pingi"), "hub-art-ball"));
                pair.Add(UiFactory.Picture(Ball("ball_minty"), "hub-art-ball"));
                return pair;

            default:
                var grid = UiFactory.Element("hub-art-grid");
                foreach (var id in new[] { "ball_pingi", "ball_minty", "ball_kitty", "ball_grape" })
                    grid.Add(UiFactory.Picture(Ball(id), "hub-art-ball"));
                return grid;
        }
    }

    private Sprite Ball(string id)
    {
        var item = cosmetics != null ? cosmetics.CatalogAsset.Find(id) : null;
        return item != null ? item.Idle : null;
    }
}
