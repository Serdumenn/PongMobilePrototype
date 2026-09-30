using System;
using UnityEngine.UIElements;

public abstract class UIScreen
{
    private const string HiddenClass = "screen--hidden";
    private const long ShowDelayMs = 16;
    private const long HideDelayMs = 220;

    protected readonly VisualElement Root;

    private IVisualElementScheduledItem pending;

    public bool IsVisible { get; private set; }

    protected UIScreen(VisualElement root)
    {
        Root = root;
        HideInstant();
    }

    public void Show()
    {
        if (IsVisible) return;
        IsVisible = true;

        pending?.Pause();
        Root.style.display = DisplayStyle.Flex;
        pending = Root.schedule.Execute(() => Root.RemoveFromClassList(HiddenClass)).StartingIn(ShowDelayMs);

        OnShow();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        pending?.Pause();
        Root.AddToClassList(HiddenClass);
        pending = Root.schedule.Execute(() => Root.style.display = DisplayStyle.None).StartingIn(HideDelayMs);

        OnHide();
    }

    public void HideInstant()
    {
        IsVisible = false;
        pending?.Pause();
        Root.AddToClassList(HiddenClass);
        Root.style.display = DisplayStyle.None;
    }

    protected virtual void OnShow()
    {
    }

    protected virtual void OnHide()
    {
    }

    protected Button Bind(string name, Action action)
    {
        var button = Root.Q<Button>(name);
        if (button == null) return null;

        button.focusable = false;
        button.clicked += () =>
        {
            HapticManager.Soft();
            action?.Invoke();
        };
        return button;
    }
}
