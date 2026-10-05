using System.Collections.Generic;
using UnityEngine.UIElements;

public sealed class UiLocalizer
{
    private readonly List<(TextElement Element, string Source)> items = new List<(TextElement, string)>();

    public int Count => items.Count;

    public void Capture(VisualElement root)
    {
        root.Query<TextElement>().ForEach(element =>
        {
            if (!string.IsNullOrEmpty(element.text) && Loc.Has(element.text)) items.Add((element, element.text));
        });
    }

    public void Apply()
    {
        foreach (var (element, source) in items) element.text = Loc.T(source);
    }
}
