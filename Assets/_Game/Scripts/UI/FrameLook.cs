using System.Collections.Generic;
using UnityEngine.UIElements;

public static class FrameLook
{
    public const string Framed = "framed";
    private const string Prefix = "frame--";
    private const string Badge = "frame-badge";

    private static readonly List<string> scratch = new List<string>();

    public static void Apply(VisualElement row, string frameClass)
    {
        if (row == null) return;

        scratch.Clear();
        foreach (var name in row.GetClasses())
            if (name.StartsWith(Prefix)) scratch.Add(name);
        foreach (var name in scratch) row.RemoveFromClassList(name);
        row.RemoveFromClassList(Framed);

        var old = row.Q(className: Badge);
        if (old != null) old.RemoveFromHierarchy();

        if (string.IsNullOrEmpty(frameClass)) return;

        row.AddToClassList(Framed);
        row.AddToClassList(frameClass);

        string icon = IconFor(frameClass);
        if (icon == null) return;

        var badge = new VisualElement { pickingMode = PickingMode.Ignore };
        badge.AddToClassList("icon");
        badge.AddToClassList(icon);
        badge.AddToClassList(Badge);
        row.Add(badge);
    }

    public static string IconFor(string frameClass)
    {
        return frameClass switch
        {
            "frame--crown" => "icon--crown",
            "frame--fire" => "icon--flame",
            "frame--star" => "icon--star-fill",
            "frame--ice" => "icon--star",
            "frame--galaxy" => "icon--moon",
            "frame--wave" => "icon--wave",
            _ => null
        };
    }
}
