using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class GameOverScreen : UIScreen
{
    private const float XpDelay = 0.35f;
    private const float XpSeconds = 0.9f;

    private readonly Label title;
    private readonly Label scoreCaption;
    private readonly Label finalScore;
    private readonly Label bestLine;
    private readonly VisualElement newBestBadge;
    private readonly VisualElement mascot;
    private readonly Button homeButton;
    private readonly Button retryButton;
    private readonly VisualElement goldRow;
    private readonly Label goldAmount;
    private readonly Button doubleButton;
    private readonly Label doubleLabel;
    private readonly VisualElement xpRow;
    private readonly Label xpLevel;
    private readonly Label xpText;
    private readonly VisualElement xpFill;
    private IVisualElementScheduledItem xpAnimation;

    public GameOverScreen(VisualElement root, Action onRetry, Action onHome, Action onDouble = null) : base(root)
    {
        title = root.Q<Label>("title");
        scoreCaption = root.Q<Label>("score-caption");
        finalScore = root.Q<Label>("final-score");
        bestLine = root.Q<Label>("best-line");
        newBestBadge = root.Q("new-best-badge");
        mascot = root.Q("mascot");

        retryButton = Bind("retry-button", onRetry);
        homeButton = Bind("home-button", onHome);

        goldRow = root.Q("gold-row");
        goldAmount = root.Q<Label>("gold-amount");
        doubleLabel = root.Q<Label>("double-label");
        doubleButton = Bind("double-button", onDouble);
        SetGold(0, false, false);

        xpRow = root.Q("xp-row");
        xpLevel = root.Q<Label>("xp-level");
        xpText = root.Q<Label>("xp-text");
        xpFill = root.Q("xp-fill");
        SetXp(0, default, default, null);
    }

    public bool GoldShown => goldRow != null && goldRow.style.display != DisplayStyle.None;
    public bool DoubleShown => doubleButton != null && doubleButton.style.display != DisplayStyle.None;
    public string GoldText => goldAmount != null ? goldAmount.text : null;
    public bool XpShown => xpRow != null && xpRow.style.display != DisplayStyle.None;
    public string XpText => xpText != null ? xpText.text : null;
    public string XpLevel => xpLevel != null ? xpLevel.text : null;

    public void SetXp(int gained, RoadStanding start, RoadStanding end, string levelUp)
    {
        if (xpRow == null) return;

        xpAnimation?.Pause();
        xpRow.style.display = gained > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (gained <= 0) return;

        bool up = end.Level > start.Level;
        xpRow.EnableInClassList("xp-row--up", up);
        xpText.text = up && !string.IsNullOrEmpty(levelUp) ? levelUp : $"+{gained} XP";
        xpLevel.text = start.Level.ToString();
        xpFill.style.width = Length.Percent(100f * start.Fraction);

        float began = Time.unscaledTime + XpDelay;
        bool done = false;
        xpAnimation = Root.schedule.Execute(() =>
        {
            float t = Mathf.Clamp01((Time.unscaledTime - began) / XpSeconds);
            float shown;
            if (!up) shown = Mathf.Lerp(start.Fraction, end.Fraction, t);
            else if (t < 0.5f) shown = Mathf.Lerp(start.Fraction, 1f, t * 2f);
            else
            {
                xpLevel.text = end.Level.ToString();
                shown = Mathf.Lerp(0f, end.Fraction, t * 2f - 1f);
            }

            xpFill.style.width = Length.Percent(100f * shown);
            done = t >= 1f;
        }).Every(16).Until(() => done);
    }

    public void SetGold(int amount, bool canDouble, bool waiting)
    {
        if (goldRow == null) return;

        goldRow.style.display = amount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        goldAmount.text = "+" + amount;
        doubleButton.style.display = canDouble ? DisplayStyle.Flex : DisplayStyle.None;
        doubleButton.SetEnabled(!waiting);
        doubleLabel.text = waiting ? Loc.T("Loading…") : Loc.T("2× gold");
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
