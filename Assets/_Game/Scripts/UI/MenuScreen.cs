using System;
using UnityEngine;
using UnityEngine.UIElements;

public readonly struct ModeCardData
{
    public readonly GameModeDefinition Mode;
    public readonly bool Together;
    public readonly int Best;
    public readonly bool Unlocked;
    public readonly Sprite Art;
    public readonly string Note;
    public readonly Sprite Left;
    public readonly Sprite Right;

    public ModeCardData(GameModeDefinition mode, int best, bool unlocked, Sprite art, string note)
    {
        Mode = mode;
        Together = false;
        Best = best;
        Unlocked = unlocked;
        Art = art;
        Note = note;
        Left = null;
        Right = null;
    }

    public ModeCardData(Sprite left, Sprite right)
    {
        Mode = null;
        Together = true;
        Best = 0;
        Unlocked = true;
        Art = null;
        Note = null;
        Left = left;
        Right = right;
    }
}

public sealed class MenuScreen : UIScreen
{
    private const string HintHiddenClass = "swipe-hint--hidden";
    private const float Spacing = 690f;
    private const float SnapSeconds = 0.5f;
    private const float HintNudge = 28f;
    private const long FrameMs = 16;
    private const float StillSeconds = 0.08f;

    private readonly VisualElement strip;
    private readonly VisualElement track;
    private readonly VisualElement modeCard;
    private readonly VisualElement prevCard;
    private readonly VisualElement nextCard;
    private readonly VisualElement hint;
    private readonly VisualElement modeDots;
    private readonly Button playButton;
    private readonly Label missionsCount;
    private readonly Label levelNumber;
    private readonly VisualElement levelFill;
    private readonly Action<int> onStepMode;

    private bool pressing;
    private bool dragging;
    private float pressX;
    private float offset;
    private float lastX;
    private float lastTime;
    private float velocity;
    private float snapFrom;
    private float snapStart;
    private IVisualElementScheduledItem snap;
    private IVisualElementScheduledItem nudge;

    public MenuScreen(VisualElement root, Action onPlay, Action onSettings, Action onShop, Action onScores, Action<int> onStepMode, Action onMissions = null, Action onLevel = null) : base(root)
    {
        this.onStepMode = onStepMode;

        strip = root.Q("mode-strip");
        track = root.Q("mode-track");
        modeCard = root.Q("mode-card");
        prevCard = root.Q("mode-card-prev");
        nextCard = root.Q("mode-card-next");
        hint = root.Q("swipe-hint");
        modeDots = root.Q("mode-dots");

        playButton = Bind("play-button", onPlay);
        Bind("settings-button", onSettings);
        Bind("shop-button", onShop);
        Bind("scores-button", onScores);
        Bind("missions-button", onMissions);
        missionsCount = root.Q<Label>("missions-count");
        Bind("level-button", onLevel);
        levelNumber = root.Q<Label>("level-number");
        levelFill = root.Q("level-fill");

        strip.RegisterCallback<PointerDownEvent>(OnPointerDown);
        strip.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        strip.RegisterCallback<PointerUpEvent>(OnPointerUp);
        strip.RegisterCallback<PointerCaptureOutEvent>(_ => Cancel());
        ShowHint(!GameSettings.SwipeHintDone);
    }

    public bool Dragging => dragging;
    public bool HintShown => hint != null && !hint.ClassListContains(HintHiddenClass);

    public void SetMissions(int done, int total)
    {
        if (missionsCount == null) return;

        missionsCount.style.display = total > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        missionsCount.text = $"{done}/{total}";
        missionsCount.EnableInClassList("missions-button__count--done", total > 0 && done >= total);
    }

    public void SetLevel(int level, float fraction)
    {
        if (levelNumber == null) return;

        levelNumber.text = level.ToString();
        levelFill.style.width = Length.Percent(100f * Mathf.Clamp01(fraction));
    }

    public void SetCards(ModeCardData prev, ModeCardData current, ModeCardData next, int index, int count)
    {
        Fill(modeCard, current);
        Fill(prevCard, prev);
        Fill(nextCard, next);
        bool single = count < 2;
        prevCard.style.display = single ? DisplayStyle.None : DisplayStyle.Flex;
        nextCard.style.display = single ? DisplayStyle.None : DisplayStyle.Flex;
        playButton?.SetEnabled(current.Unlocked);
        SetDots(index, count);
    }

