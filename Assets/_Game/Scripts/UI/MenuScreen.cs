using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MenuScreen : UIScreen
{
    private const float SwipeThreshold = 90f;

    private readonly VisualElement modeCard;
    private readonly VisualElement modeArt;
    private readonly Label modeName;
    private readonly Label modeTagline;
    private readonly VisualElement modeBestIcon;
    private readonly Label modeBestLabel;
    private readonly VisualElement modeDots;
    private readonly Button playButton;
    private readonly Action<int> onStepMode;

    private Vector2 swipeStart;
    private bool swiping;

    public MenuScreen(VisualElement root, Action onPlay, Action onSettings, Action onShop, Action onScores, Action<int> onStepMode) : base(root)
    {
        this.onStepMode = onStepMode;

        modeCard = root.Q("mode-card");
        modeArt = root.Q("mode-art");
        modeName = root.Q<Label>("mode-name");
        modeTagline = root.Q<Label>("mode-tagline");
        modeBestIcon = root.Q("mode-best-icon");
        modeBestLabel = root.Q<Label>("mode-best-label");
        modeDots = root.Q("mode-dots");

        playButton = Bind("play-button", onPlay);
        Bind("settings-button", onSettings);
        Bind("shop-button", onShop);
        Bind("scores-button", onScores);
        Bind("mode-prev", () => onStepMode?.Invoke(-1));
        Bind("mode-next", () => onStepMode?.Invoke(1));

        modeCard.RegisterCallback<PointerDownEvent>(e =>
        {
            swipeStart = e.position;
            swiping = true;
        });
        modeCard.RegisterCallback<PointerUpEvent>(e => EndSwipe(e.position));
        modeCard.RegisterCallback<PointerLeaveEvent>(e => EndSwipe(e.position));
    }

    public void SetTogether(int index, int count, Sprite left, Sprite right)
    {
        modeName.text = Loc.T("Together");
        modeTagline.text = Loc.T("Play with your friends");
        modeCard.EnableInClassList("mode-card--locked", false);
        playButton?.SetEnabled(true);

        modeArt.Clear();
        modeArt.EnableInClassList("mode-card__art--icon", false);
        modeArt.style.backgroundImage = new StyleBackground(StyleKeyword.None);

        var duo = new VisualElement { pickingMode = PickingMode.Ignore };
        duo.AddToClassList("mode-card__duo");
        foreach (var sprite in new[] { left, right })
        {
            var ball = new VisualElement { pickingMode = PickingMode.Ignore };
            ball.AddToClassList("mode-card__duo-ball");
            if (sprite != null) ball.style.backgroundImage = new StyleBackground(sprite);
            duo.Add(ball);
        }
        modeArt.Add(duo);

        modeBestIcon.EnableInClassList("icon--crown", false);
        modeBestIcon.EnableInClassList("icon--lock", false);
        modeBestIcon.EnableInClassList("icon--people", true);
        modeBestLabel.text = Loc.T("2–4 players");

        SetDots(index, count);
    }

    public void SetMode(GameModeDefinition mode, int index, int count, int best, bool unlocked, Sprite art)
    {
        if (mode == null) return;

        modeBestIcon.EnableInClassList("icon--people", false);

        modeName.text = mode.Title;
        modeCard.EnableInClassList("mode-card--locked", !unlocked);
        playButton?.SetEnabled(unlocked);

        modeArt.Clear();
        bool iconArt = art == null;
        modeArt.EnableInClassList("mode-card__art--icon", iconArt);
        modeArt.style.backgroundImage = iconArt ? new StyleBackground(StyleKeyword.None) : new StyleBackground(art);
        if (iconArt)
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("icon");
            glyph.AddToClassList(mode.Kind == GameModeKind.Rush ? "icon--clock" : "icon--play");
            modeArt.Add(glyph);
        }

        if (unlocked)
        {
            modeTagline.text = mode.Blurb;
            modeBestIcon.EnableInClassList("icon--crown", true);
            modeBestIcon.EnableInClassList("icon--lock", false);
            modeBestLabel.text = best > 0 ? Loc.T("Best {0}", best) : Loc.T("No record yet");
        }
        else
        {
            modeTagline.text = mode.Blurb;
            modeBestIcon.EnableInClassList("icon--crown", false);
            modeBestIcon.EnableInClassList("icon--lock", true);
            modeBestLabel.text = Loc.T("Classic best {0} to unlock", mode.RequiredClassicBest);
        }

        SetDots(index, count);
    }

    private void SetDots(int index, int count)
    {
        modeDots.Clear();
        for (int i = 0; i < count; i++)
        {
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("mode-dot");
            dot.EnableInClassList("mode-dot--active", i == index);
            modeDots.Add(dot);
        }
    }

    private void EndSwipe(Vector2 position)
    {
        if (!swiping) return;
        swiping = false;

        float delta = position.x - swipeStart.x;
        if (Mathf.Abs(delta) < SwipeThreshold) return;

        UiFeedback.Tap();
        onStepMode?.Invoke(delta < 0f ? 1 : -1);
    }
}
