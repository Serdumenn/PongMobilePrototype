using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PauseScreen : UIScreen
{
    private readonly VisualElement mascot;

    public PauseScreen(VisualElement root, Action onResume, Action onHome) : base(root)
    {
        mascot = root.Q("mascot");

        Bind("resume-button", onResume);
        Bind("home-button", onHome);
    }

    public void SetMascot(Sprite sprite)
    {
        if (sprite != null) mascot.style.backgroundImage = new StyleBackground(sprite);
    }
}
