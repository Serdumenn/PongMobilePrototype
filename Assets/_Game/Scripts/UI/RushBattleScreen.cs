using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class RushBattleScreen : UIScreen
{
    private const string DialogHiddenClass = "dialog--hidden";
    private const long NoteMs = 1800;

    private readonly CosmeticsService cosmetics;
    private readonly VisualElement fog;
    private readonly VisualElement rivals;
    private readonly VisualElement banner;
    private readonly VisualElement bannerIcon;
    private readonly Label bannerText;
    private readonly Label note;
    private readonly Label wait;
    private readonly Label count;
    private readonly VisualElement leaveDialog;
    private readonly ReactionView reactions;

    private OnlineRushController rush;
    private IVisualElementScheduledItem noteJob;

    public RushBattleScreen(VisualElement root, CosmeticsService cosmetics, Action onLeave) : base(root)
    {
        this.cosmetics = cosmetics;

        fog = root.Q("fog");
        rivals = root.Q("rivals");
        banner = root.Q("attack-banner");
        bannerIcon = root.Q("attack-icon");
        bannerText = root.Q<Label>("attack-text");
        note = root.Q<Label>("rush-note");
        wait = root.Q<Label>("rush-wait");
        count = root.Q<Label>("rush-count");
        leaveDialog = root.Q("leave-dialog");
        reactions = new ReactionView(root, id => rush?.SendReaction(id));

        Bind("leave-confirm", () =>
        {
            CloseLeave();
            onLeave?.Invoke();
        });
        Bind("leave-cancel", CloseLeave);
        root.Q("leave-scrim").RegisterCallback<ClickEvent>(_ => CloseLeave());
    }

    public bool IsLeaveOpen => !leaveDialog.ClassListContains(DialogHiddenClass);

    public void OpenLeave()
    {
        leaveDialog.RemoveFromClassList(DialogHiddenClass);
    }

    public void CloseLeave()
    {
        leaveDialog.AddToClassList(DialogHiddenClass);
    }

    public void Present(OnlineRushController controller)
    {
        if (rush != controller)
        {
            Unsubscribe();
            rush = controller;
            rush.StateChanged += OnState;
            rush.CountdownTick += ShowCount;
            rush.ScoresChanged += RefreshRivals;
            rush.AttackIncoming += OnIncoming;
            rush.AttackActive += OnActive;
            rush.AttackSent += OnSent;
            rush.AttackBlocked += OnBlocked;
            rush.MyAttackBlocked += OnMyAttackBlocked;
            rush.ReactionReceived += OnReaction;
        }

        count.style.display = DisplayStyle.None;
        wait.style.display = DisplayStyle.None;
        banner.AddToClassList("attack-banner--hidden");
        fog.AddToClassList("fog--hidden");
        note.AddToClassList("rush-note--hidden");
        RefreshRivals();
        Show();
    }

    protected override void OnHide()
    {
        CloseLeave();
        reactions.Close();
        fog.AddToClassList("fog--hidden");
        banner.AddToClassList("attack-banner--hidden");
    }

    private void Unsubscribe()
    {
        if (rush == null) return;
        rush.StateChanged -= OnState;
        rush.CountdownTick -= ShowCount;
        rush.ScoresChanged -= RefreshRivals;
        rush.AttackIncoming -= OnIncoming;
        rush.AttackActive -= OnActive;
        rush.AttackSent -= OnSent;
        rush.AttackBlocked -= OnBlocked;
        rush.MyAttackBlocked -= OnMyAttackBlocked;
        rush.ReactionReceived -= OnReaction;
    }

    private void OnState(OnlineRushController.RushState state)
    {
        wait.style.display = state == OnlineRushController.RushState.Finished ? DisplayStyle.Flex : DisplayStyle.None;
        if (state != OnlineRushController.RushState.Playing)
        {
            banner.AddToClassList("attack-banner--hidden");
            fog.AddToClassList("fog--hidden");
        }
    }

    private void ShowCount(int value)
    {
        count.text = value.ToString();
        count.style.display = value > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (value > 0) HapticManager.Soft();
    }

    private void RefreshRivals()
    {
        rivals.Clear();
        var rules = rush?.Rules;
        if (rules == null) return;

        var ranked = rules.Ranking();
        byte leader = ranked.Count > 0 ? ranked[0].Slot : (byte)255;

        foreach (var player in ranked)
        {
            if (player.Slot == rules.MySlot) continue;

            var row = UiFactory.Element("rival");
            row.EnableInClassList("rival--leader", player.Slot == leader && !player.Left);
            row.EnableInClassList("rival--done", player.Finished);
            row.EnableInClassList("rival--left", player.Left);
            row.Add(UiFactory.Picture(Look(player.Look)?.Idle, "rival__ball"));
            row.Add(UiFactory.Text(player.Name, "rival__name"));
            row.Add(UiFactory.Text(player.Left ? "–" : player.Score.ToString(), "rival__score"));
            rivals.Add(row);
        }
    }

    private void OnIncoming(RushAttack kind, string from)
    {
        SetIcon(bannerIcon, kind);
        bannerText.text = Incoming(kind);
        banner.RemoveFromClassList("attack-banner--hidden");
        Note(Loc.T("From {0}", from));
    }

    private void OnActive(RushAttack kind, bool on)
    {
        banner.AddToClassList("attack-banner--hidden");
        if (kind == RushAttack.Fog) fog.EnableInClassList("fog--hidden", !on);
        if (on && kind != RushAttack.Fog) Note(kind == RushAttack.MiniPaddle ? Loc.T("Mini paddle for 5 s") : Loc.T("Fast ball for 5 s"));
    }

    private void OnSent(RushAttack kind, string target)
    {
        Note(Sent(kind, target));
    }

    private void OnBlocked(string attacker)
    {
        banner.AddToClassList("attack-banner--hidden");
        Note(Loc.T("Blocked!"));
    }

    private void OnMyAttackBlocked(string target)
    {
        Note(Loc.T("{0} blocked your attack", target));
    }

    private void OnReaction(string from, byte id)
    {
        Vector2 size = reactions.LayerSize;
        reactions.Receive(id, size.x - 420f, size.y * 0.36f);
    }

    private void Note(string text)
    {
        note.text = text;
        note.RemoveFromClassList("rush-note--hidden");
        noteJob?.Pause();
        noteJob = note.schedule.Execute(() => note.AddToClassList("rush-note--hidden")).StartingIn(NoteMs);
    }

    private static string Incoming(RushAttack kind)
    {
        return kind switch
        {
            RushAttack.MiniPaddle => Loc.T("Mini paddle incoming! Hit a Perfect to block"),
            RushAttack.FastBall => Loc.T("Fast ball incoming! Hit a Perfect to block"),
            _ => Loc.T("Fog incoming! Hit a Perfect to block")
        };
    }

    private static string Sent(RushAttack kind, string target)
    {
        return kind switch
        {
            RushAttack.MiniPaddle => Loc.T("Mini paddle sent to {0}!", target),
            RushAttack.FastBall => Loc.T("Fast ball sent to {0}!", target),
            _ => Loc.T("Fog sent to {0}!", target)
        };
    }

    private static void SetIcon(VisualElement icon, RushAttack kind)
    {
        icon.EnableInClassList("icon--mini-paddle", kind == RushAttack.MiniPaddle);
        icon.EnableInClassList("icon--fast-ball", kind == RushAttack.FastBall);
        icon.EnableInClassList("icon--fog", kind == RushAttack.Fog);
    }

    private CosmeticItem Look(string id)
    {
        if (cosmetics == null) return null;
        var item = string.IsNullOrEmpty(id) ? null : cosmetics.CatalogAsset.Find(id);
        return item != null ? item : cosmetics.CatalogAsset.DefaultFor(CosmeticCategory.Ball);
    }
}
