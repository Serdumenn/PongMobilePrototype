using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class TextFit
{
    public const float MinScale = 0.6f;
    public const string GroupClass = "fit-group";
    public const string SkipClass = "fit-skip";
    private const int PruneEvery = 600;

    private sealed class State
    {
        public string Text;
        public float Base;
        public float Width;
        public float Size;
        public bool Fresh;
        public bool Applied;

        public float Natural => Width * Size / Base;
        public float Floor => Mathf.Ceil(Base * MinScale);
    }

    private readonly VisualElement root;
    private readonly Dictionary<TextElement, State> states = new Dictionary<TextElement, State>();
    private readonly List<TextElement> rowTexts = new List<TextElement>();
    private int runs;
    private Vector2 lastSize;

    public TextFit(VisualElement root)
    {
        this.root = root;
        root.schedule.Execute(Run).Every(0);
    }

    public void Run()
    {
        var size = root.contentRect.size;
        if ((size - lastSize).sqrMagnitude > 0.25f)
        {
            lastSize = size;
            ResetAll();
        }

        Visit(root);
        if (++runs % PruneEvery == 0) Prune();
    }

    public void ResetAll()
    {
        foreach (var pair in states)
        {
            if (pair.Value.Applied) pair.Key.style.fontSize = StyleKeyword.Null;
            pair.Value.Applied = false;
            pair.Value.Fresh = true;
        }
    }

    private void Visit(VisualElement element)
    {
        if (element.resolvedStyle.display == DisplayStyle.None || element.ClassListContains(SkipClass) || element is TextField) return;

        if (element is TextElement text) FitOwnBox(text);

        int count = element.hierarchy.childCount;
        for (int i = 0; i < count; i++) Visit(element.hierarchy[i]);

        var direction = element.resolvedStyle.flexDirection;
        if (count > 1 && (direction == FlexDirection.Row || direction == FlexDirection.RowReverse)) FitRow(element);
        if (element.ClassListContains(GroupClass)) MatchGroup(element);
    }

    private State Track(TextElement element)
    {
        var style = element.resolvedStyle;
        if (style.whiteSpace != WhiteSpace.NoWrap || style.textOverflow == TextOverflow.Ellipsis) return null;

        bool inline = element.style.fontSize.keyword != StyleKeyword.Null;
        states.TryGetValue(element, out var state);
        if (inline && (state == null || !state.Applied)) return null;

        string content = element.text;
        if (state == null || state.Text != content)
        {
            if (inline) element.style.fontSize = StyleKeyword.Null;
            if (state == null)
            {
                state = new State();
                states[element] = state;
            }
            state.Text = content;
            state.Applied = false;
            state.Fresh = true;
            return null;
        }

        if (string.IsNullOrEmpty(content)) return null;

        if (!state.Fresh && !state.Applied && Mathf.Abs(style.fontSize - state.Base) > 0.25f) state.Fresh = true;

        if (state.Fresh)
        {
            if (inline || style.fontSize <= 0f || float.IsNaN(element.contentRect.width)) return null;
            state.Base = style.fontSize;
            state.Size = state.Base;
            state.Width = element.MeasureTextSize(content, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
            state.Fresh = false;
        }

        return state;
    }

    private void FitOwnBox(TextElement element)
    {
        var state = Track(element);
        if (state == null) return;

        float room = element.contentRect.width;
        if (room > 1f && state.Natural > room + 0.5f) Resize(element, state, room);
    }

    private void FitRow(VisualElement row)
    {
        float room = row.contentRect.width;
        if (float.IsNaN(room) || room <= 1f) return;

        rowTexts.Clear();
        float used = 0f;
        int count = row.hierarchy.childCount;
        for (int i = 0; i < count; i++)
        {
            var child = row.hierarchy[i];
            var style = child.resolvedStyle;
            if (style.display == DisplayStyle.None || style.position == Position.Absolute) continue;

            float margins = style.marginLeft + style.marginRight;
            if (child is TextElement text && states.TryGetValue(text, out var state) && !state.Fresh && state.Text == text.text && state.Base > 0f)
            {
                if (child.ClassListContains(SkipClass)) return;
                used += state.Natural + margins + style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
                rowTexts.Add(text);
            }
            else if (child is TextElement other && states.TryGetValue(other, out var pending) && pending.Fresh) return;
            else used += child.layout.width + margins;
        }

        float overflow = used - room;
        if (overflow <= 0.5f || rowTexts.Count == 0) return;

        rowTexts.Sort((a, b) => states[b].Natural.CompareTo(states[a].Natural));
        foreach (var text in rowTexts)
        {
            var state = states[text];
            float natural = state.Natural;
            float spare = natural - state.Width * state.Floor / state.Base;
            if (spare <= 0f) continue;

            float cut = Mathf.Min(spare, overflow);
            Resize(text, state, natural - cut);
            overflow -= natural - state.Natural;
            if (overflow <= 0.5f) break;
        }
    }

    private void MatchGroup(VisualElement group)
    {
        float ratio = 1f;
        foreach (var child in group.Children())
            if (child is TextElement text && states.TryGetValue(text, out var state) && !state.Fresh && state.Base > 0f)
                ratio = Mathf.Min(ratio, state.Size / state.Base);

        if (ratio >= 1f) return;
        foreach (var child in group.Children())
            if (child is TextElement text && states.TryGetValue(text, out var state) && !state.Fresh && state.Base > 0f)
                SetSize(text, state, Mathf.Floor(state.Base * ratio));
    }

    private static void Resize(TextElement element, State state, float room)
    {
        float size = Mathf.Floor(state.Size * room / state.Natural);
        SetSize(element, state, size);
    }

    private static void SetSize(TextElement element, State state, float size)
    {
        size = Mathf.Max(state.Floor, size);
        if (size >= state.Size - 0.25f) return;

        state.Size = size;
        state.Applied = true;
        element.style.fontSize = size;
    }

    private void Prune()
    {
        var gone = new List<TextElement>();
        foreach (var element in states.Keys)
            if (element.panel == null) gone.Add(element);
        foreach (var element in gone) states.Remove(element);
    }
}
