using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class LobbyScreen : UIScreen
{
    private const string DialogHiddenClass = "dialog--hidden";
    private const long TickMs = 250;

    private readonly OnlineLobby lobby;
    private readonly CosmeticsService cosmetics;
    private readonly Action onLeave;
    private readonly Action onExpired;
    private readonly Action<string> toast;
    private readonly float codeLifetime;
    private readonly int countdownFrom;
    private readonly long countdownStepMs;

    private readonly Label title;
    private readonly VisualElement codeCard;
    private readonly Label codeLabel;
    private readonly Label expiryLabel;
    private readonly VisualElement slots;
    private readonly VisualElement versus;
    private readonly Label count;
    private readonly Label status;
    private readonly Label modeChip;
    private readonly Button readyButton;
    private readonly Label readyLabel;
    private readonly VisualElement readyIcon;
    private readonly VisualElement reconnect;
    private readonly Label reconnectCount;

    private IVisualElementScheduledItem ticker;
    private IVisualElementScheduledItem countdownJob;
    private Label pingLabel;
    private float codeShownAt = -1f;
    private bool expired;
    private bool myReady;
    private int countdown = -1;
    private string note;

    public LobbyScreen(VisualElement root, OnlineLobby lobby, CosmeticsService cosmetics, Action onLeave, Action onExpired, Action<string> toast,
        float codeLifetime, int countdownFrom, float countdownStep) : base(root)
    {
        this.lobby = lobby;
        this.cosmetics = cosmetics;
        this.onLeave = onLeave;
        this.onExpired = onExpired;
        this.toast = toast;
        this.codeLifetime = codeLifetime;
        this.countdownFrom = countdownFrom;
        countdownStepMs = (long)(countdownStep * 1000f);

        title = root.Q<Label>("lobby-title");
        codeCard = root.Q("code-card");
        codeLabel = root.Q<Label>("code-label");
        expiryLabel = root.Q<Label>("expiry-label");
        slots = root.Q("lobby-slots");
        versus = root.Q("versus");
        count = root.Q<Label>("lobby-count");
        status = root.Q<Label>("lobby-status");
        modeChip = root.Q<Label>("mode-chip");
        readyLabel = root.Q<Label>("ready-label");
        readyIcon = root.Q("ready-icon");
        reconnect = root.Q("reconnect");
        reconnectCount = root.Q<Label>("reconnect-count");

        Bind("back-button", onLeave);
        Bind("copy-button", Copy);
        Bind("share-button", Share);
        Bind("reconnect-leave", onLeave);
        readyButton = Bind("ready-button", ToggleReady);

        if (lobby != null) lobby.Changed += OnChanged;
    }

    public bool IsReconnecting => !reconnect.ClassListContains(DialogHiddenClass);

    public void Present()
    {
        codeShownAt = Time.realtimeSinceStartup;
        expired = false;
        note = null;
        StopCountdown();
        Show();
    }

    protected override void OnShow()
    {
        ticker?.Pause();
        ticker = Root.schedule.Execute(Tick).Every(TickMs);
        Refresh();
        Tick();
    }

    protected override void OnHide()
    {
        ticker?.Pause();
        StopCountdown();
        reconnect.AddToClassList(DialogHiddenClass);
    }

    private void OnChanged()
    {
        if (IsVisible) Refresh();
    }

    private void Refresh()
    {
        if (lobby == null || !lobby.InLobby) return;

        var mode = lobby.Mode;
        var players = lobby.Players;
        bool full = players.Count >= lobby.MaxPlayers;
        string modeName = mode != null ? mode.DisplayName : "Online match";

        title.text = lobby.IsPrivate && !full ? "Private match" : modeName;
        modeChip.text = mode != null ? $"{modeName} · {Rule(mode)}" : modeName;

        codeLabel.text = lobby.Code ?? string.Empty;
        codeCard.style.display = lobby.IsPrivate && !full ? DisplayStyle.Flex : DisplayStyle.None;

        myReady = false;
        foreach (var player in players)
            if (player.IsYou) myReady = player.Ready;

        if (full && players.Count == 2) BuildVersus();
        else BuildSlots(full);

        readyButton.EnableInClassList("lobby-ready--on", myReady);
        readyLabel.text = myReady ? "Not ready" : "Ready";
        readyIcon.style.display = myReady ? DisplayStyle.None : DisplayStyle.Flex;

        if (lobby.AllReady)
        {
            if (countdown < 0) StartCountdown();
        }
        else if (countdown >= 0)
        {
            StopCountdown();
        }

        RefreshStatus(full);
    }

    private void RefreshStatus(bool full)
    {
        if (countdown >= 0)
        {
            status.text = "Get ready";
            return;
        }

        if (!string.IsNullOrEmpty(note))
        {
            status.text = note;
            return;
        }

        if (!full)
        {
            status.text = string.Empty;
            return;
        }

        string other = "your friend";
        foreach (var player in lobby.Players)
            if (!player.IsYou) other = player.Name;
        status.text = myReady ? $"Waiting for {other}…" : "Tap Ready when you are set.";
    }

    private void BuildSlots(bool full)
    {
        versus.style.display = DisplayStyle.None;
        slots.style.display = DisplayStyle.Flex;
        slots.Clear();
        pingLabel = null;

        foreach (var player in lobby.Players)
        {
            string sub = player.IsYou ? (player.IsHost ? "You · host" : "You") : (player.IsHost ? "Host" : "Joined");
            slots.Add(Slot(player, sub));
        }

        if (full) return;

        bool isPrivate = lobby.IsPrivate;
        for (int i = lobby.Players.Count; i < lobby.MaxPlayers; i++)
            slots.Add(WaitingSlot(isPrivate ? "Waiting for a friend…" : "Looking for a player…",
                isPrivate ? "They tap Join code" : "This takes a few seconds"));
    }

    private VisualElement Slot(LobbyPlayer player, string sub)
    {
        var slot = UiFactory.Element("slot");
        slot.Add(UiFactory.Picture(BallFor(player), "slot__ball"));

        var text = UiFactory.Element("slot__text");
        text.Add(UiFactory.Text(player.Name, "slot__name"));
        text.Add(UiFactory.Text(sub, "slot__sub"));
        slot.Add(text);

        slot.Add(ReadyMark(player.Ready));
        return slot;
    }

    private VisualElement WaitingSlot(string heading, string sub)
    {
        var slot = UiFactory.Element("slot slot--waiting");
        slot.Add(UiFactory.Text("•••", "slot__dots"));

        var text = UiFactory.Element("slot__text");
        text.Add(UiFactory.Text(heading, "slot__name"));
        text.Add(UiFactory.Text(sub, "slot__sub"));
        slot.Add(text);
        return slot;
    }

    private void BuildVersus()
    {
        slots.style.display = DisplayStyle.None;
        versus.style.display = DisplayStyle.Flex;
        versus.Clear();
        pingLabel = null;

        var players = lobby.Players;
        versus.Add(PlayerCard(players[0], 0));
        versus.Add(UiFactory.Text("VS", "versus__label"));
        versus.Add(PlayerCard(players[1], 1));
    }

    private VisualElement PlayerCard(LobbyPlayer player, int index)
    {
        var card = UiFactory.Element($"pcard pcard--p{index}");
        card.Add(UiFactory.Picture(BallFor(player), "pcard__ball"));
        card.Add(UiFactory.Text(player.Name, "pcard__name"));

        var sub = UiFactory.Element("pcard__sub");
        if (player.IsYou)
        {
            sub.Add(UiFactory.Text("You", "pcard__sub-label"));
        }
        else
        {
            sub.Add(UiFactory.Element("icon icon--wifi"));
            pingLabel = UiFactory.Text(PingText(), "pcard__sub-label");
            sub.Add(pingLabel);
        }
        card.Add(sub);

        card.Add(ReadyMark(player.Ready));
        return card;
    }

    private static VisualElement ReadyMark(bool ready)
    {
        var mark = UiFactory.Element(ready ? "ready-mark ready-mark--on" : "ready-mark");
        if (ready) mark.Add(UiFactory.Element("icon icon--check"));
        return mark;
    }

    private Sprite BallFor(LobbyPlayer player)
    {
        if (cosmetics == null) return null;

        var item = cosmetics.CatalogAsset.Find(player.Look);
        if (item == null) item = cosmetics.CatalogAsset.DefaultFor(CosmeticCategory.Ball);
        if (item == null) return null;
        return player.Ready ? item.Happy : item.Idle;
    }

    private string PingText()
    {
        int ping = lobby != null ? lobby.PingMs() : -1;
        return ping >= 0 ? $"{ping} ms" : "Connecting…";
    }

    private void Tick()
    {
        if (lobby == null || !lobby.InLobby) return;

        if (pingLabel != null) pingLabel.text = PingText();

        bool waitingForCode = codeCard.resolvedStyle.display == DisplayStyle.Flex;
        if (waitingForCode && !expired)
        {
            float left = Mathf.Max(0f, codeLifetime - (Time.realtimeSinceStartup - codeShownAt));
            int seconds = Mathf.CeilToInt(left);
            expiryLabel.text = $"Expires in {seconds / 60}:{seconds % 60:00}";
            if (left <= 0f)
            {
                expired = true;
                onExpired?.Invoke();
                return;
            }
        }

        bool reconnecting = lobby.Reconnecting;
        reconnect.EnableInClassList(DialogHiddenClass, !reconnecting);
        if (reconnecting) reconnectCount.text = Mathf.CeilToInt(lobby.ReconnectRemaining).ToString();
    }

    private void ToggleReady()
    {
        if (lobby == null || !lobby.InLobby) return;

        note = null;
        _ = lobby.SetReadyAsync(!myReady);
    }

    private void StartCountdown()
    {
        countdown = countdownFrom;
        ShowCount();
        countdownJob?.Pause();
        countdownJob = Root.schedule.Execute(StepCountdown).Every(countdownStepMs).StartingIn(countdownStepMs);
    }

    private void StepCountdown()
    {
        countdown--;
        if (countdown > 0)
        {
            ShowCount();
            return;
        }

        AudioManager.PlayOne(Sfx.CountdownGo);
        StopCountdown();

        var mode = lobby.Mode;
        note = $"{(mode != null ? mode.DisplayName : "Online play")} arrives in the next update. Thanks for testing!";
        _ = lobby.SetReadyAsync(false);
        RefreshStatus(true);
    }

    private void ShowCount()
    {
        count.text = countdown.ToString();
        count.style.display = DisplayStyle.Flex;
        count.AddToClassList("lobby-count--pop");
        count.schedule.Execute(() => count.RemoveFromClassList("lobby-count--pop")).StartingIn(60);
        AudioManager.PlayOne(Sfx.CountdownTick);
        HapticManager.Soft();
    }

    private void StopCountdown()
    {
        countdownJob?.Pause();
        countdownJob = null;
        countdown = -1;
        count.style.display = DisplayStyle.None;
    }

    private void Copy()
    {
        if (string.IsNullOrEmpty(lobby?.Code)) return;
        NativeShare.Copy(lobby.Code);
        toast?.Invoke("Code copied");
    }

    private void Share()
    {
        if (string.IsNullOrEmpty(lobby?.Code)) return;
        if (!NativeShare.Text($"Play Pingi Pongi with me! Tap Play together, Online, Join code and type {lobby.Code}", "Share your code"))
            toast?.Invoke("Code copied");
    }

    private static string Rule(GameModeDefinition mode)
    {
        return mode.Kind switch
        {
            GameModeKind.CoopRally => $"{mode.Lives} shared lives",
            GameModeKind.RushBattle => $"{Mathf.RoundToInt(mode.DurationSeconds)} s",
            _ => $"First to {mode.PointsToWin}"
        };
    }
}
