using System;
using UnityEngine;
using UnityEngine.UIElements;

public static class UiFactory
{
    public static VisualElement Element(string classes, bool pickable = false)
    {
        var element = new VisualElement { pickingMode = pickable ? PickingMode.Position : PickingMode.Ignore };
        AddClasses(element, classes);
        return element;
    }

    public static Label Text(string text, string classes)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        AddClasses(label, classes);
        return label;
    }

    public static VisualElement Picture(Sprite sprite, string classes)
    {
        var element = Element(classes);
        SetPicture(element, sprite);
        return element;
    }

    public static void SetPicture(VisualElement element, Sprite sprite)
    {
        element.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
    }

    public static Button Button(string classes, string label, string icon, Action onClick, bool back = false)
    {
        var button = new Button { focusable = false };
        button.RemoveFromClassList(UnityEngine.UIElements.Button.ussClassName);
        AddClasses(button, classes);

        var face = Element("btn__face");
        if (!string.IsNullOrEmpty(icon)) face.Add(Element("icon " + icon));
        if (!string.IsNullOrEmpty(label)) face.Add(Text(label, "btn__label"));
        button.Add(face);

        button.clicked += () =>
        {
            if (back) UiFeedback.Back();
            else UiFeedback.Tap();
            onClick?.Invoke();
        };
        return button;
    }

    public static Label ButtonLabel(Button button)
    {
        return button.Q<Label>(className: "btn__label");
    }

    public static VisualElement ButtonIcon(Button button)
    {
        return button.Q(className: "icon");
    }

    private static void AddClasses(VisualElement element, string classes)
    {
        if (string.IsNullOrEmpty(classes)) return;
        foreach (var name in classes.Split(' '))
            if (!string.IsNullOrEmpty(name)) element.AddToClassList(name);
    }
}
