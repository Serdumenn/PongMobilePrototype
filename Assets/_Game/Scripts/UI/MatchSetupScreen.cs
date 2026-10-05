using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class MatchSetupScreen : UIScreen
{
    private const float MidHeight = 176f;
    private const float SideDepth = 520f;
    private const float PanelContent = 580f;

    private sealed class Slot
    {
        public FieldSide Side;
        public int Look;
        public bool Joined;
        public bool Ready;
        public VisualElement Panel;
        public VisualElement Picker;
        public VisualElement Ball;
        public Button Action;
    }

    private readonly VisualElement board;
    private readonly CosmeticsService cosmetics;
    private readonly Action onBack;
    private readonly Action<List<LocalMatchController.PlayerEntry>> onStart;
    private readonly List<Slot> slots = new List<Slot>();
    private readonly List<CosmeticItem> looks = new List<CosmeticItem>();

    private GameModeDefinition mode;
    private VisualElement middle;
    private VisualElement line;
    private Button partyBack;
    private Button startButton;
    private bool party;
    private bool started;

    public MatchSetupScreen(VisualElement root, CosmeticsService cosmetics, Action onBack, Action<List<LocalMatchController.PlayerEntry>> onStart) : base(root)
    {
        this.cosmetics = cosmetics;
        this.onBack = onBack;
        this.onStart = onStart;

        board = root.Q("setup-board");
        board.RegisterCallback<GeometryChangedEvent>(_ => Arrange());
    }

    public int JoinedCount
    {
        get
        {
            int count = 0;
            foreach (var slot in slots) if (slot.Joined) count++;
            return count;
        }
    }

    public void Present(GameModeDefinition definition)
    {
        mode = definition;
        party = mode.Topology == FieldTopology.FourSides;
        started = false;
        CollectLooks();
        Build();
        Show();
    }

    public void SetJoined(FieldSide side, bool joined)
    {
        var slot = Find(side);
        if (slot == null || !party) return;
        slot.Joined = joined;
        if (joined) slot.Look = FreeLook(slot, slot.Look, 1);
        RefreshSlot(slot);
        RefreshStart();
    }

    public void SetReady(FieldSide side)
    {
        var slot = Find(side);
        if (slot == null || party || slot.Ready) return;
        slot.Ready = true;
        RefreshSlot(slot);
        TryAutoStart();
    }

    public void StartParty()
    {
        if (!party || JoinedCount < mode.MinPlayers) return;
        Launch();
    }

    private void CollectLooks()
    {
        looks.Clear();
        if (cosmetics == null) return;

        var equipped = cosmetics.Equipped(CosmeticCategory.Ball);
        if (equipped != null) looks.Add(equipped);

        foreach (var item in cosmetics.CatalogAsset.InCategory(CosmeticCategory.Ball))
            if (item != equipped && cosmetics.IsUnlocked(item)) looks.Add(item);
    }

    private void Build()
    {
        board.Clear();
        slots.Clear();

        foreach (var side in LocalMatchController.SidesFor(mode))
        {
            var slot = new Slot { Side = side, Joined = !party };
            slot.Look = FreeLook(slot, slots.Count % Mathf.Max(1, looks.Count), 1);
            slots.Add(slot);
            slot.Panel = BuildPanel(slot);
            board.Add(slot.Panel);
            RefreshSlot(slot);
        }

        line = party ? null : UiFactory.Element("setup-line");
        if (line != null) board.Add(line);

        partyBack = party ? UiFactory.Button("btn btn--icon", null, "icon--back", onBack, true) : null;
        if (partyBack != null)
        {
            partyBack.style.position = Position.Absolute;
            board.Add(partyBack);
        }

        middle = party ? BuildPartyMiddle() : BuildDuelMiddle();
        board.Add(middle);
        RefreshStart();
        Arrange();
    }

    private VisualElement BuildPanel(Slot slot)
    {
        int index = (int)slot.Side;
        var panel = UiFactory.Element("side-panel");
        panel.name = $"setup-{slot.Side.ToString().ToLowerInvariant()}";

        panel.Add(UiFactory.Text(Loc.T("Player {0}", index + 1), $"setup-name player-text-{index}"));

        slot.Picker = UiFactory.Element("setup-picker");
        slot.Picker.Add(UiFactory.Button("btn btn--round", null, "icon--chevron-left", () => StepLook(slot, -1)));
        slot.Ball = UiFactory.Element("setup-ball");
        slot.Picker.Add(slot.Ball);
        slot.Picker.Add(UiFactory.Button("btn btn--round", null, "icon--chevron-right", () => StepLook(slot, 1)));
        panel.Add(slot.Picker);

        slot.Action = UiFactory.Button("btn setup-ready", Loc.T("Ready"), "icon--check", () => OnAction(slot));
        slot.Action.name = $"ready-{slot.Side.ToString().ToLowerInvariant()}";
        panel.Add(slot.Action);
        return panel;
    }

    private VisualElement BuildDuelMiddle()
    {
        var mid = UiFactory.Element("setup-mid");
        mid.Add(UiFactory.Button("btn btn--icon", null, "icon--back", onBack, true));
        mid.Add(UiFactory.Text(Rule(), "ink-chip"));
        mid.Add(UiFactory.Element("court-gap"));
        return mid;
    }

    private VisualElement BuildPartyMiddle()
    {
        var mid = UiFactory.Element("side-panel");
        mid.Add(UiFactory.Text(Rule(), "ink-chip"));

        startButton = UiFactory.Button("btn btn--primary setup-start", Loc.T("Start"), null, StartParty);
        startButton.name = "start-button";
        startButton.style.marginTop = 40f;
        mid.Add(startButton);
        return mid;
    }

    private string Rule()
    {
        return mode.Kind switch
        {
            GameModeKind.TableDuel => Loc.T("First to {0}", mode.PointsToWin),
            GameModeKind.CoopRally => Loc.Plural("{0} shared life", "{0} shared lives", mode.Lives),
            _ => Loc.Plural("{0} life each", "{0} lives each", mode.Lives)
        };
    }

    private void OnAction(Slot slot)
    {
        if (party) SetJoined(slot.Side, !slot.Joined);
        else if (!slot.Ready) SetReady(slot.Side);
    }

    private void StepLook(Slot slot, int direction)
    {
        if (looks.Count == 0 || slot.Ready) return;
        slot.Look = FreeLook(slot, slot.Look + direction, direction);
        RefreshSlot(slot);
    }

    private int FreeLook(Slot slot, int start, int direction)
    {
        int count = looks.Count;
        if (count == 0) return 0;

        for (int step = 0; step < count; step++)
        {
            int candidate = ((start + step * direction) % count + count) % count;
            if (!Taken(slot, candidate)) return candidate;
        }

        return ((start % count) + count) % count;
    }

    private bool Taken(Slot owner, int look)
    {
        foreach (var slot in slots)
            if (slot != owner && slot.Joined && slot.Look == look) return true;
        return false;
    }

    private void RefreshSlot(Slot slot)
    {
        int index = (int)slot.Side;
        var look = looks.Count > 0 ? looks[slot.Look] : null;
        UiFactory.SetPicture(slot.Ball, slot.Ready ? look?.Happy : look?.Idle);

        slot.Picker.style.opacity = slot.Joined ? 1f : 0.35f;
        slot.Picker.SetEnabled(slot.Joined && !slot.Ready);

        bool active = party ? slot.Joined : slot.Ready;
        slot.Action.EnableInClassList($"player-btn-{index}", party ? slot.Joined : true);
        slot.Action.EnableInClassList("join-btn", party && !slot.Joined);
        slot.Action.EnableInClassList("setup-ready--on", !party && slot.Ready);

        var label = UiFactory.ButtonLabel(slot.Action);
        var icon = UiFactory.ButtonIcon(slot.Action);
        if (party) label.text = slot.Joined ? Loc.T("Playing") : Loc.T("Tap to join");
        else label.text = slot.Ready ? Loc.T("Ready!") : Loc.T("Ready");
        icon.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        icon.EnableInClassList("icon--light", index != 2);
    }

    private void RefreshStart()
    {
        startButton?.SetEnabled(JoinedCount >= (mode != null ? mode.MinPlayers : 2));
    }

    private void TryAutoStart()
    {
        foreach (var slot in slots) if (!slot.Ready) return;
        Launch();
    }

    private void Launch()
    {
        if (started) return;
        started = true;

        var entries = new List<LocalMatchController.PlayerEntry>();
        foreach (var slot in slots)
        {
            if (!slot.Joined) continue;
            entries.Add(new LocalMatchController.PlayerEntry(slot.Side, looks.Count > 0 ? looks[slot.Look] : null));
        }

        onStart?.Invoke(entries);
    }

    private Slot Find(FieldSide side)
    {
        foreach (var slot in slots) if (slot.Side == side) return slot;
        return null;
    }

    private void Arrange()
    {
        Vector2 size = board.layout.size;
        if (float.IsNaN(size.x) || size.x <= 0f || slots.Count == 0) return;

        if (party) ArrangeParty(size);
        else ArrangeDuel(size);
    }

    private void ArrangeDuel(Vector2 size)
    {
        float half = (size.y - MidHeight) / 2f;
        foreach (var slot in slots)
        {
            var s = slot.Panel.style;
            s.left = 0f;
            s.width = size.x;
            s.height = half;
            bool top = slot.Side == FieldSide.Top;
            s.top = top ? 0f : half + MidHeight;
            s.rotate = new Rotate(top ? 180f : 0f);
        }

        middle.style.top = half;
        if (line != null) line.style.top = size.y / 2f - 4f;
    }

    private void ArrangeParty(Vector2 size)
    {
        float shortSide = Mathf.Min(size.x, size.y);
        float width = shortSide * 0.6f;
        float depth = Mathf.Min(SideDepth, shortSide * 0.3f);
        Vector2 center = size / 2f;

        foreach (var slot in slots)
        {
            Vector2 anchor;
            float angle;
            switch (slot.Side)
            {
                case FieldSide.Top: anchor = new Vector2(center.x, depth / 2f); angle = 180f; break;
                case FieldSide.Left: anchor = new Vector2(depth / 2f, center.y); angle = 90f; break;
                case FieldSide.Right: anchor = new Vector2(size.x - depth / 2f, center.y); angle = -90f; break;
                default: anchor = new Vector2(center.x, size.y - depth / 2f); angle = 0f; break;
            }

            float scale = Mathf.Clamp01(depth / PanelContent);
            float boxWidth = width / Mathf.Max(0.01f, scale);
            var s = slot.Panel.style;
            s.width = boxWidth;
            s.height = PanelContent;
            s.left = anchor.x - boxWidth / 2f;
            s.top = anchor.y - PanelContent / 2f;
            s.rotate = new Rotate(angle);
            s.scale = new Scale(Vector3.one * scale);
        }

        float inner = Mathf.Max(200f, size.x - depth * 2f - 48f);
        var m = middle.style;
        m.width = inner;
        m.height = 360f;
        m.left = center.x - inner / 2f;
        m.top = center.y - 180f;
        startButton.style.width = Mathf.Min(400f, inner);

        if (partyBack != null)
        {
            partyBack.style.left = 24f;
            partyBack.style.top = size.y - 176f - 24f;
        }
    }
}
