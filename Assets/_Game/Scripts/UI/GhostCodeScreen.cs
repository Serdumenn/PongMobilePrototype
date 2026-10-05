using System;
using UnityEngine.UIElements;

public sealed class GhostCodeScreen : UIScreen
{
    private readonly Label code;
    private readonly Label info;
    private readonly Action<string> toast;

    private string current;

    public GhostCodeScreen(VisualElement root, Action<string> toast, Action onDone) : base(root)
    {
        this.toast = toast;
        code = root.Q<Label>("ghost-code");
        info = root.Q<Label>("ghost-code-info");

        Bind("ghost-copy", Copy);
        Bind("ghost-share", Share);
        Bind("ghost-done", onDone);
        root.Q("ghost-code-scrim").RegisterCallback<ClickEvent>(_ => onDone?.Invoke());
    }

    public string Code => current;

    public void Present(string ghostCode, int days)
    {
        current = ghostCode;
        code.text = ghostCode;
        info.text = Loc.T("Friends type it in Challenges to race your run. It works for {0} days.", days);
        Show();
    }

    private void Copy()
    {
        if (string.IsNullOrEmpty(current)) return;
        NativeShare.Copy(current);
        toast?.Invoke(Loc.T("Code copied"));
    }

    private void Share()
    {
        if (string.IsNullOrEmpty(current)) return;
        if (!NativeShare.Text(Loc.T("Race my ghost in Pingi Pongi! Tap Play together, Challenges, Ghost Challenge and type {0}", current), Loc.T("Share your ghost code")))
            toast?.Invoke(Loc.T("Code copied"));
    }
}
