using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ScoresScreen : UIScreen
{
    private const string ActiveTabClass = "tab--active";
    private const int StreakGoal = 7;

    private readonly SoloGameManager game;
    private readonly CosmeticsService cosmetics;
    private readonly Button tabMe;
    private readonly Button tabWorld;
    private readonly Button tabFriends;
    private readonly Button findButton;
    private readonly OnlineFriends friends;
    private readonly VisualElement mePanel;
    private readonly VisualElement worldPanel;
    private readonly Label statGames;
    private readonly Label statHits;
    private readonly Label statStreak;
    private readonly Label statTime;
    private readonly VisualElement matchRecords;
    private readonly VisualElement records;
    private readonly Label streakTitle;
    private readonly VisualElement streakDays;
    private readonly Label streakNote;
    private static readonly string[] Boards = { OnlineScores.ClassicBoard, OnlineScores.RushBoard, OnlineScores.DailyBoard, OnlineScores.CoopBoard };

    private readonly OnlineScores onlineScores;
    private readonly VisualElement worldModes;
    private readonly Label worldTitle;
    private readonly VisualElement worldRows;
    private readonly Label worldNote;
    private readonly ScrollView scroll;

    private string worldBoard = OnlineScores.ClassicBoard;
    private int worldRequest;
    private int tab;
    private int friendsSignature = -1;

    public ScoresScreen(VisualElement root, SoloGameManager game, CosmeticsService cosmetics, OnlineScores onlineScores, Action onBack,
        OnlineFriends friends, Action onFindFriends) : base(root)
    {
        this.onlineScores = onlineScores;
        this.friends = friends;
        this.game = game;
        this.cosmetics = cosmetics;

        mePanel = root.Q("me-panel");
        worldPanel = root.Q("world-panel");
        statGames = root.Q<Label>("stat-games");
        statHits = root.Q<Label>("stat-hits");
        statStreak = root.Q<Label>("stat-streak");
        statTime = root.Q<Label>("stat-time");
        matchRecords = root.Q("match-records");
        records = root.Q("records");
        streakTitle = root.Q<Label>("streak-title");
        streakDays = root.Q("streak-days");
        streakNote = root.Q<Label>("streak-note");
        worldModes = root.Q("world-modes");
        worldTitle = root.Q<Label>("world-title");
        worldRows = root.Q("world-rows");
        worldNote = root.Q<Label>("world-note");
        scroll = root.Q<ScrollView>("scores-scroll");

        Bind("back-button", onBack);
        tabMe = Bind("tab-me", () => SelectTab(0));
        tabWorld = Bind("tab-world", () => SelectTab(1));
        tabFriends = Bind("tab-friends", () => SelectTab(2));
        findButton = Bind("world-find", onFindFriends);
        if (friends != null) friends.Changed += OnFriendsChanged;
    }

    private void OnFriendsChanged()
    {
        int signature = friends.IsReady ? friends.Friends.Count + 1 : 0;
        if (signature == friendsSignature) return;
        friendsSignature = signature;
        if (IsVisible && tab == 2) RefreshWorld();
    }

    protected override void OnShow()
    {
        scroll.scrollOffset = Vector2.zero;
        worldBoard = OnlineScores.BoardFor(game.CurrentMode) ?? OnlineScores.ClassicBoard;
        SelectTab(0);
    }

    private void SelectTab(int index)
    {
        tab = index;
        bool world = index != 0;
        tabMe.EnableInClassList(ActiveTabClass, index == 0);
        tabWorld.EnableInClassList(ActiveTabClass, index == 1);
        tabFriends?.EnableInClassList(ActiveTabClass, index == 2);
        mePanel.style.display = world ? DisplayStyle.None : DisplayStyle.Flex;
        worldPanel.style.display = world ? DisplayStyle.Flex : DisplayStyle.None;

        if (world) RefreshWorld();
        else RefreshMe();
    }

    private void RefreshMe()
    {
        var source = game.RecordsSource;
        if (source == null || source.Records == null) return;

        var data = source.Records;
        statGames.text = data.GamesPlayed.ToString("N0", Loc.Culture);
        statHits.text = data.TotalHits.ToString("N0", Loc.Culture);
        statStreak.text = data.LongestStreak.ToString();
        statTime.text = FormatDuration(data.PlayTimeSeconds);

        records.Clear();
        foreach (var mode in game.ModeList)
        {
            if (mode == null) continue;
            int best = game.BestFor(mode);
            records.Add(RecordRow(mode.Title, best > 0 ? best.ToString() : "-", FormatDate(data.BestDate(mode.Id), best > 0)));
        }

        matchRecords.Clear();
        matchRecords.Add(RecordRow(Loc.T("Matches"), data.MatchesPlayed.ToString("N0", Loc.Culture), ""));
        matchRecords.Add(RecordRow(Loc.T("Best co-op rally"), data.BestCoopRally > 0 ? data.BestCoopRally.ToString() : "-", ""));

        int streak = source.DayStreak;
        streakTitle.text = streak > 0 ? Loc.Plural("{0}-day streak", "{0}-day streak#other", streak) : Loc.T("Daily streak");

        var reward = FindStreakReward();
        bool rewardOwned = reward != null && cosmetics != null && cosmetics.IsUnlocked(reward);

        streakDays.Clear();
        for (int i = 0; i < StreakGoal; i++)
        {
            var day = new VisualElement { pickingMode = PickingMode.Ignore };
            day.AddToClassList("streak-day");
            bool done = i < Mathf.Min(streak, StreakGoal);
            day.EnableInClassList("streak-day--done", done);

            bool rewardSlot = i == StreakGoal - 1 && reward != null && !done;
            if (rewardSlot)
            {
                day.AddToClassList("streak-day--reward");
                day.style.backgroundImage = new StyleBackground(reward.Happy);
            }
            else
            {
                var label = new Label((i + 1).ToString()) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("streak-day__label");
                day.Add(label);
            }

            streakDays.Add(day);
        }

        if (reward == null) streakNote.text = "";
        else if (rewardOwned) streakNote.text = Loc.T("{0} unlocked. Keep the streak going!", reward.Title);
        else streakNote.text = source.Records.PlayedToday(DateTime.Now)
            ? Loc.T("Day {0}: {1}. Come back tomorrow!", StreakGoal, reward.Title)
            : Loc.T("Day {0}: {1}. Play today to keep it!", StreakGoal, reward.Title);
    }

    private void RefreshWorld()
    {
        worldModes.Clear();
        foreach (var board in Boards)
        {
            var chip = new Button { text = BoardLabel(board), focusable = false };
            chip.RemoveFromClassList(Button.ussClassName);
            chip.AddToClassList("mode-chip");
            chip.EnableInClassList("mode-chip--active", board == worldBoard);
            chip.clicked += () =>
            {
                UiFeedback.Tap();
                worldBoard = board;
                RefreshWorld();
            };
            worldModes.Add(chip);
        }

        bool daily = worldBoard == OnlineScores.DailyBoard;
        if (tab == 2) worldTitle.text = daily ? Loc.T("Friends today · {0}", DailyChallenge.Label(DateTime.UtcNow)) : Loc.T("You and your friends");
        else worldTitle.text = daily ? Loc.T("Today · {0}", DailyChallenge.Label(DateTime.UtcNow)) : Loc.T("Top {0}", onlineScores != null ? onlineScores.Top : 50);
        worldRows.Clear();
        worldNote.text = Loc.T("Loading…");
        worldNote.style.display = DisplayStyle.Flex;
        findButton.style.display = DisplayStyle.None;
        if (tab == 2) _ = LoadFriends(++worldRequest, worldBoard);
        else _ = LoadWorld(++worldRequest, worldBoard);
    }

    private async System.Threading.Tasks.Task LoadFriends(int request, string board)
    {
        if (onlineScores == null || friends == null)
        {
            worldNote.text = Loc.T("World rankings are not available.");
            return;
        }

        await onlineScores.SyncBestsAsync(game.BestFor(game.FindMode("classic")), game.BestFor(game.FindMode("rush")));
        if (request != worldRequest || !IsVisible || tab != 2) return;
        if (!friends.IsReady)
        {
            worldNote.text = OnlineService.HasInternet ? Loc.T("Connecting…") : Loc.T("You're offline. Connect to see your friends' scores.");
            return;
        }

        var ids = new System.Collections.Generic.List<string>();
        foreach (var friend in friends.Friends) ids.Add(friend.Id);
        if (ids.Count == 0)
        {
            worldNote.text = Loc.T("Add friends to compare your scores.");
            findButton.style.display = DisplayStyle.Flex;
            return;
        }

        var result = await onlineScores.LoadFriendsAsync(board, ids);
        if (request != worldRequest || !IsVisible || tab != 2) return;
        Fill(result, board, true);
    }

    private async System.Threading.Tasks.Task LoadWorld(int request, string board)
    {
        if (onlineScores == null)
        {
            worldNote.text = Loc.T("World rankings are not available.");
            return;
        }

        await onlineScores.SyncBestsAsync(game.BestFor(game.FindMode("classic")), game.BestFor(game.FindMode("rush")));
        var result = await onlineScores.LoadAsync(board);
        if (request != worldRequest || !IsVisible || tab != 1) return;
        Fill(result, board, false);
    }

    private void Fill(OnlineScores.Board result, string board, bool friendsOnly)
    {
        worldRows.Clear();
        switch (result.Outcome)
        {
            case OnlineScores.Outcome.Offline:
                worldNote.text = Loc.T("You're offline. Connect to see the world rankings.");
                return;
            case OnlineScores.Outcome.Missing:
                worldNote.text = Loc.T("Rankings open soon.");
                return;
            case OnlineScores.Outcome.Failed:
                worldNote.text = Loc.T("Couldn't load the rankings. Try again in a little while.");
                return;
        }

        bool meShown = false;
        foreach (var row in result.Top)
        {
            worldRows.Add(WorldRow(row));
            meShown |= row.IsMe;
        }

        if (!meShown && result.Me.HasValue)
        {
            worldRows.Add(UiFactory.Text("•••", "world-gap"));
            worldRows.Add(WorldRow(result.Me.Value));
        }

        if (friendsOnly && result.Top.Count == 0) worldNote.text = Loc.T("No scores from your friends here yet.");
        else if (friendsOnly && !result.Me.HasValue) worldNote.text = Loc.T("Play to get on the board.");
        else if (friendsOnly) worldNote.text = string.Empty;
        else if (result.Top.Count == 0) worldNote.text = Loc.T("No scores yet. Be the first!");
        else if (!result.Me.HasValue) worldNote.text = board == OnlineScores.CoopBoard ? Loc.T("Play Co-op Rally online to get on the board.") : Loc.T("Play to get on the board.");
        else worldNote.text = string.Empty;
        worldNote.style.display = string.IsNullOrEmpty(worldNote.text) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private static VisualElement WorldRow(OnlineScores.Row row)
    {
        var element = new VisualElement { pickingMode = PickingMode.Ignore };
        element.AddToClassList("rank-row");
        element.EnableInClassList("rank-row--me", row.IsMe);

        var place = new Label(row.Rank.ToString()) { pickingMode = PickingMode.Ignore };
        place.AddToClassList("rank-row__place");
        place.EnableInClassList("rank-row__place--gold", row.Rank == 1);
        element.Add(place);

        string name = string.IsNullOrEmpty(row.Partner) ? row.Name : $"{row.Name} & {row.Partner}";
        var nameLabel = new Label(row.IsMe ? Loc.T("{0} (you)", name) : name) { pickingMode = PickingMode.Ignore };
        nameLabel.AddToClassList("rank-row__name");
        element.Add(nameLabel);

        var score = new Label(row.Score.ToString()) { pickingMode = PickingMode.Ignore };
        score.AddToClassList("rank-row__score");
        element.Add(score);
        return element;
    }

    private CosmeticItem FindStreakReward()
    {
        if (cosmetics == null) return null;
        foreach (var item in cosmetics.CatalogAsset.Items)
            if (item != null && item.Unlock == UnlockKind.Streak) return item;
        return null;
    }

    private static VisualElement RecordRow(string name, string score, string date)
    {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList("record-row");

        var nameLabel = new Label(name) { pickingMode = PickingMode.Ignore };
        nameLabel.AddToClassList("record-row__name");
        var scoreLabel = new Label(score) { pickingMode = PickingMode.Ignore };
        scoreLabel.AddToClassList("record-row__score");
        var dateLabel = new Label(date) { pickingMode = PickingMode.Ignore };
        dateLabel.AddToClassList("record-row__date");

        row.Add(nameLabel);
        row.Add(scoreLabel);
        row.Add(dateLabel);
        return row;
    }

    private static string FormatDate(string isoDay, bool hasRecord)
    {
        if (!hasRecord || string.IsNullOrEmpty(isoDay)) return hasRecord ? Loc.T("earlier") : "";
        if (!DateTime.TryParseExact(isoDay, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return "";

        var today = DateTime.Now.Date;
        if (date == today) return Loc.T("Today");
        if (date == today.AddDays(-1)) return Loc.T("Yesterday");
        return Loc.T("{0:d MMM}", date);
    }

    private static string FormatDuration(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        if (minutes < 60) return Loc.T("{0}m", minutes);
        return Loc.T("{0}h {1}m", minutes / 60, minutes % 60);
    }

    private static string BoardLabel(string board)
    {
        return board switch
        {
            OnlineScores.ClassicBoard => Loc.T("Classic"),
            OnlineScores.RushBoard => Loc.T("Rush"),
            OnlineScores.DailyBoard => Loc.T("Daily"),
            _ => Loc.T("Co-op")
        };
    }
}
