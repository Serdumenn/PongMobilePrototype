using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class AdventureScreen : UIScreen
{
    private const string ActiveTabClass = "tab--active";
    private const string DialogHiddenClass = "adv-dialog--hidden";
    private static readonly float[] Lanes = { 0.3f, 0.5f, 0.7f, 0.5f };

    private readonly Action<int> onPlay;
    private readonly Label stars;
    private readonly Button[] tabs = new Button[3];
    private readonly ScrollView scroll;
    private readonly VisualElement worldBadge;
    private readonly VisualElement worldIcon;
    private readonly Label worldName;
    private readonly Label worldNote;
    private readonly Label worldStars;
    private readonly VisualElement path;
    private readonly VisualElement dialog;
    private readonly VisualElement dialogBadge;
    private readonly VisualElement dialogIcon;
    private readonly Label dialogTitle;
    private readonly Label dialogGoal;
    private readonly VisualElement dialogTags;
    private readonly List<VisualElement> dialogStars = new List<VisualElement>();
    private readonly Label dialogReward;

    private AdventureRun run;
    private VisualElement focus;
    private string worldIconClass;
    private string dialogIconClass;

    public AdventureScreen(VisualElement root, Action onBack, Action<int> onPlay) : base(root)
    {
        this.onPlay = onPlay;

        stars = root.Q<Label>("adv-stars");
        for (int i = 0; i < tabs.Length; i++)
        {
            int world = i;
            tabs[i] = Bind($"adv-world-{i}", () => SelectWorld(world));
        }

        scroll = root.Q<ScrollView>("adv-scroll");
        worldBadge = root.Q("adv-world-badge");
        worldIcon = root.Q("adv-world-icon");
        worldName = root.Q<Label>("adv-world-name");
        worldNote = root.Q<Label>("adv-world-note");
        worldStars = root.Q<Label>("adv-world-stars");
        path = root.Q("adv-path");

        dialog = root.Q("adv-dialog");
        dialogBadge = root.Q("adv-dialog-badge");
        dialogIcon = root.Q("adv-dialog-icon");
        dialogTitle = root.Q<Label>("adv-dialog-title");
        dialogGoal = root.Q<Label>("adv-dialog-goal");
        dialogTags = root.Q("adv-dialog-tags");
        root.Q("adv-dialog-stars")?.Query(className: "adv-star").ForEach(dialogStars.Add);
        dialogReward = root.Q<Label>("adv-dialog-reward");

        Bind("adv-back-button", onBack);
        Bind("adv-dialog-close", CloseDialog);
        Bind("adv-dialog-play", PlaySelected);
        root.Q("adv-dialog-scrim")?.RegisterCallback<ClickEvent>(_ => CloseDialog());
    }

    public int World { get; private set; }
    public int Selected { get; private set; } = -1;
    public bool DialogOpen => dialog != null && !dialog.ClassListContains(DialogHiddenClass);
    public int Nodes => path != null ? path.childCount : 0;

    public void Present(AdventureRun adventure, int world)
    {
        run = adventure;
        CloseDialog();
        if (run == null || run.BookAsset == null) return;

        stars.text = $"{run.TotalStars}/{run.BookAsset.MaxStars}";
        for (int i = 0; i < tabs.Length; i++)
        {
            bool exists = i < run.BookAsset.Worlds.Count;
            tabs[i].style.display = exists ? DisplayStyle.Flex : DisplayStyle.None;
            if (exists) tabs[i].text = run.BookAsset.Worlds[i].Title;
        }

        SelectWorld(Mathf.Clamp(world, 0, run.BookAsset.Worlds.Count - 1));
    }

    public void SelectWorld(int world)
    {
        if (run == null || run.BookAsset == null || world < 0 || world >= run.BookAsset.Worlds.Count) return;

        World = world;
        for (int i = 0; i < tabs.Length; i++) tabs[i].EnableInClassList(ActiveTabClass, i == world);

        var book = run.BookAsset;
        var info = book.Worlds[world];
        int first = book.FirstOf(world);
        bool open = run.IsOpen(first);

        worldBadge.style.backgroundColor = info.Tint;
        SetIcon(worldIcon, ref worldIconClass, info.Icon);
        worldName.text = info.Title;
        worldNote.text = open ? Loc.T("{0} stages", info.Levels.Count) : Loc.T("Finish {0} to open", world > 0 ? book.Worlds[world - 1].Title : "");

        int earned = 0;
        for (int i = 0; i < info.Levels.Count; i++) earned += run.StarsAt(first + i);
        worldStars.text = $"{earned}/{info.Levels.Count * AdventureMap.MaxStars}";

        path.Clear();
        focus = null;
        int next = run.NextIndex;
        for (int i = 0; i < info.Levels.Count; i++)
        {
            int index = first + i;
            var row = BuildNode(index, i, info.Tint, index == next);
            path.Add(row);
            if (index != next) continue;

            focus = row;
            row.RegisterCallback<GeometryChangedEvent>(OnFocusLaid);
        }

        if (focus == null) scroll.scrollOffset = Vector2.zero;
    }

    private VisualElement BuildNode(int index, int slot, Color tint, bool next)
    {
        var book = run.BookAsset;
        int earned = run.StarsAt(index);
        bool open = run.IsOpen(index);
        bool boss = book.IsBoss(index);

        var row = UiFactory.Element("adv-row");
        var node = new Button(() => OpenLevel(index)) { focusable = false };
        node.RemoveFromClassList(Button.ussClassName);
        node.AddToClassList("adv-node");
        node.EnableInClassList("adv-node--done", earned > 0);
        node.EnableInClassList("adv-node--next", open && earned <= 0);
        node.EnableInClassList("adv-node--locked", !open);
        node.EnableInClassList("adv-node--boss", boss);
        node.name = $"adv-node-{index}";
        node.style.left = Length.Percent(Lanes[slot % Lanes.Length] * 100f);
        if (earned > 0)
        {
            node.style.backgroundColor = tint;
            node.style.borderBottomColor = Color.Lerp(tint, Color.black, 0.25f);
        }

        var face = UiFactory.Element("adv-node__face");
        if (!open) face.Add(UiFactory.Element("icon icon--lock adv-node__lock"));
        else
        {
            if (boss) face.Add(UiFactory.Element("icon icon--crown adv-node__crown"));
            face.Add(UiFactory.Text((slot + 1).ToString(), "adv-node__number fit-skip"));
        }
        node.Add(face);

        var row3 = UiFactory.Element("adv-node__stars");
        for (int s = 0; s < AdventureMap.MaxStars; s++)
        {
            var star = UiFactory.Element("icon icon--star-fill adv-node__star");
            star.EnableInClassList("adv-node__star--on", s < earned);
            row3.Add(star);
        }
        node.Add(row3);

        row.Add(node);
        return row;
    }

    private void OnFocusLaid(GeometryChangedEvent e)
    {
        (e.target as VisualElement)?.UnregisterCallback<GeometryChangedEvent>(OnFocusLaid);
        if (focus == null || scroll == null) return;
        float y = scroll.contentContainer.WorldToLocal(focus.worldBound.center).y;
        float view = scroll.contentViewport.layout.height;
        scroll.scrollOffset = new Vector2(0f, y < view * 0.75f ? 0f : y - view * 0.45f);
    }

    public void OpenLevel(int index)
    {
        if (run == null || !run.IsOpen(index)) return;

        var book = run.BookAsset;
        var level = book.Level(index);
        Selected = index;

        dialogBadge.style.backgroundColor = book.Worlds[book.WorldOf(index)].Tint;
        SetIcon(dialogIcon, ref dialogIconClass, GoalIcon(level.Goal));
        dialogTitle.text = Loc.T("Stage {0}", book.Label(index));
        dialogGoal.text = Goal(level);

        dialogTags.Clear();
        foreach (var tag in Tags(level)) dialogTags.Add(Tag(tag.icon, tag.text));

        int earned = run.StarsAt(index);
        for (int i = 0; i < dialogStars.Count; i++) dialogStars[i].EnableInClassList("adv-star--on", i < earned);
        dialogReward.text = earned > 0 ? Loc.T("Best: {0} of 3 stars", earned) : Loc.T("First clear: +{0} gold", level.Gold);

        dialog.RemoveFromClassList(DialogHiddenClass);
    }

    public void CloseDialog()
    {
        dialog?.AddToClassList(DialogHiddenClass);
    }

    private void PlaySelected()
    {
        if (Selected < 0) return;
        CloseDialog();
        onPlay?.Invoke(Selected);
    }

    public static string Goal(AdventureLevel level)
    {
        int n = level.Goals;
        return level.Goal switch
        {
            AdventureGoal.Targets => Loc.T("Hit all {0} targets", n),
            AdventureGoal.Coins => Loc.T("Grab {0} coins", n),
            AdventureGoal.Survive => Loc.T("Survive {0} seconds", n),
            AdventureGoal.Perfects => Loc.T("Make {0} Perfect hits", n),
            _ => Loc.T("Make {0} hits", n)
        };
    }

    public static string GoalIcon(AdventureGoal goal)
    {
        return goal switch
        {
            AdventureGoal.Targets => "target",
            AdventureGoal.Coins => "coin",
            AdventureGoal.Survive => "clock",
            AdventureGoal.Perfects => "star",
            _ => "gamepad"
        };
    }

    private static IEnumerable<(string icon, string text)> Tags(AdventureLevel level)
    {
        if (level.Goal != AdventureGoal.Survive && level.TimeLimit > 0f) yield return ("clock", Loc.T("{0} sec", Mathf.RoundToInt(level.TimeLimit)));
        if (level.Speed > 1.05f) yield return ("fast-ball", Loc.T("Fast ball"));
        if (level.BallScale > 1.05f) yield return ("giant", Loc.T("Giant ball"));
        if (level.Bumpers.Count > 0) yield return ("shield", level.BumperSway > 0f ? Loc.T("Moving bumpers") : Loc.T("Bumpers"));
        if (level.Goal == AdventureGoal.Targets && level.TargetSway > 0f) yield return ("target", Loc.T("Moving targets"));
    }

    private static VisualElement Tag(string icon, string text)
    {
        var tag = UiFactory.Element("adv-tag");
        tag.Add(UiFactory.Element("icon icon--" + icon));
        tag.Add(UiFactory.Text(text, "adv-tag__label"));
        return tag;
    }

    private static void SetIcon(VisualElement icon, ref string current, string name)
    {
        if (icon == null) return;
        if (!string.IsNullOrEmpty(current)) icon.RemoveFromClassList(current);
        current = "icon--" + name;
        icon.AddToClassList(current);
    }
}
