using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RushResultScreen : UIScreen
{
    private readonly CosmeticsService cosmetics;
    private readonly VisualElement mascot;
    private readonly Label title;
    private readonly Label caption;
    private readonly VisualElement list;
    private readonly Label note;
    private readonly Button rematchButton;

    private OnlineRushController rush;

    public RushResultScreen(VisualElement root, CosmeticsService cosmetics, Action onRematch, Action onLeave) : base(root)
    {
        this.cosmetics = cosmetics;

        mascot = root.Q("mascot");
        title = root.Q<Label>("title");
        caption = root.Q<Label>("caption");
        list = root.Q("rank-list");
        note = root.Q<Label>("note");

        rematchButton = Bind("rematch-button", onRematch);
        Bind("home-button", onLeave);
    }

    public void Present(OnlineRushController controller)
    {
        if (rush != controller)
        {
            if (rush != null)
            {
                rush.RematchChanged -= Refresh;
                rush.ScoresChanged -= Refresh;
            }
            rush = controller;
            rush.RematchChanged += Refresh;
            rush.ScoresChanged += Refresh;
        }

        Refresh();
        Show();
    }

    private void Refresh()
    {
        var rules = rush?.Rules;
        if (rules == null) return;

        int place = rules.PlaceOf(rules.MySlot);
        title.text = place switch
        {
            1 => Loc.T("1st place!"),
            2 => Loc.T("2nd place"),
            3 => Loc.T("3rd place"),
            _ => Loc.T("4th place")
        };

        var me = rules.Me;
        var look = cosmetics != null && me != null ? cosmetics.CatalogAsset.Find(me.Look) : null;
        if (look == null && cosmetics != null) look = cosmetics.Equipped(CosmeticCategory.Ball);
        if (look != null) UiFactory.SetPicture(mascot, place == 1 ? look.Happy : place == 2 ? look.Idle : look.Sad);

        string mode = rush.Mode != null ? rush.Mode.Title : Loc.T("Rush Battle");
        int seconds = rush.Mode != null ? Mathf.RoundToInt(rush.Mode.DurationSeconds) : 60;
        caption.text = Loc.T("{0} · {1} s", mode, seconds);

        list.Clear();
        foreach (var player in rules.Ranking()) list.Add(Row(rules, player));

        var waiting = new List<string>();
        var asking = new List<string>();
        foreach (var player in rules.Players)
        {
            if (player.Left || player.Slot == rules.MySlot) continue;
            if (player.WantsRematch) asking.Add(player.Name);
            else waiting.Add(player.Name);
        }

        bool alone = rules.AloneLeft;
        if (alone) note.text = Loc.T("Everyone else left.");
        else if (me != null && me.WantsRematch) note.text = waiting.Count > 0 ? Loc.T("Waiting for {0}…", string.Join(", ", waiting)) : Loc.T("Starting…");
        else if (asking.Count > 0) note.text = asking.Count == 1 ? Loc.T("{0} wants to play again!", asking[0]) : Loc.T("{0} want to play again!", string.Join(", ", asking));
        else note.text = string.Empty;

        rematchButton.SetEnabled(!alone && me != null && !me.WantsRematch);
    }

    private VisualElement Row(RushBattleRules rules, RushBattlePlayer player)
    {
        int place = rules.PlaceOf(player.Slot);
        var row = UiFactory.Element("rank-row");
        row.EnableInClassList("rank-row--me", player.Slot == rules.MySlot);
        row.EnableInClassList("rank-row--left", player.Left);

        row.Add(UiFactory.Text(player.Left ? "–" : place.ToString(), place == 1 && !player.Left ? "rank-row__place rank-row__place--gold" : "rank-row__place"));

        var look = cosmetics != null && !string.IsNullOrEmpty(player.Look) ? cosmetics.CatalogAsset.Find(player.Look) : null;
        if (look == null && cosmetics != null) look = cosmetics.CatalogAsset.DefaultFor(CosmeticCategory.Ball);
        row.Add(UiFactory.Picture(look != null ? look.Idle : null, "rank-row__ball"));

        string name = player.Slot == rules.MySlot ? Loc.T("{0} (you)", player.Name) : player.Name;
        row.Add(UiFactory.Text(player.Left ? Loc.T("{0} · left", name) : name, "rank-row__name"));
        row.Add(UiFactory.Text(player.Score.ToString(), "rank-row__score"));
        return row;
    }
}
