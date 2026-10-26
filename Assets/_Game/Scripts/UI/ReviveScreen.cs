using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ReviveScreen : UIScreen
{
    private const int Seconds = 5;

    private readonly VisualElement mascot;
    private readonly Label count;
    private readonly Button continueButton;
    private readonly Action onTimeout;

    private IVisualElementScheduledItem ticker;
    private int left;
    private bool waiting;

    public ReviveScreen(VisualElement root, Action onContinue, Action onDecline) : base(root)
    {
        mascot = root.Q("mascot");
        count = root.Q<Label>("revive-count");
        continueButton = Bind("continue-button", onContinue);
        Bind("later-button", onDecline);
        onTimeout = onDecline;
    }

    public int SecondsLeft => left;

    public void Present(Sprite face)
    {
        UiFactory.SetPicture(mascot, face);
        left = Seconds;
        count.text = left.ToString();
        SetWaiting(false);

        ticker?.Pause();
        ticker = Root.schedule.Execute(Tick).Every(1000).StartingIn(1000);
        Show();
    }

    public void SetWaiting(bool on)
    {
        waiting = on;
        continueButton?.SetEnabled(!on);
    }

    public void Close()
    {
        ticker?.Pause();
        waiting = false;
        Hide();
    }

    private void Tick()
    {
        if (!IsVisible || waiting) return;

        left = Mathf.Max(0, left - 1);
        count.text = left.ToString();
        if (left > 0) return;

        ticker?.Pause();
        onTimeout?.Invoke();
    }
}
