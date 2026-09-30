using UnityEngine.UIElements;

public sealed class ToastView
{
    private const string HiddenClass = "toast--hidden";
    private const long VisibleMs = 2400;

    private readonly VisualElement root;
    private readonly Label label;
    private IVisualElementScheduledItem hideJob;

    public ToastView(VisualElement root)
    {
        this.root = root;
        label = root?.Q<Label>("toast-label");
    }

    public void Show(string message)
    {
        if (root == null || label == null || string.IsNullOrEmpty(message)) return;

        label.text = message;
        root.RemoveFromClassList(HiddenClass);
        hideJob?.Pause();
        hideJob = root.schedule.Execute(() => root.AddToClassList(HiddenClass)).StartingIn(VisibleMs);
    }
}
