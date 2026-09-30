using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SettingsScreen : UIScreen
{
    private const string SwitchOnClass = "switch--on";
    private const string ArmedClass = "is-armed";
    private const long ResetConfirmMs = 2500;

    private readonly VisualElement vibrationSwitch;
    private readonly VisualElement soundSwitch;
    private readonly Label bestValue;
    private readonly Button resetButton;
    private readonly Label resetLabel;
    private readonly Func<int> getBest;
    private readonly Action onReset;

    private IVisualElementScheduledItem disarmJob;
    private bool armed;

    public SettingsScreen(VisualElement root, Action onBack, Func<int> getBest, Action onReset) : base(root)
    {
        this.getBest = getBest;
        this.onReset = onReset;

        vibrationSwitch = root.Q("vibration-switch");
        soundSwitch = root.Q("sound-switch");
        bestValue = root.Q<Label>("best-value");
        resetLabel = root.Q<Label>("reset-label");
        root.Q<Label>("version-label").text = $"Pingi Pongi · v{Application.version}";

        vibrationSwitch.RegisterCallback<ClickEvent>(_ => ToggleVibration());
        soundSwitch.RegisterCallback<ClickEvent>(_ => ToggleSound());
        Bind("back-button", onBack);
        resetButton = Bind("reset-button", OnResetPressed);
    }

    protected override void OnShow()
    {
        vibrationSwitch.EnableInClassList(SwitchOnClass, GameSettings.HapticsEnabled);
        soundSwitch.EnableInClassList(SwitchOnClass, GameSettings.SoundEnabled);
        RefreshBest();
        Disarm();
    }

    protected override void OnHide()
    {
        Disarm();
    }

    private void ToggleVibration()
    {
        bool enabled = !GameSettings.HapticsEnabled;
        GameSettings.HapticsEnabled = enabled;
        vibrationSwitch.EnableInClassList(SwitchOnClass, enabled);
        if (enabled) HapticManager.Soft();
    }

    private void ToggleSound()
    {
        bool enabled = !GameSettings.SoundEnabled;
        GameSettings.SoundEnabled = enabled;
        soundSwitch.EnableInClassList(SwitchOnClass, enabled);

        if (enabled) AudioManager.PlayOne(Sfx.UiTap);
        else if (AudioManager.Instance != null) AudioManager.Instance.StopAll();
    }

    private void OnResetPressed()
    {
        if (!armed)
        {
            armed = true;
            resetButton.AddToClassList(ArmedClass);
            resetLabel.text = "Tap again";
            disarmJob?.Pause();
            disarmJob = resetButton.schedule.Execute(Disarm).StartingIn(ResetConfirmMs);
            return;
        }

        Disarm();
        onReset?.Invoke();
        HapticManager.Medium();
        RefreshBest();
    }

    private void Disarm()
    {
        armed = false;
        disarmJob?.Pause();
        resetButton?.RemoveFromClassList(ArmedClass);
        if (resetLabel != null) resetLabel.text = "Reset";
    }

    private void RefreshBest()
    {
        int best = getBest != null ? getBest() : 0;
        bestValue.text = best.ToString();
        resetButton?.SetEnabled(best > 0);
    }
}
