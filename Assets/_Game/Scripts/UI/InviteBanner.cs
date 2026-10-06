using System;
using UnityEngine.UIElements;

public sealed class InviteBanner
{
    private const string HiddenClass = "invite-banner--hidden";
    private const long VisibleMs = 20000;

    private readonly VisualElement root;
    private readonly Label title;
    private readonly Label mode;
    private IVisualElementScheduledItem hideJob;
    private Action onJoin;

    public bool IsVisible { get; private set; }

    public InviteBanner(VisualElement root)
    {
        this.root = root;
        title = root.Q<Label>("invite-title");
        mode = root.Q<Label>("invite-mode");

        Bind("invite-join", () =>
        {
            var join = onJoin;
            Hide();
            join?.Invoke();
        });
        Bind("invite-close", Hide, true);
        Hide();
    }

    public void Present(string from, string modeTitle, Action join)
    {
        onJoin = join;
        title.text = Loc.T("{0} invites you", from);
        mode.text = modeTitle;
        IsVisible = true;
        root.style.display = DisplayStyle.Flex;
        root.schedule.Execute(() => root.RemoveFromClassList(HiddenClass)).StartingIn(16);
        hideJob?.Pause();
        hideJob = root.schedule.Execute(Hide).StartingIn(VisibleMs);
        UiFeedback.Tap();
    }

    public void Hide()
    {
        IsVisible = false;
        onJoin = null;
        hideJob?.Pause();
        root.AddToClassList(HiddenClass);
        root.style.display = DisplayStyle.None;
    }

    private void Bind(string name, Action action, bool back = false)
    {
        var button = root.Q<Button>(name);
        if (button == null) return;
        button.focusable = false;
        button.clicked += () =>
        {
            if (back) UiFeedback.Back();
            else UiFeedback.Tap();
            action();
        };
    }
}
