using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class OnlineResultScreen : UIScreen
{
    private readonly CosmeticsService cosmetics;
    private readonly VisualElement mascot;
    private readonly Label title;
    private readonly Label caption;
    private readonly Label score;
    private readonly Label note;
    private readonly Button rematchButton;
    private readonly Label rematchLabel;
    private readonly Button homeButton;

    private OnlineMatchController match;
    private bool opponentGone;

    public OnlineResultScreen(VisualElement root, CosmeticsService cosmetics, Action onRematch, Action onLeave) : base(root)
    {
        this.cosmetics = cosmetics;

        mascot = root.Q("mascot");
        title = root.Q<Label>("title");
        caption = root.Q<Label>("caption");
        score = root.Q<Label>("score");
        note = root.Q<Label>("note");
        rematchLabel = root.Q<Label>("rematch-label");

        rematchButton = Bind("rematch-button", onRematch);
        homeButton = Bind("home-button", onLeave);
    }

    public void Present(OnlineMatchController controller)
    {
        if (match != controller)
        {
            if (match != null) match.RematchChanged -= Refresh;
            match = controller;
            match.RematchChanged += Refresh;
        }

        opponentGone = false;
        homeButton.SetEnabled(true);
        Refresh();
        Show();
    }

    public void SetOpponentGone()
    {
        opponentGone = true;
        Refresh();
    }

    private void Refresh()
    {
        if (match == null || match.Rules == null) return;

        var rules = match.Rules;
        string mode = match.Mode != null ? match.Mode.DisplayName : "Online";
        bool happy = rules.Coop ? rules.Passes > 0 : match.Won;

        var look = cosmetics != null ? cosmetics.CatalogAsset.Find(match.MyLook) : null;
        if (look == null && cosmetics != null) look = cosmetics.Equipped(CosmeticCategory.Ball);
        if (look != null) UiFactory.SetPicture(mascot, happy ? look.Happy : look.Sad);

        if (rules.Coop)
        {
            title.text = rules.Passes >= 10 ? "Great teamwork!" : "Nice try!";
            caption.text = $"Passes with {match.OpponentName}";
            score.text = rules.Passes.ToString();
        }
        else
        {
            title.text = match.Won ? "You win!" : "So close!";
            caption.text = $"{mode} · vs {match.OpponentName}";
            score.text = $"{match.MyScore} – {match.OpponentScore}";
        }

        bool forfeit = !rules.Coop && Mathf.Max(rules.HostScore, rules.GuestScore) < rules.PointsToWin;
        if (opponentGone || (forfeit && match.Won)) note.text = $"{match.OpponentName} left the match.";
        else if (match.WantsRematch) note.text = $"Waiting for {match.OpponentName}…";
        else if (match.OpponentWantsRematch) note.text = $"{match.OpponentName} wants a rematch!";
        else note.text = string.Empty;

        bool canRematch = !opponentGone && !(forfeit && match.Won) && !match.WantsRematch;
        rematchButton.SetEnabled(canRematch);
        rematchLabel.text = match.OpponentWantsRematch && !match.WantsRematch ? "Accept" : "Rematch";
    }
}
