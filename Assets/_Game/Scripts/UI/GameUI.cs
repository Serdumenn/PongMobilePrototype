using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class GameUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private LocalMatchController Match;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private OnlineService Online;
    [SerializeField] private OnlineLobby Lobby;
    [SerializeField] private OnlineMatchController OnlineMatch;
    [SerializeField] private OnlineRushController OnlineRush;

    [Header("Onboarding")]
    [SerializeField] private int HintRuns = 3;

    [Header("Rewards")]
    [SerializeField] private long RewardDelayMs = 650;

    [Header("Online")]
    [SerializeField] private float CodeLifetime = 600f;

    [Header("Layout")]
    [SerializeField] private float MaxContentWidth = 1080f;
    [SerializeField] private float HintInset = 56f;
    [SerializeField] private float ToastInset = 32f;

    private VisualElement root;
    private MenuScreen menu;
    private HudScreen hud;
    private PauseScreen pause;
    private GameOverScreen gameOver;
    private SettingsScreen settings;
    private ShopScreen shop;
    private RewardScreen reward;
    private ScoresScreen scores;
    private ToastView toast;
    private HubScreen hub;
    private MatchSetupScreen setup;
    private MatchHudScreen matchHud;
    private MatchResultScreen matchResult;
    private LobbyScreen lobbyScreen;
    private OnlineHudScreen onlineHud;
    private OnlineResultScreen onlineResult;
    private RushBattleScreen rushBattle;
    private RushResultScreen rushResult;
    private bool togetherSelected;

    private SoloGameManager.GameState lastState;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private bool hintActive;
    private bool built;

    private void Start()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Match == null) Match = FindFirstObjectByType<LocalMatchController>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (Online == null) Online = FindFirstObjectByType<OnlineService>();
        if (Lobby == null) Lobby = FindFirstObjectByType<OnlineLobby>();
        if (OnlineMatch == null) OnlineMatch = FindFirstObjectByType<OnlineMatchController>();
        if (OnlineRush == null) OnlineRush = FindFirstObjectByType<OnlineRushController>();

        root = GetComponent<UIDocument>().rootVisualElement;
        root.Query<Button>().ForEach(b => b.RemoveFromClassList(Button.ussClassName));

        menu = new MenuScreen(root.Q("menu"), OnMenuPlay, OpenSettings, OpenShop, OpenScores, StepMode);
        hud = new HudScreen(root.Q("hud"), OnHudPause);
        pause = new PauseScreen(root.Q("pause"), Resume, LeaveFromPause);
        gameOver = new GameOverScreen(root.Q("game-over"), () => ContinueAfterAd(Game.RestartRun, gameOver.SetInteractable), () => ContinueAfterAd(Game.ReturnToMenu, gameOver.SetInteractable));
        toast = new ToastView(root.Q("toast"));
        settings = new SettingsScreen(root.Q("settings"), CloseSettings, () => Game.ScoreManager.BestFor(SoloScoreManager.BestScoreKey), Game.ResetAllBests, DeleteOnlineData);
        shop = new ShopScreen(root.Q("shop"), Cosmetics, CloseShop, toast.Show);
        reward = new RewardScreen(root.Q("reward"), EquipReward, CloseReward);
        scores = new ScoresScreen(root.Q("scores"), Game, Cosmetics, CloseScores);
        hub = new HubScreen(root.Q("hub"), Match, Cosmetics, Online, Lobby, CloseHub, PickMatchMode, QuickMatch, CreateCode, JoinCode, toast.Show);
        lobbyScreen = new LobbyScreen(root.Q("lobby"), Lobby, Cosmetics, LeaveLobby, OnCodeExpired, toast.Show, CodeLifetime);
        onlineHud = new OnlineHudScreen(root.Q("online-hud"), Cosmetics, LeaveOnline);
        onlineResult = new OnlineResultScreen(root.Q("online-result"), Cosmetics, () => OnlineMatch?.RequestRematch(), LeaveOnline);
        rushBattle = new RushBattleScreen(root.Q("rush-battle"), Cosmetics, LeaveOnline);
        rushResult = new RushResultScreen(root.Q("rush-result"), Cosmetics, () => OnlineRush?.RequestRematch(), LeaveOnline);
        setup = new MatchSetupScreen(root.Q("match-setup"), Cosmetics, CancelSetup, entries => Match.Begin(entries));
        matchHud = new MatchHudScreen(root.Q("match-hud"), () => Match.SetPaused(true));
        matchResult = new MatchResultScreen(root.Q("match-result"),
            () => ContinueAfterAd(Match.Rematch, matchResult.SetInteractable),
            () => ContinueAfterAd(Match.Exit, matchResult.SetInteractable));

        root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());

        Game.StateChanged += OnStateChanged;
        Game.ModeChanged += OnModeChanged;
        Game.HitScored += OnHitScored;
        Game.ScoreManager.ScoreChanged += OnScoreChanged;
        Game.Ball.Launched += OnBallLaunched;
        if (Cosmetics != null) Cosmetics.Changed += RefreshMenuMode;
        if (Match != null)
        {
            Match.StateChanged += OnMatchState;
            Match.CountdownTick += matchHud.ShowCount;
            Match.HitScored += OnMatchHit;
            Match.PointLost += OnMatchPoint;
            Match.PlayerEliminated += OnMatchPoint;
        }
        if (Lobby != null) Lobby.Closed += OnLobbyClosed;
        if (OnlineMatch != null) OnlineMatch.StateChanged += OnOnlineState;
        if (OnlineRush != null) OnlineRush.StateChanged += OnRushState;
        if (Lobby != null) Lobby.Changed += OnLobbyChanged;

        built = true;
        lastState = Game.State;
        OnStateChanged(Game.State);
    }

    private void OnDestroy()
    {
        if (!built || Game == null) return;

        Game.StateChanged -= OnStateChanged;
        Game.ModeChanged -= OnModeChanged;
        Game.HitScored -= OnHitScored;
        if (Game.ScoreManager != null) Game.ScoreManager.ScoreChanged -= OnScoreChanged;
        if (Game.Ball != null) Game.Ball.Launched -= OnBallLaunched;
        if (Cosmetics != null) Cosmetics.Changed -= RefreshMenuMode;
        if (Match != null)
        {
            Match.StateChanged -= OnMatchState;
            Match.CountdownTick -= matchHud.ShowCount;
            Match.HitScored -= OnMatchHit;
            Match.PointLost -= OnMatchPoint;
            Match.PlayerEliminated -= OnMatchPoint;
        }
        if (Lobby != null) Lobby.Closed -= OnLobbyClosed;
        if (OnlineMatch != null) OnlineMatch.StateChanged -= OnOnlineState;
        if (OnlineRush != null) OnlineRush.StateChanged -= OnRushState;
        if (Lobby != null) Lobby.Changed -= OnLobbyChanged;
    }

    private void Update()
    {
        if (!built) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HandleBack();

        var rules = Game.Rules;
        if (Game.State == SoloGameManager.GameState.Playing && rules != null && rules.IsTimed)
            hud.SetTime(rules.TimeRemaining, rules.TimeLimit);

        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            ApplySafeArea();

        PumpOnline();
    }

    private void PumpOnline()
    {
        if (OnlineMatch == null || Lobby == null || !Lobby.InLobby) return;

        if (Lobby.Mode != null && Lobby.Mode.Kind == GameModeKind.RushBattle)
        {
            PumpRush();
            return;
        }

        bool full = Lobby.Players.Count >= Lobby.MaxPlayers;
        var state = OnlineMatch.State;

        if (state == OnlineMatchController.MatchState.Idle)
        {
            if (!lobbyScreen.IsVisible || !full) return;

            var opponent = Lobby.Opponent;
            var link = opponent.HasValue ? Lobby.CreateLink() : null;
            if (link == null) return;

            OnlineMatch.Open(link, Lobby.Mode, MyLobbyLook(), opponent.Value.Name, opponent.Value.Look);
            return;
        }

        if (state == OnlineMatchController.MatchState.Waiting)
        {
            if (!full)
            {
                CloseOnlineMatch();
                return;
            }

            if (OnlineMatch.IsHost && Lobby.AllReady && Lobby.PeerOnline) OnlineMatch.HostStart();
            return;
        }

        if (!full)
        {
            if (state == OnlineMatchController.MatchState.Result) onlineResult.SetOpponentGone();
            else OnlineMatch.OpponentLeft();
        }
    }

    private void PumpRush()
    {
        if (OnlineRush == null) return;

        switch (OnlineRush.State)
        {
            case OnlineRushController.RushState.Idle:
                if (!lobbyScreen.IsVisible || !Lobby.HasEnoughPlayers || !Lobby.EveryoneOnline) return;
                var link = Lobby.CreateLink();
                if (link == null) return;
                OnlineRush.Open(link, Lobby.Mode, Lobby.MyPlayerId, MyLobbyLook(), Entrants());
                break;

            case OnlineRushController.RushState.Waiting:
                if (!Lobby.HasEnoughPlayers)
                {
                    CloseRush();
                    return;
                }
                if (OnlineRush.IsHost && Lobby.AllReady && Lobby.EveryoneOnline) OnlineRush.HostStart();
                break;
        }
    }

    private void OnLobbyChanged()
    {
        if (OnlineRush != null && OnlineRush.IsActive && Lobby != null && Lobby.InLobby) OnlineRush.UpdateEntrants(Entrants());
    }

    private System.Collections.Generic.List<OnlineRushController.Entrant> Entrants()
    {
        var list = new System.Collections.Generic.List<OnlineRushController.Entrant>();
        foreach (var player in Lobby.Players) list.Add(new OnlineRushController.Entrant(player.Id, player.Name, player.Look));
        return list;
    }

    private void OnRushState(OnlineRushController.RushState state)
    {
        switch (state)
        {
            case OnlineRushController.RushState.Countdown:
                lobbyScreen.Hide();
                hub.Hide();
                menu.Hide();
                rushResult.Hide();
                gameOver.Hide();
                Game.ShowGameObjects();
                rushBattle.Present(OnlineRush);
                if (Lobby != null && Lobby.InLobby) _ = Lobby.SetReadyAsync(false);
                break;

            case OnlineRushController.RushState.Result:
                rushBattle.Hide();
                hud.Hide();
                rushResult.Present(OnlineRush);
                break;

            case OnlineRushController.RushState.Idle:
                rushBattle.Hide();
                rushResult.Hide();
                break;
        }
    }

    private void CloseRush()
    {
        if (OnlineRush == null || !OnlineRush.IsActive) return;
        OnlineRush.Exit();
        hud.Hide();
        Game.HideGameObjects();
    }

    private void OnHudPause()
    {
        if (OnlineRush != null && OnlineRush.IsActive)
        {
            rushBattle.OpenLeave();
            return;
        }

        Game.SetPaused(true);
    }

    private string MyLobbyLook()
    {
        foreach (var player in Lobby.Players)
            if (player.IsYou) return player.Look;
        return hub.LookId;
    }

    private void OnOnlineState(OnlineMatchController.MatchState state)
    {
        switch (state)
        {
            case OnlineMatchController.MatchState.Countdown:
                lobbyScreen.Hide();
                hub.Hide();
                onlineResult.Hide();
                onlineHud.Present(OnlineMatch);
                if (Lobby != null && Lobby.InLobby) _ = Lobby.SetReadyAsync(false);
                break;

            case OnlineMatchController.MatchState.Result:
                onlineHud.Hide();
                onlineResult.Present(OnlineMatch);
                break;

            case OnlineMatchController.MatchState.Idle:
                onlineHud.Hide();
                onlineResult.Hide();
                break;
        }
    }

    private void CloseOnlineMatch()
    {
        if (OnlineMatch == null || !OnlineMatch.IsActive) return;
        OnlineMatch.Exit();
        Game.HideGameObjects();
    }

    private void LeaveOnline()
    {
        onlineHud.Hide();
        onlineResult.Hide();
        rushBattle.Hide();
        rushResult.Hide();
        lobbyScreen.Hide();
        hub.Present(1);
        CloseOnlineMatch();
        CloseRush();
        if (Lobby != null) _ = Lobby.LeaveAsync();
    }

    private void OnModeChanged(GameModeDefinition mode)
    {
        RefreshMenuMode();
    }

    private bool HasTogether => Match != null && Match.ModeList.Count > 0;

    private void RefreshMenuMode()
    {
        var modes = Game.ModeList;
        int count = modes.Count + (HasTogether ? 1 : 0);

        if (togetherSelected && HasTogether)
        {
            var friend = Cosmetics != null ? Cosmetics.CatalogAsset.Find("ball_minty") : null;
            menu.SetTogether(modes.Count, count, EquippedBall()?.Happy, friend != null ? friend.Happy : null);
            return;
        }

        var mode = Game.CurrentMode;
        if (mode == null) return;

        int index = 0;
        for (int i = 0; i < modes.Count; i++) if (modes[i] == mode) index = i;

        Sprite art = mode.Kind == GameModeKind.Classic ? EquippedBall()?.Happy : null;
        menu.SetMode(mode, index, count, Game.BestFor(mode), Game.IsModeUnlocked(mode), art);
    }

    private void StepMode(int direction)
    {
        var modes = Game.ModeList;
        int count = modes.Count + (HasTogether ? 1 : 0);
        if (count < 2) return;

        int index = togetherSelected ? modes.Count : 0;
        if (!togetherSelected)
            for (int i = 0; i < modes.Count; i++) if (modes[i] == Game.CurrentMode) index = i;

        index = (index + direction + count) % count;
        togetherSelected = index >= modes.Count;
        if (!togetherSelected) Game.SelectMode(modes[index]);
        RefreshMenuMode();
    }

    private void OnMenuPlay()
    {
        if (togetherSelected && HasTogether) OpenHub();
        else Game.StartGameFromMenu();
    }

    private void OpenHub()
    {
        menu.Hide();
        hub.Present(0);
        Game.HideGameObjects();
    }

    private void CloseHub()
    {
        hub.Hide();
        ShowMenuFromOverlay();
    }

    private void PickMatchMode(GameModeDefinition mode)
    {
        if (!LocalMatchController.FitsScreen(mode))
        {
            toast.Show(mode.DisplayName + " needs a tablet. Gather " + mode.MinPlayers + "–" + mode.MaxPlayers + " players around a bigger screen.");
            return;
        }

        Match.Open(mode);
    }

    private void CancelSetup()
    {
        Match.Cancel();
        setup.Hide();
        hub.Present(0);
    }

    private void QuickMatch(GameModeDefinition mode)
    {
        if (mode == null) return;
        EnterLobby(() => Lobby.QuickMatchAsync(mode.Id, mode.MaxPlayers, hub.LookId), "Finding a player…");
    }

    private void CreateCode(GameModeDefinition mode)
    {
        if (mode == null) return;
        EnterLobby(() => Lobby.CreateAsync(mode.Id, mode.MaxPlayers, hub.LookId), "Creating a code…");
    }

    private void JoinCode(string code)
    {
        EnterLobby(() => Lobby.JoinAsync(code, hub.LookId), "Joining…");
    }

    private async void EnterLobby(Func<Task<OnlineLobby.Failure>> open, string busyLabel)
    {
        if (Lobby == null || Lobby.Busy) return;

        hub.SetBusy(true, busyLabel);
        var result = await open();
        hub.SetBusy(false, null);

        if (result != OnlineLobby.Failure.None)
        {
            toast.Show(FailureText(result));
            return;
        }

        if (!hub.IsVisible)
        {
            _ = Lobby.LeaveAsync();
            return;
        }

        hub.Hide();
        lobbyScreen.Present();
    }

    private void LeaveLobby()
    {
        lobbyScreen.Hide();
        hub.Present(1);
        CloseOnlineMatch();
        CloseRush();
        if (Lobby != null) _ = Lobby.LeaveAsync();
    }

    private void OnCodeExpired()
    {
        LeaveLobby();
        toast.Show("Your code expired. Make a new one any time.");
    }

    private void OnLobbyClosed(OnlineLobby.Failure reason)
    {
        if (OnlineRush != null && OnlineRush.IsActive)
        {
            bool inResult = OnlineRush.State == OnlineRushController.RushState.Result;
            if (inResult && reason == OnlineLobby.Failure.HostLeft) return;

            LeaveOnline();
            toast.Show(reason == OnlineLobby.Failure.HostLeft ? "The host left, so the battle ended." : FailureText(reason));
            return;
        }

        if (OnlineMatch != null && OnlineMatch.IsActive && OnlineMatch.State != OnlineMatchController.MatchState.Waiting)
        {
            if (reason == OnlineLobby.Failure.HostLeft)
            {
                OnlineMatch.OpponentLeft();
                onlineResult.SetOpponentGone();
                return;
            }

            LeaveOnline();
            toast.Show(FailureText(reason));
            return;
        }

        CloseOnlineMatch();
        if (!lobbyScreen.IsVisible) return;

        lobbyScreen.Hide();
        hub.Present(1);
        toast.Show(FailureText(reason));
    }

    private static string FailureText(OnlineLobby.Failure failure)
    {
        return failure switch
        {
            OnlineLobby.Failure.Offline => "You're offline. Check your connection and try again.",
            OnlineLobby.Failure.NotFound => "No match with that code. Check the letters and try again.",
            OnlineLobby.Failure.Full => "That match is already full.",
            OnlineLobby.Failure.VersionMismatch => "Your friend has a different version of Pingi Pongi. Update both and try again.",
            OnlineLobby.Failure.ConnectionLost => "Connection lost, so you left the match.",
            OnlineLobby.Failure.HostLeft => "Your friend left the match.",
            _ => "Something went wrong. Please try again."
        };
    }

    private async void DeleteOnlineData()
    {
        if (Online == null) return;

        settings.SetOnlineBusy(true);
        try
        {
            bool deleted = await Online.DeleteDataAsync();
            toast.Show(deleted ? "Online data deleted. You'll get a new name next time." : "There is no online data on this device.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Deleting online data failed: {e.Message}");
            toast.Show(OnlineService.HasInternet ? "Couldn't delete right now. Please try again later." : "You're offline. Connect to delete your online data.");
        }
        settings.SetOnlineBusy(false);
    }

    private void Resume()
    {
        if (Match != null && Match.IsActive) Match.SetPaused(false);
        else Game.SetPaused(false);
    }

    private void LeaveFromPause()
    {
        if (Match != null && Match.IsActive) Match.Exit();
        else Game.ReturnToMenu();
    }

    private void OnMatchState(LocalMatchController.MatchState state)
    {
        switch (state)
        {
            case LocalMatchController.MatchState.Setup:
                menu.Hide();
                hub.Hide();
                matchResult.Hide();
                setup.Present(Match.Mode);
                break;

            case LocalMatchController.MatchState.Countdown:
                setup.Hide();
                hub.Hide();
                pause.Hide();
                reward.Hide();
                matchResult.Hide();
                matchHud.Begin(Match, FieldOnPanel);
                matchHud.Show();
                break;

            case LocalMatchController.MatchState.Playing:
                pause.Hide();
                matchHud.Show();
                break;

            case LocalMatchController.MatchState.Paused:
                pause.SetMascot(EquippedBall()?.Idle);
                pause.Show();
                break;

            case LocalMatchController.MatchState.Result:
                matchHud.Hide();
                pause.Hide();
                matchHud.Refresh();
                matchResult.Present(Match);
                root.schedule.Execute(ShowNextReward).StartingIn(RewardDelayMs);
                break;

            default:
                setup.Hide();
                matchHud.Hide();
                matchResult.Hide();
                pause.Hide();
                break;
        }
    }

    private void OnMatchHit(HitResult hit)
    {
        matchHud.Refresh();
    }

    private void OnMatchPoint(Participant participant)
    {
        matchHud.Refresh();
    }

    private Rect FieldOnPanel()
    {
        var cam = Camera.main;
        if (cam == null || Layout == null || root?.panel == null || Screen.width <= 0 || Screen.height <= 0) return default;

        Vector2 size = root.layout.size;
        if (float.IsNaN(size.x) || size.x <= 0f) return default;

        Rect f = Layout.Field;
        Vector3 a = cam.WorldToScreenPoint(new Vector3(f.xMin, f.yMax, 0f));
        Vector3 b = cam.WorldToScreenPoint(new Vector3(f.xMax, f.yMin, 0f));
        float sx = size.x / Screen.width;
        float sy = size.y / Screen.height;
        return Rect.MinMaxRect(a.x * sx, (Screen.height - a.y) * sy, b.x * sx, (Screen.height - b.y) * sy);
    }

    private void OnHitScored(HitResult hit)
    {
        hud.ShowHit(hit);
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        var previous = lastState;
        lastState = state;

        switch (state)
        {
            case SoloGameManager.GameState.Menu:
                hud.Hide();
                pause.Hide();
                gameOver.Hide();
                reward.Hide();
                if (!settings.IsVisible && !shop.IsVisible && !scores.IsVisible && !hub.IsVisible && !lobbyScreen.IsVisible
                    && !onlineHud.IsVisible && !onlineResult.IsVisible && !rushBattle.IsVisible && !rushResult.IsVisible
                    && (Match == null || !Match.IsActive) && (OnlineMatch == null || !OnlineMatch.IsActive) && (OnlineRush == null || !OnlineRush.IsActive))
                {
                    RefreshMenuMode();
                    menu.Show();
                }
                break;

            case SoloGameManager.GameState.Playing:
                menu.Hide();
                pause.Hide();
                gameOver.Hide();
                reward.Hide();
                if (previous != SoloGameManager.GameState.Paused) BeginRun();
                hud.Show();
                break;

            case SoloGameManager.GameState.Paused:
                pause.SetMascot(EquippedBall()?.Idle);
                pause.Show();
                break;

            case SoloGameManager.GameState.GameOver:
                pause.Hide();
                SetHint(false);
                if (Game.BattleRun) break;
                hud.Hide();
                ShowGameOverResult();
                gameOver.SetInteractable(true);
                root.schedule.Execute(ShowNextReward).StartingIn(RewardDelayMs);
                break;
        }
    }

    private CosmeticItem EquippedBall()
    {
        return Cosmetics != null ? Cosmetics.Equipped(CosmeticCategory.Ball) : null;
    }

    private void ShowGameOverResult()
    {
        var ball = EquippedBall();
        var score = Game.ScoreManager;
        gameOver.SetResult(Game.CurrentMode?.DisplayName, score.Score, score.BestScore, score.PreviousBest, score.IsNewBest, ball?.Happy, ball?.Sad);
        gameOver.Show();
    }

    private void BeginRun()
    {
        var rules = Game.Rules;
        bool timed = rules != null && rules.IsTimed;
        hud.SetTimed(timed);
        if (timed) hud.SetTime(rules.TimeRemaining, rules.TimeLimit);
        hud.SetScore(Game.ScoreManager.Score, false);
        SetHint(GameSettings.HintRunsShown < HintRuns);
    }

    private void OnScoreChanged(int value)
    {
        hud.SetScore(value, value > 0);
    }

    private void OnBallLaunched()
    {
        if (!hintActive) return;

        GameSettings.HintRunsShown++;
        SetHint(false);
    }

    private void SetHint(bool visible)
    {
        hintActive = visible;
        hud.SetHintVisible(visible);
    }

    private bool InMatchResult => Match != null && Match.State == LocalMatchController.MatchState.Result;

    private void ShowNextReward()
    {
        if (Cosmetics == null || reward.IsVisible) return;
        if (Game.State != SoloGameManager.GameState.GameOver && !InMatchResult) return;

        var item = Cosmetics.TakeNextReward();
        if (item == null) return;

        gameOver.Hide();
        matchResult.Hide();
        reward.Present(item);
    }

    private void EquipReward(CosmeticItem item)
    {
        if (Cosmetics != null && item != null) Cosmetics.Equip(item);
        CloseReward();
    }

    private void CloseReward()
    {
        reward.Hide();

        if (Cosmetics != null && Cosmetics.PendingRewards.Count > 0)
        {
            root.schedule.Execute(ShowNextReward).StartingIn(RewardDelayMs / 2);
            return;
        }

        if (InMatchResult)
        {
            matchResult.Show();
            return;
        }

        if (Game.State != SoloGameManager.GameState.GameOver) return;

        ShowGameOverResult();
    }

    private void OpenSettings()
    {
        menu.Hide();
        settings.Show();
        Game.HideGameObjects();
    }

    private void CloseSettings()
    {
        settings.Hide();
        ShowMenuFromOverlay();
    }

    private void OpenShop()
    {
        menu.Hide();
        shop.Show();
        Game.HideGameObjects();
    }

    private void CloseShop()
    {
        shop.Hide();
        ShowMenuFromOverlay();
    }

    private void OpenScores()
    {
        menu.Hide();
        scores.Show();
        Game.HideGameObjects();
    }

    private void CloseScores()
    {
        scores.Hide();
        ShowMenuFromOverlay();
    }

    private void ShowMenuFromOverlay()
    {
        RefreshMenuMode();
        menu.Show();
        Game.ShowGameObjects();
    }

    private void ContinueAfterAd(Action next, Action<bool> setInteractable)
    {
        setInteractable?.Invoke(false);

        var ads = AdManager.Instance;
        if (ads == null)
        {
            next();
            return;
        }

        ads.ShowInterstitialThen(next);
    }

    private void HandleBack()
    {
        if (reward.IsVisible)
        {
            CloseReward();
            return;
        }

        if (shop.IsVisible)
        {
            if (shop.IsDialogOpen) shop.CloseDialog();
            else CloseShop();
            return;
        }

        if (scores.IsVisible)
        {
            CloseScores();
            return;
        }

        if (settings.IsVisible)
        {
            CloseSettings();
            return;
        }

        if (onlineResult.IsVisible || rushResult.IsVisible)
        {
            LeaveOnline();
            return;
        }

        if (rushBattle.IsVisible)
        {
            if (rushBattle.IsLeaveOpen) rushBattle.CloseLeave();
            else rushBattle.OpenLeave();
            return;
        }

        if (onlineHud.IsVisible)
        {
            if (onlineHud.IsLeaveOpen) onlineHud.CloseLeave();
            else onlineHud.OpenLeave();
            return;
        }

        if (lobbyScreen.IsVisible)
        {
            LeaveLobby();
            return;
        }

        if (hub.IsVisible)
        {
            if (hub.IsJoinOpen) hub.CloseJoin();
            else CloseHub();
            return;
        }

        if (Match != null && Match.IsActive)
        {
            switch (Match.State)
            {
                case LocalMatchController.MatchState.Setup:
                    CancelSetup();
                    break;
                case LocalMatchController.MatchState.Playing:
                    Match.SetPaused(true);
                    break;
                case LocalMatchController.MatchState.Paused:
                    Match.SetPaused(false);
                    break;
                case LocalMatchController.MatchState.Result:
                    ContinueAfterAd(Match.Exit, matchResult.SetInteractable);
                    break;
            }
            return;
        }

        switch (Game.State)
        {
            case SoloGameManager.GameState.Playing:
                Game.SetPaused(true);
                break;
            case SoloGameManager.GameState.Paused:
                Game.SetPaused(false);
                break;
            case SoloGameManager.GameState.GameOver:
                ContinueAfterAd(Game.ReturnToMenu, gameOver.SetInteractable);
                break;
        }
    }

    private void ApplySafeArea()
    {
        if (root?.panel == null) return;

        Vector2 size = root.layout.size;
        if (float.IsNaN(size.x) || size.x <= 0f) return;

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect area = Screen.safeArea;
        float toPanelX = size.x / Screen.width;
        float toPanelY = size.y / Screen.height;

        float left = Mathf.Max(0f, area.xMin * toPanelX);
        float right = Mathf.Max(0f, (Screen.width - area.xMax) * toPanelX);
        float top = Mathf.Max(0f, (Screen.height - area.yMax) * toPanelY);
        float bottom = Mathf.Max(0f, area.yMin * toPanelY);

        float column = Mathf.Max(0f, (size.x - left - right - MaxContentWidth) * 0.5f);
        left += column;
        right += column;

        root.Query(className: "safe").ForEach(e =>
        {
            e.style.paddingLeft = left;
            e.style.paddingTop = top;
            e.style.paddingRight = right;
            e.style.paddingBottom = bottom;
        });

        root.Query(className: "hint").ForEach(e =>
        {
            e.style.left = HintInset + left;
            e.style.right = HintInset + right;
        });

        matchHud?.Arrange();

        var toastRoot = root.Q("toast");
        if (toastRoot == null) return;
        toastRoot.style.top = ToastInset + top;
        toastRoot.style.left = left;
        toastRoot.style.right = right;
    }
}
