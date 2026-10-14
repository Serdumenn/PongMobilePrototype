using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MissionsScreen : UIScreen
{
    private const string DoneClass = "mission--done";

    private readonly VisualElement mascot;
    private readonly Label note;
    private readonly VisualElement[] rows = new VisualElement[MissionBook.Count];
    private readonly VisualElement[] icons = new VisualElement[MissionBook.Count];
    private readonly Label[] texts = new Label[MissionBook.Count];
    private readonly VisualElement[] fills = new VisualElement[MissionBook.Count];
    private readonly Label[] counts = new Label[MissionBook.Count];
    private readonly VisualElement rewardArt;
    private readonly Label rewardTitle;
    private readonly VisualElement rewardFill;
    private readonly Label stars;

    public MissionsScreen(VisualElement root, Action onClose) : base(root)
    {
        mascot = root.Q("mascot");
        note = root.Q<Label>("missions-note");
        for (int i = 0; i < MissionBook.Count; i++)
        {
            rows[i] = root.Q($"mission-{i}");
            icons[i] = root.Q($"mission-{i}-icon");
            texts[i] = root.Q<Label>($"mission-{i}-text");
            fills[i] = root.Q($"mission-{i}-fill");
            counts[i] = root.Q<Label>($"mission-{i}-count");
        }

        rewardArt = root.Q("reward-art");
        rewardTitle = root.Q<Label>("reward-title");
        rewardFill = root.Q("reward-fill");
        stars = root.Q<Label>("stars-count");

        Bind("back-button", onClose);
        root.Q("missions-scrim")?.RegisterCallback<ClickEvent>(_ => onClose?.Invoke());
    }

    public void Present(DailyMissions missions, CosmeticsService cosmetics, Sprite face)
    {
        UiFactory.SetPicture(mascot, face);

        int done = 0;
        for (int i = 0; i < MissionBook.Count; i++)
        {
            bool shown = missions != null && i < missions.Count;
            rows[i].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown) continue;

            var mission = missions.Mission(i);
            int progress = Mathf.Min(missions.Progress(i), mission.Target);
            bool complete = missions.Done(i);
            if (complete) done++;

            rows[i].EnableInClassList(DoneClass, complete);
            SetIcon(icons[i], complete ? "check" : IconFor(mission.Kind));
            texts[i].text = Describe(mission);
            fills[i].style.width = Length.Percent(100f * progress / Mathf.Max(1, mission.Target));
            counts[i].text = $"{progress}/{mission.Target}";
        }

        note.text = done >= MissionBook.Count ? Loc.T("All done! New missions tomorrow") : Loc.T("New missions every day");
        ShowReward(cosmetics);
    }

    public static string Describe(MissionDef mission)
    {
        int n = mission.Target;
        return mission.Kind switch
        {
            MissionKind.ClassicHits => Loc.T("Make {0} hits in one Classic run", n),
            MissionKind.RushScore => Loc.T("Score {0} in one Rush run", n),
            MissionKind.Perfects => Loc.T("Make {0} Perfect hits", n),
            MissionKind.Hits => Loc.T("Make {0} hits", n),
            MissionKind.Streak => Loc.T("Hit {0} in a row", n),
            MissionKind.Runs => Loc.T("Play {0} runs", n),
            MissionKind.CloseSaves => Loc.T("Make {0} close saves", n),
            _ => Loc.T("Collect {0} power-ups", n)
        };
    }

    private static string IconFor(MissionKind kind)
    {
        return kind switch
        {
            MissionKind.ClassicHits => "crown",
            MissionKind.RushScore => "clock",
            MissionKind.Perfects => "star",
            MissionKind.Hits => "gamepad",
            MissionKind.Streak => "flame",
            MissionKind.Runs => "retry",
            MissionKind.CloseSaves => "shield",
            _ => "bolt"
        };
    }

    private static void SetIcon(VisualElement icon, string name)
    {
        var keep = new System.Collections.Generic.List<string>();
        foreach (var cls in icon.GetClasses())
            if (cls.StartsWith("icon--") && cls != "icon--light") keep.Add(cls);
        foreach (var cls in keep) icon.RemoveFromClassList(cls);
        icon.AddToClassList("icon--" + name);
    }

    private void ShowReward(CosmeticsService cosmetics)
    {
        int total = cosmetics != null ? cosmetics.Stars : 0;
        stars.text = total.ToString();

        var next = cosmetics != null ? cosmetics.NextStarReward() : null;
        if (next == null)
        {
            rewardTitle.text = Loc.T("All rewards collected!");
            rewardFill.style.width = Length.Percent(100f);
            UiFactory.SetPicture(rewardArt, null);
            return;
        }

        rewardTitle.text = Loc.T("Next reward: {0}", next.Title) + $"  {total}/{next.UnlockValue}";
        rewardFill.style.width = Length.Percent(100f * Mathf.Min(total, next.UnlockValue) / Mathf.Max(1, next.UnlockValue));
        UiFactory.SetPicture(rewardArt, next.Preview);
    }
}
