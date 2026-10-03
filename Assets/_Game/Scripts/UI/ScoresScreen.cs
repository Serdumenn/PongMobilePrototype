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
    private readonly VisualElement worldModes;
    private readonly Label worldText;
    private readonly Label worldBest;
    private readonly ScrollView scroll;

    private GameModeDefinition worldMode;

    public ScoresScreen(VisualElement root, SoloGameManager game, CosmeticsService cosmetics, Action onBack) : base(root)
    {
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
        worldText = root.Q<Label>("world-text");
        worldBest = root.Q<Label>("world-best");
        scroll = root.Q<ScrollView>("scores-scroll");

        Bind("back-button", onBack);
        tabMe = Bind("tab-me", () => SelectTab(false));
        tabWorld = Bind("tab-world", () => SelectTab(true));
    }

    protected override void OnShow()
    {
        scroll.scrollOffset = Vector2.zero;
        worldMode = game.CurrentMode;
        SelectTab(false);
    }

    private void SelectTab(bool world)
    {
        tabMe.EnableInClassList(ActiveTabClass, !world);
        tabWorld.EnableInClassList(ActiveTabClass, world);
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
        statGames.text = data.GamesPlayed.ToString("N0", CultureInfo.InvariantCulture);
        statHits.text = data.TotalHits.ToString("N0", CultureInfo.InvariantCulture);
        statStreak.text = data.LongestStreak.ToString();
        statTime.text = FormatDuration(data.PlayTimeSeconds);

        records.Clear();
        foreach (var mode in game.ModeList)
        {
            if (mode == null) continue;
            int best = game.BestFor(mode);
            records.Add(RecordRow(mode.DisplayName, best > 0 ? best.ToString() : "-", FormatDate(data.BestDate(mode.Id), best > 0)));
        }

        matchRecords.Clear();
        matchRecords.Add(RecordRow("Matches", data.MatchesPlayed.ToString("N0", CultureInfo.InvariantCulture), ""));
        matchRecords.Add(RecordRow("Best co-op rally", data.BestCoopRally > 0 ? data.BestCoopRally.ToString() : "-", ""));

        int streak = source.DayStreak;
        streakTitle.text = streak > 0 ? $"{streak}-day streak" : "Daily streak";

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
        else if (rewardOwned) streakNote.text = $"{reward.DisplayName} unlocked. Keep the streak going!";
        else streakNote.text = source.Records.PlayedToday(DateTime.Now)
            ? $"Day {StreakGoal}: {reward.DisplayName}. Come back tomorrow!"
            : $"Day {StreakGoal}: {reward.DisplayName}. Play today to keep it!";
    }

    private void RefreshWorld()
    {
        worldModes.Clear();
        foreach (var mode in game.ModeList)
        {
            if (mode == null) continue;
            var chip = new Button { text = mode.DisplayName, focusable = false };
            chip.RemoveFromClassList(Button.ussClassName);
            chip.AddToClassList("mode-chip");
            chip.EnableInClassList("mode-chip--active", mode == worldMode);
            var captured = mode;
            chip.clicked += () =>
            {
                UiFeedback.Tap();
                worldMode = captured;
                RefreshWorld();
            };
            worldModes.Add(chip);
        }

        worldText.text = "Coming soon with Google Play Games.\nYour bests will sync automatically.";
        int best = game.BestFor(worldMode);
        worldBest.text = best > 0 ? $"Your {worldMode.DisplayName} best: {best}" : $"Play {worldMode.DisplayName} to set a score";
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
        if (!hasRecord || string.IsNullOrEmpty(isoDay)) return hasRecord ? "earlier" : "";
        if (!DateTime.TryParseExact(isoDay, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return "";

        var today = DateTime.Now.Date;
        if (date == today) return "Today";
        if (date == today.AddDays(-1)) return "Yesterday";
        return date.ToString("d MMM", CultureInfo.InvariantCulture);
    }

    private static string FormatDuration(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        if (minutes < 60) return $"{minutes}m";
        return $"{minutes / 60}h {minutes % 60}m";
    }
}
