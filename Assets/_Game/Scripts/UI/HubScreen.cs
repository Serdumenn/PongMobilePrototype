using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class HubScreen : UIScreen
{
    private readonly LocalMatchController match;
    private readonly CosmeticsService cosmetics;
    private readonly Action<GameModeDefinition> onPick;
    private readonly ScrollView local;
    private readonly VisualElement online;
    private readonly VisualElement challenges;
    private readonly VisualElement note;
    private readonly Button[] tabs;

    public HubScreen(VisualElement root, LocalMatchController match, CosmeticsService cosmetics, Action onBack, Action<GameModeDefinition> onPick) : base(root)
    {
        this.match = match;
        this.cosmetics = cosmetics;
        this.onPick = onPick;

        local = root.Q<ScrollView>("hub-local");
        online = root.Q("hub-online");
        challenges = root.Q("hub-challenges");
        note = root.Q("hub-note");

        Bind("back-button", onBack);
        tabs = new[]
        {
            Bind("tab-local", () => SelectTab(0)),
            Bind("tab-online", () => SelectTab(1)),
            Bind("tab-challenges", () => SelectTab(2))
        };
    }

    protected override void OnShow()
    {
        BuildTiles();
        SelectTab(0);
    }

    private void SelectTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++) tabs[i]?.EnableInClassList("tab--active", i == index);

        local.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        online.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        challenges.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
        note.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BuildTiles()
    {
        local.Clear();
        if (match == null) return;

        foreach (var mode in match.ModeList)
            if (mode != null) local.Add(Tile(mode));
    }

    private VisualElement Tile(GameModeDefinition mode)
    {
        bool fits = LocalMatchController.FitsScreen(mode);

        var tile = new Button { focusable = false, name = $"tile-{mode.Id}" };
        tile.RemoveFromClassList(Button.ussClassName);
        tile.AddToClassList("hub-tile");
        tile.EnableInClassList("hub-tile--locked", !fits);
        tile.clicked += () =>
        {
            UiFeedback.Tap();
            onPick?.Invoke(mode);
        };

        var art = UiFactory.Element("hub-tile__art");
        art.Add(Art(mode));
        tile.Add(art);

        var text = UiFactory.Element("hub-tile__text");
        text.Add(UiFactory.Text(mode.DisplayName, "hub-tile__name"));
        text.Add(UiFactory.Text(mode.Tagline, "hub-tile__sub"));
        if (!fits)
        {
            var lockRow = UiFactory.Element("hub-tile__lock");
            lockRow.Add(UiFactory.Element("icon icon--lock"));
            lockRow.Add(UiFactory.Text("Tablet only", "hub-tile__lock-label"));
            text.Add(lockRow);
        }
        tile.Add(text);

        string players = mode.MinPlayers == mode.MaxPlayers ? $"{mode.MinPlayers}" : $"{mode.MinPlayers}–{mode.MaxPlayers}";
        tile.Add(UiFactory.Text(players, mode.Kind == GameModeKind.CoopRally ? "players-chip players-chip--team" : "players-chip"));
        return tile;
    }

    private VisualElement Art(GameModeDefinition mode)
    {
        switch (mode.Kind)
        {
            case GameModeKind.TableDuel:
                var duel = UiFactory.Element("");
                duel.style.alignItems = Align.Center;
                duel.Add(UiFactory.Picture(match.PaddleSpriteFor(FieldSide.Top), "hub-art-paddle hub-art-paddle--flip"));
                duel.Add(UiFactory.Text("vs", "hub-art-vs"));
                duel.Add(UiFactory.Picture(match.PaddleSpriteFor(FieldSide.Bottom), "hub-art-paddle"));
                return duel;

            case GameModeKind.CoopRally:
                var pair = UiFactory.Element("hub-art-row");
                pair.Add(UiFactory.Picture(Ball("ball_pingi"), "hub-art-ball"));
                pair.Add(UiFactory.Picture(Ball("ball_minty"), "hub-art-ball"));
                return pair;

            default:
                var grid = UiFactory.Element("hub-art-grid");
                foreach (var id in new[] { "ball_pingi", "ball_minty", "ball_kitty", "ball_grape" })
                    grid.Add(UiFactory.Picture(Ball(id), "hub-art-ball"));
                return grid;
        }
    }

    private Sprite Ball(string id)
    {
        var item = cosmetics != null ? cosmetics.CatalogAsset.Find(id) : null;
        return item != null ? item.Idle : null;
    }
}
