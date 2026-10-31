using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class AdventureResultScreen : UIScreen
{
    private const long StarDelayMs = 380;
    private const long StarStepMs = 260;

    private readonly VisualElement mascot;
    private readonly Label title;
    private readonly Label stage;
    private readonly List<VisualElement> stars = new List<VisualElement>();
    private readonly Label note;
    private readonly VisualElement goldRow;
    private readonly Label goldLabel;
    private readonly Button mapButton;
    private readonly Button retryButton;
    private readonly Button nextButton;
    private readonly VisualElement nextIcon;
    private readonly Label nextLabel;
    private readonly Action onRetry;
    private readonly Action onNext;

    private bool forward;

    public AdventureResultScreen(VisualElement root, Action onMap, Action onRetry, Action onNext) : base(root)
    {
        this.onRetry = onRetry;
        this.onNext = onNext;

        mascot = root.Q("adv-result-mascot");
        title = root.Q<Label>("adv-result-title");
        stage = root.Q<Label>("adv-result-stage");
        root.Q("adv-result-stars")?.Query(className: "adv-star").ForEach(stars.Add);
        note = root.Q<Label>("adv-result-note");
        goldRow = root.Q("adv-result-gold");
        goldLabel = root.Q<Label>("adv-result-gold-label");
        nextIcon = root.Q("adv-next-icon");
        nextLabel = root.Q<Label>("adv-next-label");

        mapButton = Bind("adv-map-button", onMap);
        retryButton = Bind("adv-retry-button", onRetry);
        nextButton = Bind("adv-next-button", () => (forward ? this.onNext : this.onRetry)?.Invoke());
    }

    public int StarsShown { get; private set; }
    public bool Forward => forward;
    public string TitleText => title != null ? title.text : null;

    public void Present(string stageLabel, bool won, int earned, int gold, int runGold, bool hasNext, string goal, Sprite happy, Sprite sad)
    {
        forward = won && hasNext;
        StarsShown = won ? earned : 0;

        UiFactory.SetPicture(mascot, won ? happy : sad);
        title.text = won ? Loc.T("Stage clear!") : Loc.T("So close!");
        stage.text = Loc.T("Stage {0}", stageLabel);
        note.text = won ? (earned >= AdventureMap.MaxStars ? Loc.T("Perfect! All 3 stars") : Loc.T("Keep more tries for more stars")) : goal;

        int total = gold + runGold;
        goldRow.style.display = total > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        goldLabel.text = "+" + total;

        retryButton.style.display = forward ? DisplayStyle.Flex : DisplayStyle.None;
        nextLabel.text = forward ? Loc.T("Next") : Loc.T("Play again");
        nextIcon.style.display = forward ? DisplayStyle.None : DisplayStyle.Flex;

        for (int i = 0; i < stars.Count; i++)
        {
            var star = stars[i];
            star.RemoveFromClassList("adv-star--on");
            if (i >= StarsShown) continue;
            Root.schedule.Execute(() => star.AddToClassList("adv-star--on")).StartingIn(StarDelayMs + i * StarStepMs);
        }

        SetInteractable(true);
    }

    public void SetInteractable(bool interactable)
    {
        mapButton?.SetEnabled(interactable);
        retryButton?.SetEnabled(interactable);
        nextButton?.SetEnabled(interactable);
    }
}
