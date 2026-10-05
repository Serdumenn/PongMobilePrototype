using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class OnlineHudScreen : UIScreen
{
    private const string DialogHiddenClass = "dialog--hidden";
    private const long TickMs = 250;
    private const long PortalChipMs = 2600;
    private const int WeakPingMs = 180;

    private readonly CosmeticsService cosmetics;
    private readonly Action onLeave;

    private readonly VisualElement oppBall;
    private readonly Label oppName;
    private readonly VisualElement oppLives;
    private readonly Label oppScore;
    private readonly VisualElement oppWifi;
    private readonly Label portalChip;
    private readonly Label myScore;
    private readonly Label count;
    private readonly Label serveChip;
    private readonly ReactionView reactions;
    private readonly VisualElement leaveDialog;
    private readonly VisualElement peerDialog;
    private readonly Label peerCount;
    private readonly Label peerText;

    private OnlineMatchController match;
    private IVisualElementScheduledItem ticker;
    private int shownScore = -1;

    public OnlineHudScreen(VisualElement root, CosmeticsService cosmetics, Action onLeave) : base(root)
    {
        this.cosmetics = cosmetics;
        this.onLeave = onLeave;

        oppBall = root.Q("opp-ball");
        oppName = root.Q<Label>("opp-name");
        oppLives = root.Q("opp-lives");
        oppScore = root.Q<Label>("opp-score");
        oppWifi = root.Q("opp-wifi");
        portalChip = root.Q<Label>("portal-chip");
        myScore = root.Q<Label>("my-score");
        count = root.Q<Label>("ohud-count");
        serveChip = root.Q<Label>("serve-chip");
        reactions = new ReactionView(root, id => match?.SendReaction(id));
        leaveDialog = root.Q("leave-dialog");
        peerDialog = root.Q("peer-dialog");
        peerCount = root.Q<Label>("peer-count");
        peerText = root.Q<Label>("peer-text");

        Bind("menu-button", OpenLeave);
        Bind("leave-confirm", () =>
        {
            CloseLeave();
            onLeave?.Invoke();
        });
        Bind("leave-cancel", CloseLeave);
        Bind("peer-leave", onLeave);
        root.Q("leave-scrim").RegisterCallback<ClickEvent>(_ => CloseLeave());
    }

    public bool IsLeaveOpen => !leaveDialog.ClassListContains(DialogHiddenClass);

    public void Present(OnlineMatchController controller)
    {
        if (match != controller)
        {
            Unsubscribe();
            match = controller;
            match.ScoreChanged += Refresh;
            match.CountdownTick += ShowCount;
            match.ReactionReceived += OnReaction;
            match.PeerMissingChanged += OnPeerMissing;
        }

        oppName.text = match.OpponentName;
        UiFactory.SetPicture(oppBall, Look(match.OpponentLook)?.Idle);
        portalChip.RemoveFromClassList("portal-chip--hidden");
        count.style.display = DisplayStyle.None;
        shownScore = -1;
        Refresh();
        Show();
    }

    public void OpenLeave()
    {
        leaveDialog.RemoveFromClassList(DialogHiddenClass);
    }

    public void CloseLeave()
    {
        leaveDialog.AddToClassList(DialogHiddenClass);
    }

    protected override void OnShow()
    {
        ticker?.Pause();
        ticker = Root.schedule.Execute(Tick).Every(TickMs);
        Tick();
    }

    protected override void OnHide()
    {
        ticker?.Pause();
        CloseLeave();
        reactions.Close();
        peerDialog.AddToClassList(DialogHiddenClass);
    }

    private void Unsubscribe()
    {
        if (match == null) return;
        match.ScoreChanged -= Refresh;
        match.CountdownTick -= ShowCount;
        match.ReactionReceived -= OnReaction;
        match.PeerMissingChanged -= OnPeerMissing;
    }

    private void Refresh()
    {
        if (match == null || match.Rules == null)
        {
            myScore.text = "0";
            oppScore.text = "0";
            oppLives.Clear();
            return;
        }

        bool coop = match.Coop;
        oppScore.style.display = coop ? DisplayStyle.None : DisplayStyle.Flex;
        oppLives.style.display = coop ? DisplayStyle.Flex : DisplayStyle.None;

        if (coop)
        {
            oppLives.Clear();
            int total = match.Mode != null ? match.Mode.Lives : match.Rules.Lives;
            for (int i = 0; i < total; i++) oppLives.Add(UiFactory.Element(i < match.Rules.Lives ? "life" : "life life--lost"));
        }
        else
        {
            oppScore.text = match.OpponentScore.ToString();
        }

        int mine = coop ? match.Rules.Passes : match.MyScore;
        myScore.text = mine.ToString();
        if (shownScore >= 0 && mine != shownScore)
        {
            myScore.AddToClassList("ohud-score--pop");
            myScore.schedule.Execute(() => myScore.RemoveFromClassList("ohud-score--pop")).StartingIn(60);
        }
        shownScore = mine;
    }

    private void ShowCount(int value)
    {
        if (value > 0)
        {
            count.text = value.ToString();
            count.style.display = DisplayStyle.Flex;
            myScore.style.display = DisplayStyle.None;
            HapticManager.Soft();
            return;
        }

        count.style.display = DisplayStyle.None;
        myScore.style.display = DisplayStyle.Flex;
        portalChip.schedule.Execute(() => portalChip.AddToClassList("portal-chip--hidden")).StartingIn(PortalChipMs);
    }

    private void Tick()
    {
        if (match == null) return;

        int ping = match.PingMs;
        oppWifi.EnableInClassList("opp-card__wifi--weak", ping < 0 || ping > WeakPingMs);

        bool serving = match.State == OnlineMatchController.MatchState.Playing && match.AwaitingMyServe;
        serveChip.style.display = serving ? DisplayStyle.Flex : DisplayStyle.None;

        if (match.PeerMissing) peerCount.text = Mathf.CeilToInt(match.PeerWaitRemaining).ToString();
    }

    private void OnPeerMissing(bool missing)
    {
        peerText.text = $"{match.OpponentName}'s connection dropped. We'll wait 10 seconds before ending the match.";
        peerDialog.EnableInClassList(DialogHiddenClass, !missing);
        Tick();
    }

    private void OnReaction(byte id)
    {
        reactions.Receive(id, reactions.LayerSize.x * 0.22f, 240f);
    }

    private CosmeticItem Look(string id)
    {
        if (cosmetics == null) return null;
        var item = string.IsNullOrEmpty(id) ? null : cosmetics.CatalogAsset.Find(id);
        return item != null ? item : cosmetics.CatalogAsset.DefaultFor(CosmeticCategory.Ball);
    }
}