    private static void Fill(VisualElement card, ModeCardData data)
    {
        var art = card.Q(className: "mode-card__art");
        var name = card.Q<Label>(className: "mode-card__name");
        var tagline = card.Q<Label>(className: "mode-card__tagline");
        var bestIcon = card.Q(className: "mode-card__best-icon");
        var bestLabel = card.Q<Label>(className: "mode-card__best-label");

        art.Clear();
        card.EnableInClassList("mode-card--locked", !data.Unlocked);
        bestIcon.EnableInClassList("icon--people", data.Together);
        bestIcon.EnableInClassList("icon--lock", !data.Unlocked);
        bestIcon.EnableInClassList("icon--star-fill", data.Unlocked && data.Note != null);
        bestIcon.EnableInClassList("icon--crown", data.Unlocked && !data.Together && data.Note == null);

        if (data.Together)
        {
            name.text = Loc.T("Together");
            tagline.text = Loc.T("Play with your friends");
            art.EnableInClassList("mode-card__art--icon", false);
            art.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            var duo = new VisualElement { pickingMode = PickingMode.Ignore };
            duo.AddToClassList("mode-card__duo");
            foreach (var sprite in new[] { data.Left, data.Right })
            {
                var ball = new VisualElement { pickingMode = PickingMode.Ignore };
                ball.AddToClassList("mode-card__duo-ball");
                if (sprite != null) ball.style.backgroundImage = new StyleBackground(sprite);
                duo.Add(ball);
            }
            art.Add(duo);
            bestLabel.text = Loc.T("2–4 players");
            return;
        }

        var mode = data.Mode;
        if (mode == null) return;

        name.text = mode.Title;
        tagline.text = mode.Blurb;
        bool iconArt = data.Art == null;
        art.EnableInClassList("mode-card__art--icon", iconArt);
        art.style.backgroundImage = iconArt ? new StyleBackground(StyleKeyword.None) : new StyleBackground(data.Art);
        if (iconArt)
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("icon");
            glyph.AddToClassList(mode.Kind switch
            {
                GameModeKind.Rush => "icon--clock",
                GameModeKind.Arcade => "icon--coin",
                GameModeKind.Adventure => "icon--map",
                _ => "icon--play"
            });
            art.Add(glyph);
        }

        if (data.Unlocked) bestLabel.text = data.Note ?? (data.Best > 0 ? Loc.T("Best {0}", data.Best) : Loc.T("No record yet"));
        else bestLabel.text = Loc.T("Classic best {0} to unlock", mode.RequiredClassicBest);
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

    private void OnPointerDown(PointerDownEvent e)
    {
        FinishSnap();
        pressing = true;
        dragging = false;
        pressX = e.position.x;
        lastX = pressX;
        lastTime = Time.realtimeSinceStartup;
        velocity = 0f;
        strip.CapturePointer(e.pointerId);
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!pressing) return;

        float drag = e.position.x - pressX;
        if (!dragging && ModeSwipe.IsTap(drag)) return;

        dragging = true;
        Track(e.position.x);
        Place(Mathf.Clamp(drag, -Spacing, Spacing));
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (!pressing) return;

        pressing = false;
        if (strip.HasPointerCapture(e.pointerId)) strip.ReleasePointer(e.pointerId);
        float drag = e.position.x - pressX;
        if (Time.realtimeSinceStartup - lastTime > StillSeconds) velocity = 0f;

        if (!dragging || ModeSwipe.IsTap(drag))
        {
            dragging = false;
            Place(0f);
            TapAt(e.position);
            return;
        }

        dragging = false;
        int direction = ModeSwipe.Decide(drag, velocity, Spacing);
        if (direction == 0)
        {
            SnapFrom(offset);
            return;
        }

        Step(direction, offset + direction * Spacing);
    }

    private void Cancel()
    {
        if (!pressing) return;
        pressing = false;
        dragging = false;
        SnapFrom(offset);
    }

    private void TapAt(Vector2 position)
    {
        var center = modeCard.worldBound;
        if (position.x < center.xMin) Step(-1, -Spacing);
        else if (position.x > center.xMax) Step(1, Spacing);
    }

    private void Step(int direction, float from)
    {
        UiFeedback.Tap();
        HapticManager.Light();
        onStepMode?.Invoke(direction);
        if (!GameSettings.SwipeHintDone) GameSettings.SwipeHintDone = true;
        ShowHint(false);
        SnapFrom(from);
    }

    private void Track(float x)
    {
        float now = Time.realtimeSinceStartup;
        float dt = now - lastTime;
        if (dt > 0.0001f) velocity = Mathf.Lerp(velocity, (x - lastX) / dt, 0.6f);
        lastX = x;
        lastTime = now;
    }

    private void Place(float value)
    {
        offset = value;
        track.style.translate = new Translate(value, 0f);
    }

    private void SnapFrom(float from)
    {
        snap?.Pause();
        snapFrom = from;
        snapStart = Time.realtimeSinceStartup;
        Place(from);
        snap = track.schedule.Execute(() =>
        {
            float t = (Time.realtimeSinceStartup - snapStart) / SnapSeconds;
            Place(snapFrom * (1f - FeelMath.Overshoot(t)));
            if (t >= 1f) FinishSnap();
        }).Every(FrameMs);
    }

    private void FinishSnap()
    {
        snap?.Pause();
        snap = null;
        Place(0f);
    }

    protected override void OnShow()
    {
        ShowHint(!GameSettings.SwipeHintDone);
    }

    protected override void OnHide()
    {
        pressing = false;
        dragging = false;
        FinishSnap();
        nudge?.Pause();
        nudge = null;
    }

    private void ShowHint(bool visible)
    {
        if (hint == null) return;

        hint.EnableInClassList(HintHiddenClass, !visible);
        nudge?.Pause();
        nudge = null;
        if (!visible) return;

        nudge = hint.schedule.Execute(() =>
        {
            float wave = Mathf.Sin(Time.realtimeSinceStartup * 3f);
            hint.style.translate = new Translate(-HintNudge * Mathf.Max(0f, wave), 0f);
        }).Every(FrameMs);
    }
}
