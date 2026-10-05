using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HudScreen : UIScreen
{
    private const string PopClass = "hud-score--pop";
    private const string HintHiddenClass = "hint--hidden";
    private const string TimedClass = "hud--timed";
    private const long PopMs = 70;
    private const long PerfectMs = 520;
    private const float LowTimeSeconds = 10f;

    private readonly Label score;
    private readonly VisualElement hint;
    private readonly VisualElement rush;
    private readonly VisualElement rushFill;
    private readonly Label rushTime;
    private readonly Label combo;
    private readonly Label ghostDelta;
    private readonly Label perfect;

    private IVisualElementScheduledItem perfectJob;
    private int shownSeconds = -1;

    public HudScreen(VisualElement root, Action onPause) : base(root)
    {
        score = root.Q<Label>("score-label");
        hint = root.Q("hint");
        rush = root.Q("rush");
        rushFill = root.Q("rush-fill");
        rushTime = root.Q<Label>("rush-time");
        combo = root.Q<Label>("combo");
        ghostDelta = root.Q<Label>("ghost-delta");
        perfect = root.Q<Label>("perfect");

        Bind("pause-button", onPause);
    }

    public void SetScore(int value, bool animate)
    {
        score.text = value.ToString();
        if (!animate) return;

        score.AddToClassList(PopClass);
        score.schedule.Execute(() => score.RemoveFromClassList(PopClass)).StartingIn(PopMs);
    }

    public void SetHintVisible(bool visible)
    {
        hint.EnableInClassList(HintHiddenClass, !visible);
    }

    public void SetTimed(bool timed)
    {
        Root.EnableInClassList(TimedClass, timed);
        rush.EnableInClassList("rush--hidden", !timed);
        combo.AddToClassList("combo--hidden");
        perfect.RemoveFromClassList("perfect--show");
        shownSeconds = -1;
    }

    public void SetTime(float remaining, float limit)
    {
        float ratio = limit > 0f ? Mathf.Clamp01(remaining / limit) : 0f;
        rushFill.style.width = Length.Percent(ratio * 100f);
        rushFill.EnableInClassList("rush-bar__fill--low", remaining <= LowTimeSeconds);

        int seconds = Mathf.CeilToInt(remaining);
        if (seconds == shownSeconds) return;

        shownSeconds = seconds;
        rushTime.text = $"{seconds / 60}:{seconds % 60:00}";
    }

    public void SetDelta(int? delta)
    {
        ghostDelta.EnableInClassList("ghost-delta--hidden", !delta.HasValue);
        if (!delta.HasValue) return;

        int value = delta.Value;
        ghostDelta.text = value > 0 ? Loc.T("+{0} ahead", value) : value < 0 ? Loc.T("{0} behind", -value) : Loc.T("Tied");
        ghostDelta.EnableInClassList("ghost-delta--ahead", value > 0);
        ghostDelta.EnableInClassList("ghost-delta--behind", value < 0);
    }

    public void ShowHit(HitResult hit)
    {
        if (!hit.Perfect)
        {
            combo.AddToClassList("combo--hidden");
            return;
        }

        perfect.text = Loc.T("Perfect! +{0}", hit.Points);
        perfect.AddToClassList("perfect--show");
        perfectJob?.Pause();
        perfectJob = perfect.schedule.Execute(() => perfect.RemoveFromClassList("perfect--show")).StartingIn(PerfectMs);

        combo.EnableInClassList("combo--hidden", hit.PerfectStreak < 2);
        combo.text = Loc.T("×{0} perfect", hit.PerfectStreak);
    }
}
