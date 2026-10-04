using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SettingsScreen : UIScreen
{
    private const string SwitchOnClass = "switch--on";
    private const string ArmedClass = "is-armed";
    private const long ConfirmMs = 2500;

    private sealed class ConfirmPill
    {
        private readonly Button button;
        private readonly Label label;
        private readonly string idleText;
        private IVisualElementScheduledItem disarmJob;

        public bool Armed { get; private set; }

        public ConfirmPill(Button button, Label label, string idleText)
        {
            this.button = button;
            this.label = label;
            this.idleText = idleText;
        }

        public void Arm()
        {
            Armed = true;
            button.AddToClassList(ArmedClass);
            label.text = "Tap again";
            disarmJob?.Pause();
            disarmJob = button.schedule.Execute(Disarm).StartingIn(ConfirmMs);
        }

        public void Disarm()
        {
            Armed = false;
            disarmJob?.Pause();
            button?.RemoveFromClassList(ArmedClass);
            if (label != null) label.text = idleText;
        }

        public void SetEnabled(bool enabled)
        {
            button?.SetEnabled(enabled);
        }
    }

    private readonly VisualElement vibrationSwitch;
    private readonly VisualElement soundSwitch;
    private readonly Label bestValue;
    private readonly ConfirmPill reset;
    private readonly ConfirmPill deleteOnline;
    private readonly Func<int> getBest;
    private readonly Action onReset;
    private readonly Action onDeleteOnline;

    public SettingsScreen(VisualElement root, Action onBack, Func<int> getBest, Action onReset, Action onDeleteOnline) : base(root)
    {
        this.getBest = getBest;
        this.onReset = onReset;
        this.onDeleteOnline = onDeleteOnline;

        vibrationSwitch = root.Q("vibration-switch");
        soundSwitch = root.Q("sound-switch");
        bestValue = root.Q<Label>("best-value");
        root.Q<Label>("version-label").text = $"Pingi Pongi · v{Application.version}";

        vibrationSwitch.RegisterCallback<ClickEvent>(_ => ToggleVibration());
        soundSwitch.RegisterCallback<ClickEvent>(_ => ToggleSound());
        Bind("back-button", onBack);
        reset = new ConfirmPill(Bind("reset-button", OnResetPressed), root.Q<Label>("reset-label"), "Reset");
        deleteOnline = new ConfirmPill(Bind("online-button", OnDeleteOnlinePressed), root.Q<Label>("online-label"), "Delete");
    }

    public void SetOnlineBusy(bool busy)
    {
        deleteOnline.SetEnabled(!busy);
    }

    protected override void OnShow()
    {
        vibrationSwitch.EnableInClassList(SwitchOnClass, GameSettings.HapticsEnabled);
        soundSwitch.EnableInClassList(SwitchOnClass, GameSettings.SoundEnabled);
        RefreshBest();
        reset.Disarm();
        deleteOnline.Disarm();
        deleteOnline.SetEnabled(true);
    }

    protected override void OnHide()
    {
        reset.Disarm();
        deleteOnline.Disarm();
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
        if (!reset.Armed)
        {
            reset.Arm();
            return;
        }

        reset.Disarm();
        onReset?.Invoke();
        HapticManager.Medium();
        RefreshBest();
    }

    private void OnDeleteOnlinePressed()
    {
        if (!deleteOnline.Armed)
        {
            deleteOnline.Arm();
            return;
        }

        deleteOnline.Disarm();
        HapticManager.Medium();
        onDeleteOnline?.Invoke();
    }

    private void RefreshBest()
    {
        int best = getBest != null ? getBest() : 0;
        bestValue.text = best.ToString();
        reset.SetEnabled(best > 0);
    }
}
