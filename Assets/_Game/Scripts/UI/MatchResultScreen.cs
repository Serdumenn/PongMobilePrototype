using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MatchResultScreen : UIScreen
{
    private const float MidHeight = 208f;

    private readonly VisualElement board;
    private readonly Action onRematch;
    private readonly Action onHome;

    private VisualElement top;
    private VisualElement bottom;
    private VisualElement middle;
    private Button rematch;
    private Button home;
    private bool split;

    public MatchResultScreen(VisualElement root, Action onRematch, Action onHome) : base(root)
    {
        this.onRematch = onRematch;
        this.onHome = onHome;

        board = root.Q("result-board");
        board.RegisterCallback<GeometryChangedEvent>(_ => Arrange());
    }

    public void Present(LocalMatchController match)
    {
        board.Clear();
        top = null;
        bottom = null;

        var mode = match.Mode;
        split = mode.Topology == FieldTopology.TopBottom;

        rematch = UiFactory.Button("btn btn--primary", "Rematch", "icon--retry icon--light", onRematch);
        rematch.name = "rematch-button";
        home = UiFactory.Button("btn btn--secondary", null, "icon--home", onHome, true);
        home.name = "home-button";

        if (split)
        {
            foreach (var p in match.Participants)
            {
                var half = mode.Kind == GameModeKind.CoopRally ? TeamHalf(match) : DuelHalf(match, p);
                half.name = $"result-{p.Side.ToString().ToLowerInvariant()}";
                if (p.Side == FieldSide.Top) top = half;
                else bottom = half;
                board.Add(half);
            }

            middle = UiFactory.Element("result-mid");
            middle.Add(rematch);
            middle.Add(home);
            board.Add(middle);
        }
        else
        {
            middle = PartyCard(match);
            board.Add(middle);
        }

        SetInteractable(true);
        Arrange();
        Show();
    }

    public void SetInteractable(bool interactable)
    {
        rematch?.SetEnabled(interactable);
        home?.SetEnabled(interactable);
    }

    private static VisualElement DuelHalf(LocalMatchController match, Participant p)
    {
        bool won = match.Rules.Winner == p;
        var half = UiFactory.Element("result-half");

        if (won)
        {
            var badge = UiFactory.Element("result-badge");
            badge.Add(UiFactory.Element("icon icon--crown"));
            badge.Add(UiFactory.Text("Winner", "result-badge__label"));
            half.Add(badge);
        }

        half.Add(UiFactory.Text(won ? "You win!" : "So close!", "result-title"));
        half.Add(UiFactory.Text(p.Score.ToString(), $"result-number player-text-{p.Index}"));
        half.Add(UiFactory.Picture(won ? p.Look?.Happy : p.Look?.Sad, "result-ball"));
        return half;
    }

    private static VisualElement TeamHalf(LocalMatchController match)
    {
        var half = UiFactory.Element("result-half");

        if (match.NewBestRally)
        {
            var badge = UiFactory.Element("result-badge");
            badge.Add(UiFactory.Element("icon icon--crown"));
            badge.Add(UiFactory.Text("New best!", "result-badge__label"));
            half.Add(badge);
        }

        half.Add(UiFactory.Text("Great teamwork!", "result-title"));
        half.Add(UiFactory.Text(match.Rules.TeamScore.ToString(), "result-number player-text-1"));
        half.Add(UiFactory.Text("passes together", "caption result-caption"));
        return half;
    }

    private VisualElement PartyCard(LocalMatchController match)
    {
        var winner = match.Rules.Winner;
        var card = UiFactory.Element("card");

        var mascot = UiFactory.Picture(winner?.Look?.Happy, "card__mascot");
        card.Add(mascot);
        card.Add(UiFactory.Text(winner != null ? $"{winner.Name} wins!" : "Game over", $"card__title player-text-{(winner != null ? winner.Index : 0)}"));
        card.Add(UiFactory.Text("Last one standing", "caption card__subtitle"));

        var actions = UiFactory.Element("card__actions");
        actions.Add(home);
        actions.Add(rematch);
        card.Add(actions);

        var wrap = UiFactory.Element("overlay");
        wrap.style.flexGrow = 1f;
        wrap.Add(card);
        return wrap;
    }

    private void Arrange()
    {
        if (!split || middle == null) return;

        Vector2 size = board.layout.size;
        if (float.IsNaN(size.y) || size.y <= 0f) return;

        float half = (size.y - MidHeight) / 2f;
        if (top != null)
        {
            top.style.top = 0f;
            top.style.height = half;
            top.style.rotate = new Rotate(180f);
        }

        if (bottom != null)
        {
            bottom.style.top = half + MidHeight;
            bottom.style.height = half;
        }

        middle.style.top = half;
    }
}
