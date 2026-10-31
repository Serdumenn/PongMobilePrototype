using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RoadScreen : UIScreen
{
    private const int Ahead = 10;

    private readonly VisualElement mascot;
    private readonly Label level;
    private readonly Label title;
    private readonly VisualElement fill;
    private readonly Label xp;
    private readonly ScrollView list;

    private VisualElement focus;

    public RoadScreen(VisualElement root, Action onClose) : base(root)
    {
        mascot = root.Q("road-mascot");
        level = root.Q<Label>("road-level");
        title = root.Q<Label>("road-title");
        fill = root.Q("road-fill");
        xp = root.Q<Label>("road-xp");
        list = root.Q<ScrollView>("road-list");

        Bind("road-back-button", onClose);
        root.Q("road-scrim")?.RegisterCallback<ClickEvent>(_ => onClose?.Invoke());
    }

    public int Rows => list != null ? list.contentContainer.childCount : 0;

    public void Present(PlayerLevel player, Sprite face)
    {
        UiFactory.SetPicture(mascot, face);
        if (player == null || player.RoadAsset == null) return;

        var road = player.RoadAsset;
        var standing = player.Standing;
        level.text = standing.Level.ToString();
        title.text = Loc.T("Level {0}", standing.Level);
        fill.style.width = Length.Percent(100f * standing.Fraction);
        xp.text = $"{standing.Into}/{standing.Need} XP";

        list.Clear();
        focus = null;
        int last = Mathf.Max(road.CapLevel, standing.Level + Ahead);
        for (int step = 2; step <= last; step++)
        {
            var row = BuildRow(road.RewardFor(step), standing.Level);
            list.Add(row);
            if (step != standing.Level + 1) continue;

            focus = row;
            row.RegisterCallback<GeometryChangedEvent>(OnFocusLaid);
        }
    }

    private void OnFocusLaid(GeometryChangedEvent e)
    {
        (e.target as VisualElement)?.UnregisterCallback<GeometryChangedEvent>(OnFocusLaid);
        ScrollToFocus();
    }

    private static VisualElement BuildRow(RoadReward reward, int current)
    {
        bool done = reward.Level <= current;
        var row = UiFactory.Element("road-step");
        row.EnableInClassList("road-step--done", done);
        row.EnableInClassList("road-step--next", reward.Level == current + 1);
        row.EnableInClassList("road-step--item", reward.Item != null);

        var node = UiFactory.Element("road-step__node");
        node.Add(UiFactory.Text(reward.Level.ToString(), "road-step__level fit-skip"));
        row.Add(node);

        row.Add(BuildArt(reward));

        var body = UiFactory.Element("road-step__body");
        body.Add(UiFactory.Text(Describe(reward), "road-step__text"));
        if (reward.Item != null || reward.Stars > 0) body.Add(UiFactory.Text(Loc.T("+{0} gold", reward.Gold), "road-step__extra"));
        row.Add(body);

        if (done) row.Add(UiFactory.Element("icon icon--check road-step__state"));
        return row;
    }

    private static VisualElement BuildArt(RoadReward reward)
    {
        var art = UiFactory.Element("road-step__art");
        if (reward.Item == null)
        {
            art.Add(UiFactory.Element("icon " + (reward.Stars > 0 ? "icon--star" : "icon--coin")));
            art.AddToClassList(reward.Stars > 0 ? "road-step__art--star" : "road-step__art--coin");
            return art;
        }

        art.AddToClassList("road-step__art--item");
        if (reward.Item.Category == CosmeticCategory.Theme)
        {
            var swatch = UiFactory.Element("road-step__swatch");
            swatch.style.backgroundColor = reward.Item.BackgroundColor;
            swatch.style.borderTopColor = swatch.style.borderBottomColor = reward.Item.BlobPrimary;
            swatch.style.borderLeftColor = swatch.style.borderRightColor = reward.Item.BlobSecondary;
            art.Add(swatch);
        }
        else
        {
            var picture = UiFactory.Picture(reward.Item.Preview, "road-step__picture");
            picture.EnableInClassList("road-step__picture--paddle", reward.Item.Category == CosmeticCategory.Paddle);
            art.Add(picture);
        }
        return art;
    }

    public static string Describe(RoadReward reward)
    {
        if (reward.Item != null) return reward.Item.Title;
        if (reward.Stars > 0) return Loc.Plural("{0} mission star", "{0} mission stars", reward.Stars);
        return Loc.T("{0} gold", reward.Gold);
    }

    private void ScrollToFocus()
    {
        if (focus == null || list == null || float.IsNaN(focus.layout.height) || focus.layout.height <= 0f) return;
        float gap = focus.resolvedStyle.marginTop;
        list.scrollOffset = new Vector2(0f, Mathf.Max(0f, focus.layout.y - focus.layout.height - gap * 2f));
    }
}
