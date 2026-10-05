using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GameOverScreen : UIScreen
{
    private readonly Label title;
    private readonly Label scoreCaption;
    private readonly Label finalScore;
    private readonly Label bestLine;
    private readonly VisualElement newBestBadge;
    private readonly VisualElement mascot;
    private readonly Button homeButton;
    private readonly Button retryButton;

    public GameOverScreen(VisualElement root, Action onRetry, Action onHome) : base(root)
    {
        title = root.Q<Label>("title");
        scoreCaption = root.Q<Label>("score-caption");
        finalScore = root.Q<Label>("final-score");
        bestLine = root.Q<Label>("best-line");
        newBestBadge = root.Q("new-best-badge");
        mascot = root.Q("mascot");

        retryButton = Bind("retry-button", onRetry);
        homeButton = Bind("home-button", onHome);
    }

    public void SetResult(string modeName, int score, int best, int previousBest, bool isNewBest, Sprite happyMascot, Sprite sadMascot)
    {
        scoreCaption.text = string.IsNullOrEmpty(modeName) ? Loc.T("Score") : Loc.T("{0} · Score", modeName);

        var face = isNewBest ? happyMascot : sadMascot;
        if (face != null) mascot.style.backgroundImage = new StyleBackground(face);

        title.text = isNewBest ? Loc.T("Amazing!") : Loc.T("So close!");
        finalScore.text = score.ToString();
        newBestBadge.style.display = isNewBest ? DisplayStyle.Flex : DisplayStyle.None;

        if (isNewBest)
            bestLine.text = previousBest > 0 ? Loc.T("Previous best {0}", previousBest) : Loc.T("Your first record");
        else
            bestLine.text = Loc.T("Best {0}", best);
    }

    public void SetDaily(string dateLabel, int score, int bestToday, bool isNewBest, Sprite happyMascot, Sprite sadMascot)
    {
        scoreCaption.text = Loc.T("Daily Challenge · {0}", dateLabel);

        var face = isNewBest ? happyMascot : sadMascot;
        if (face != null) mascot.style.backgroundImage = new StyleBackground(face);

        title.text = isNewBest ? Loc.T("New best today!") : Loc.T("So close!");
        finalScore.text = score.ToString();
        newBestBadge.style.display = DisplayStyle.None;
        bestLine.text = Loc.T("Today's best {0}", bestToday);
    }

    public void SetBestLine(string text)
    {
        bestLine.text = text;
    }

    public void SetInteractable(bool interactable)
    {
        retryButton?.SetEnabled(interactable);
        homeButton?.SetEnabled(interactable);
    }
}
