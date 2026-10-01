using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MatchHudScreen : UIScreen
{
    private const float ScoreGap = 40f;
    private const float TagGap = 36f;
    private const long PopMs = 60;

    private readonly VisualElement board;
    private readonly Action onPause;
    private readonly Dictionary<FieldSide, Label> scores = new Dictionary<FieldSide, Label>();
    private readonly Dictionary<FieldSide, VisualElement> tags = new Dictionary<FieldSide, VisualElement>();
    private readonly List<Label> passes = new List<Label>();

    private LocalMatchController match;
    private Func<Rect> fieldOnPanel;
    private Label count;
    private Button pause;
    private VisualElement teamLives;

    public MatchHudScreen(VisualElement root, Action onPause) : base(root)
    {
        this.onPause = onPause;
        board = root.Q("mhud");
    }

    public void Begin(LocalMatchController controller, Func<Rect> fieldRect)
    {
        match = controller;
        fieldOnPanel = fieldRect;

        board.Clear();
        scores.Clear();
        tags.Clear();
        passes.Clear();
        teamLives = null;

        var kind = match.Mode.Kind;
        foreach (var p in match.Participants)
        {
            if (kind == GameModeKind.TableDuel)
            {
                var label = UiFactory.Text("0", $"mhud-score player-text-{p.Index}");
                label.name = $"score-{p.Side.ToString().ToLowerInvariant()}";
                scores[p.Side] = label;
                board.Add(label);
            }
            else if (kind == GameModeKind.PartyTable)
            {
                var tag = UiFactory.Element("mhud-tag");
                tag.name = $"tag-{p.Side.ToString().ToLowerInvariant()}";
                tag.Add(UiFactory.Text(p.Name, $"mhud-tag__name player-text-{p.Index}"));
                var lives = UiFactory.Element("hub-art-row");
                tag.Add(lives);
                tags[p.Side] = tag;
                board.Add(tag);
            }
        }

        if (kind == GameModeKind.CoopRally)
        {
            for (int i = 0; i < 2; i++)
            {
                var label = UiFactory.Text("0", "mhud-passes");
                passes.Add(label);
                board.Add(label);
            }

            teamLives = UiFactory.Element("mhud-lives");
            teamLives.name = "team-lives";
            board.Add(teamLives);
        }

        pause = UiFactory.Button("btn mhud-pause", null, "icon--pause", onPause);
        pause.name = "pause-button";
        board.Add(pause);

        count = UiFactory.Text("", "mhud-count mhud-count--hidden");
        count.name = "countdown";
        board.Add(count);

        Refresh();
        Arrange();
    }

    public void ShowCount(int value)
    {
        if (count == null) return;

        if (value <= 0)
        {
            count.AddToClassList("mhud-count--hidden");
            return;
        }

        count.text = value.ToString();
        count.RemoveFromClassList("mhud-count--hidden");
        count.AddToClassList("mhud-count--pop");
        count.schedule.Execute(() => count.RemoveFromClassList("mhud-count--pop")).StartingIn(PopMs);
    }

    public void Refresh()
    {
        if (match == null || match.Mode == null || match.Rules == null) return;

        foreach (var p in match.Participants)
        {
            if (scores.TryGetValue(p.Side, out var score)) score.text = p.Score.ToString();

            if (tags.TryGetValue(p.Side, out var tag))
            {
                tag.EnableInClassList("mhud-tag--out", p.Eliminated);
                FillLives(tag[1], p.Lives, match.Mode.Lives);
            }
        }

        foreach (var label in passes) label.text = match.Rules.TeamScore.ToString();
        if (teamLives != null) FillLives(teamLives, match.Rules.TeamLives, match.Mode.Lives);
    }

    public void Arrange()
    {
        if (match == null || match.Mode == null || fieldOnPanel == null || pause == null) return;

        Rect f = fieldOnPanel();
        if (f.width <= 0f) return;

        float midY = f.center.y;
        bool party = match.Mode.Topology == FieldTopology.FourSides;

        if (party) Place(pause, f.xMax - 72f, f.yMax + TagGap + 40f, 0f);
        else Place(pause, f.xMax - 24f - 72f, midY, 0f);
        Place(count, f.center.x, party ? midY - 300f : midY - 260f, 0f);
        count.style.left = 0f;
        count.style.right = 0f;
        count.style.translate = new Translate(0f, Length.Percent(-50f));

        foreach (var pair in scores)
        {
            bool top = pair.Key == FieldSide.Top;
            Place(pair.Value, f.xMin + 24f + 150f, top ? midY - ScoreGap - 110f : midY + ScoreGap + 110f, top ? 180f : 0f);
        }

        for (int i = 0; i < passes.Count; i++)
        {
            bool top = i == 1;
            Place(passes[i], f.xMin + 24f + 180f, top ? midY - ScoreGap - 100f : midY + ScoreGap + 100f, top ? 180f : 0f);
        }

        if (teamLives != null) Place(teamLives, f.xMin + 24f + 90f, midY, 0f);

        foreach (var pair in tags)
        {
            switch (pair.Key)
            {
                case FieldSide.Top: Place(pair.Value, f.center.x, f.yMin - TagGap - 40f, 180f); break;
                case FieldSide.Left: Place(pair.Value, f.xMin - TagGap - 40f, midY, 90f); break;
                case FieldSide.Right: Place(pair.Value, f.xMax + TagGap + 40f, midY, -90f); break;
                default: Place(pair.Value, f.center.x, f.yMax + TagGap + 40f, 0f); break;
            }
        }
    }

    private static void Place(VisualElement element, float x, float y, float angle)
    {
        var s = element.style;
        s.position = Position.Absolute;
        s.left = x;
        s.top = y;
        s.translate = new Translate(Length.Percent(-50f), Length.Percent(-50f));
        s.rotate = new Rotate(angle);
    }

    private static void FillLives(VisualElement row, int lives, int max)
    {
        if (row.childCount != max)
        {
            row.Clear();
            for (int i = 0; i < max; i++) row.Add(UiFactory.Element("life"));
        }

        for (int i = 0; i < row.childCount; i++) row[i].EnableInClassList("life--lost", i >= lives);
    }
}
