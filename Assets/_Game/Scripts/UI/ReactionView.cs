using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ReactionView
{
    private const long FloatMs = 1600;

    private static readonly string[] Icons = { "icon--thumb", "icon--wow", "icon--laugh", "icon--flame" };

    private readonly VisualElement bar;
    private readonly VisualElement floatLayer;
    private readonly Action<byte> onSend;
    private bool open;

    public ReactionView(VisualElement root, Action<byte> onSend)
    {
        this.onSend = onSend;
        bar = root.Q("reaction-bar");
        floatLayer = root.Q("float-layer");

        Hook(root.Q<Button>("react-toggle"), () => SetOpen(!open));
        for (int i = 0; i < Icons.Length; i++)
        {
            byte id = (byte)i;
            Hook(root.Q<Button>($"react-{i}"), () => Send(id));
        }
        SetOpen(false);
    }

    public void Close()
    {
        SetOpen(false);
        floatLayer.Clear();
    }

    public void Receive(byte id, float x, float y)
    {
        if (id >= Icons.Length) return;

        Float(id, x, y, 220f);
        AudioManager.PlayOne(Sfx.Reaction);
        HapticManager.Light();
    }

    private void Send(byte id)
    {
        SetOpen(false);
        onSend?.Invoke(id);

        Vector2 size = floatLayer.layout.size;
        if (float.IsNaN(size.x) || size.x <= 0f) return;
        Float(id, size.x - 300f, size.y - 420f, -280f);
        AudioManager.PlayOne(Sfx.Reaction);
    }

    public Vector2 LayerSize => floatLayer.layout.size;

    private void Float(byte id, float x, float y, float rise)
    {
        if (id >= Icons.Length) return;

        var bubble = UiFactory.Element("reaction-float reaction-float--start");
        bubble.Add(UiFactory.Element("icon " + Icons[id]));
        bubble.style.left = x;
        bubble.style.top = y;
        floatLayer.Add(bubble);

        bubble.schedule.Execute(() =>
        {
            bubble.RemoveFromClassList("reaction-float--start");
            bubble.style.translate = new Translate(0f, rise);
            bubble.AddToClassList("reaction-float--gone");
        }).StartingIn(16);
        bubble.schedule.Execute(() => bubble.RemoveFromHierarchy()).StartingIn(FloatMs);
    }

    private void SetOpen(bool value)
    {
        open = value;
        bar.EnableInClassList("reaction-bar--hidden", !value);
        bar.pickingMode = value ? PickingMode.Position : PickingMode.Ignore;
        foreach (var button in bar.Query<Button>().ToList()) button.SetEnabled(value);
    }

    private static void Hook(Button button, Action action)
    {
        if (button == null) return;
        button.focusable = false;
        button.clicked += () =>
        {
            UiFeedback.Tap();
            action();
        };
    }
}
