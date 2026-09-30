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
        scoreCaption.text = string.IsNullOrEmpty(modeName) ? "Score" : $"{modeName} · Score";

        var face = isNewBest ? happyMascot : sadMascot;
        if (face != null) mascot.style.backgroundImage = new StyleBackground(face);

        title.text = isNewBest ? "Amazing!" : "So close!";
        finalScore.text = score.ToString();
        newBestBadge.style.display = isNewBest ? DisplayStyle.Flex : DisplayStyle.None;

        if (isNewBest)
            bestLine.text = previousBest > 0 ? $"Previous best {previousBest}" : "Your first record";
        else
            bestLine.text = $"Best {best}";
    }

    public void SetInteractable(bool interactable)
    {
        retryButton?.SetEnabled(interactable);
        homeButton?.SetEnabled(interactable);
    }
}
