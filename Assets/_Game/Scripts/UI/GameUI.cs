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
    [SerializeField] private OnlineScores Scores;
    [SerializeField] private OnlineGhosts Ghosts;
    [SerializeField] private GhostRecorder Recorder;
    [SerializeField] private GhostRace Race;
    [SerializeField] private OnlineFriends Friends;
    [SerializeField] private CloudProgress Cloud;

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
    private GhostRaceScreen ghostRace;
    private GhostResultScreen ghostResult;
    private GhostCodeScreen ghostCode;
    private FriendsScreen friendsScreen;
    private InviteBanner inviteBanner;
    private string pendingInvite;
    private string lastOpponentId;
    private GhostRun raceRun;
    private readonly UiLocalizer localizer = new UiLocalizer();
    private TextFit textFit;
    private bool togetherSelected;
    private int dailyScore;
    private int dailyBest;
    private bool dailyNewBest;
    private string dailyLabel;

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
        if (Scores == null) Scores = FindFirstObjectByType<OnlineScores>();
        if (Ghosts == null) Ghosts = FindFirstObjectByType<OnlineGhosts>();
        if (Recorder == null) Recorder = FindFirstObjectByType<GhostRecorder>();
        if (Race == null) Race = FindFirstObjectByType<GhostRace>();
        if (Friends == null) Friends = FindFirstObjectByType<OnlineFriends>();
        if (Cloud == null) Cloud = FindFirstObjectByType<CloudProgress>();

        root = GetComponent<UIDocument>().rootVisualElement;
        root.Query<Button>().ForEach(b => b.RemoveFromClassList(Button.ussClassName));
        localizer.Capture(root);
        localizer.Apply();
        textFit = new TextFit(root);
        Loc.Changed += OnLanguageChanged;

        menu = new MenuScreen(root.Q("menu"), OnMenuPlay, OpenSettings, OpenShop, OpenScores, StepMode);
        hud = new HudScreen(root.Q("hud"), OnHudPause);
        pause = new PauseScreen(root.Q("pause"), Resume, LeaveFromPause);
        gameOver = new GameOverScreen(root.Q("game-over"), () => ContinueAfterAd(RetryRun, gameOver.SetInteractable), () => ContinueAfterAd(HomeFromGameOver, gameOver.SetInteractable));
        toast = new ToastView(root.Q("toast"));
        settings = new SettingsScreen(root.Q("settings"), CloseSettings, () => Game.ScoreManager.BestFor(SoloScoreManager.BestScoreKey), ResetBests, DeleteOnlineData,
            Online, toast.Show);
        shop = new ShopScreen(root.Q("shop"), Cosmetics, CloseShop, toast.Show);
        reward = new RewardScreen(root.Q("reward"), EquipReward, CloseReward);
        scores = new ScoresScreen(root.Q("scores"), Game, Cosmetics, Scores, CloseScores, Friends, OpenFriends);
        hub = new HubScreen(root.Q("hub"), Match, Cosmetics, Online, Lobby, Scores, CloseHub, PickMatchMode, QuickMatch, CreateCode, JoinCode, StartDaily,
            () => Recorder != null && Recorder.LastRun != null, ShareLastRun, RaceGhostCode, toast.Show, Friends, OpenFriends);
        ghostRace = new GhostRaceScreen(root.Q("ghost-race"));
        ghostResult = new GhostResultScreen(root.Q("ghost-result"), SendRunBack,
            () => ContinueAfterAd(RetryGhost, ghostResult.SetInteractable),
            () => ContinueAfterAd(HomeFromGhost, ghostResult.SetInteractable));
        ghostCode = new GhostCodeScreen(root.Q("ghost-code"), toast.Show, () => ghostCode.Hide());
        lobbyScreen = new LobbyScreen(root.Q("lobby"), Lobby, Cosmetics, LeaveLobby, OnCodeExpired, toast.Show, CodeLifetime, OpenFriends);
        onlineHud = new OnlineHudScreen(root.Q("online-hud"), Cosmetics, LeaveOnline);
        onlineResult = new OnlineResultScreen(root.Q("online-result"), Cosmetics, () => OnlineMatch?.RequestRematch(), LeaveOnline, Friends, () => lastOpponentId, toast.Show);
        rushBattle = new RushBattleScreen(root.Q("rush-battle"), Cosmetics, LeaveOnline);
        rushResult = new RushResultScreen(root.Q("rush-result"), Cosmetics, () => OnlineRush?.RequestRematch(), LeaveOnline, Friends, toast.Show);
        friendsScreen = new FriendsScreen(root.Q("friends"), Friends, Online, CloseFriends, InviteFriend, toast.Show);
        inviteBanner = new InviteBanner(root.Q("invite-banner"));
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
        if (Race != null) Race.GhostScoreChanged += OnGhostScore;
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
        if (Friends != null) Friends.RequestReceived += OnFriendRequest;

        built = true;
        lastState = Game.State;
        OnStateChanged(Game.State);
    }

    private void OnDestroy()
    {
        Loc.Changed -= OnLanguageChanged;
        if (!built || Game == null) return;

        Game.StateChanged -= OnStateChanged;
        Game.ModeChanged -= OnModeChanged;
        Game.HitScored -= OnHitScored;
        if (Game.ScoreManager != null) Game.ScoreManager.ScoreChanged -= OnScoreChanged;
        if (Game.Ball != null) Game.Ball.Launched -= OnBallLaunched;
        if (Race != null) Race.GhostScoreChanged -= OnGhostScore;
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
        if (Friends != null) Friends.RequestReceived -= OnFriendRequest;
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
        PumpFriends();
    }

    private bool IsBusy()
    {
        if (Game.State == SoloGameManager.GameState.Playing || Game.State == SoloGameManager.GameState.Paused) return true;
        if (OnlineMatch != null && OnlineMatch.IsActive) return true;
        if (OnlineRush != null && OnlineRush.IsActive) return true;
        if (Match != null && Match.IsActive) return true;
        return Lobby != null && Lobby.InLobby;
    }

    private void PumpFriends()
    {
        if (Friends == null) return;

        bool busy = IsBusy();
        Friends.SetPlaying(busy);
        if (busy || inviteBanner.IsVisible || !Friends.HasInvite || reward.IsVisible) return;
        if (Friends.TryTakeInvite(out var invite)) ShowInvite(invite);
    }

    private void ShowInvite(OnlineFriends.Invite invite)
    {
        var mode = Lobby != null ? Lobby.FindMode(invite.ModeId) : null;
        inviteBanner.Present(invite.FromName, mode != null ? mode.Title : Loc.T("Online"), () => AcceptInvite(invite));
    }

    private void AcceptInvite(OnlineFriends.Invite invite)
    {
        if (IsBusy())
        {
            toast.Show(Loc.T("Finish your game first, then join {0}.", invite.FromName));
            return;
        }

        settings.Hide();
        shop.Hide();
        scores.Hide();
        friendsScreen.Hide();
        ghostCode.Hide();
        ghostResult.Hide();
        gameOver.Hide();
        if (Game.State != SoloGameManager.GameState.Menu) Game.ReturnToMenu();
        menu.Hide();
        Game.HideGameObjects();
        hub.Present(1);
        JoinCode(invite.Code);
    }

    private void OnFriendRequest(string name)
    {
        if (!IsBusy()) toast.Show(Loc.T("{0} sent you a friend request", name));
    }

    private void ResetBests()
    {
        Game.ResetAllBests();
        if (Cloud != null) Cloud.BestsReset();
    }

    private void OpenFriends()
    {
        friendsScreen.Show();
    }

    private void CloseFriends()
    {
        friendsScreen.Hide();
    }

    private async void InviteFriend(string playerId)
    {
        if (Friends == null || string.IsNullOrEmpty(playerId)) return;

        if (Lobby != null && Lobby.InLobby)
        {
            if (string.IsNullOrEmpty(Lobby.Code) || Lobby.Players.Count >= Lobby.MaxPlayers)
            {
                toast.Show(Loc.T("That match is already full."));
                return;
            }
            await SendInvite(playerId);
            return;
        }

        var mode = hub.SelectedMode ?? FirstOnlineMode();
        if (mode == null) return;

        pendingInvite = playerId;
        friendsScreen.Hide();
        if (!hub.IsVisible)
        {
            scores.Hide();
            menu.Hide();
            Game.HideGameObjects();
            hub.Present(1);
        }
        CreateCode(mode);
    }

    private async System.Threading.Tasks.Task SendInvite(string playerId)
    {
        var result = await Friends.InviteAsync(playerId, Lobby.Code, Lobby.ModeId);
        toast.Show(result == FriendsResult.Ok ? Loc.T("Invite sent to {0}", PlayerNames.ForId(playerId)) : FriendsScreen.ResultText(result, null));
    }

    private GameModeDefinition FirstOnlineMode()
    {
        if (Lobby == null) return null;
        foreach (var mode in Lobby.ModeList)
            if (mode != null && !mode.ComingSoon) return mode;
        return null;
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

            lastOpponentId = opponent.Value.Id;
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
                friendsScreen.Hide();
                inviteBanner.Hide();
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
                friendsScreen.Hide();
                inviteBanner.Hide();
                lobbyScreen.Hide();
                hub.Hide();
                menu.Hide();
                gameOver.Hide();
                pause.Hide();
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

    private void OnLanguageChanged()
    {
        localizer.Apply();
        textFit.ResetAll();
        RefreshMenuMode();
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

    private void OpenHubAt(int tab)
    {
        menu.Hide();
        hub.Present(tab);
        Game.HideGameObjects();
    }

    private void StartDaily()
    {
        var rush = Game.FindMode("rush");
        if (rush == null) return;

        hub.Hide();
        gameOver.Hide();
        Game.StartDailyRun(rush, DailyChallenge.SeedFor(DateTime.UtcNow));
    }

    private async void ShareLastRun()
    {
        var run = Recorder != null ? Recorder.LastRun : null;
        if (run == null || Ghosts == null || hub.GhostBusy) return;

        hub.SetGhostBusy(true, null, Loc.T("Creating a code…"));
        var shared = await Ghosts.ShareAsync(run);
        hub.SetGhostBusy(false);
        ShowShared(shared);
    }

    private async void SendRunBack()
    {
        if (raceRun == null || Ghosts == null) return;

        ghostResult.SetSending(true);
        var shared = await Ghosts.ShareAsync(raceRun);
        ghostResult.SetSending(false);
        if (ghostResult.IsVisible) ShowShared(shared);
    }

    private void ShowShared(OnlineGhosts.Shared shared)
    {
        if (shared.Outcome == OnlineGhosts.Outcome.Ok)
        {
            ghostCode.Present(shared.Code, shared.Days);
            return;
        }
        toast.Show(GhostFailureText(shared.Outcome));
    }

    private async void RaceGhostCode(string code)
    {
        if (Ghosts == null || hub.GhostBusy) return;

        hub.SetGhostBusy(true, Loc.T("Finding the ghost…"));
        var loaded = await Ghosts.LoadAsync(code);
        hub.SetGhostBusy(false);

        if (loaded.Outcome != OnlineGhosts.Outcome.Ok)
        {
            toast.Show(GhostFailureText(loaded.Outcome));
            return;
        }

        if (hub.IsVisible) StartGhostRace(loaded.Run, loaded.Name);
    }

    private void StartGhostRace(GhostRun ghost, string ghostName)
    {
        if (Race == null || ghost == null) return;

        hub.Hide();
        gameOver.Hide();
        ghostResult.Hide();
        ghostCode.Hide();
        Race.Begin(ghost, ghostName);
    }

    private void RetryGhost()
    {
        ghostResult.Hide();
        if (Race == null || !Race.Retry()) HomeFromGhost();
    }

    private void HomeFromGhost()
    {
        ghostResult.Hide();
        ghostRace.Hide();
        if (Race != null) Race.End();
        Game.ReturnToMenu();
        OpenHubAt(2);
    }

    private void FinishGhost()
    {
        ghostRace.Hide();
        raceRun = Recorder != null ? Recorder.FinishNow() : null;
        ShowGhostResult();
    }

    private void ShowGhostResult()
    {
        if (Race == null || Race.Ghost == null) return;
        ghostResult.Present(Game.ScoreManager.Score, raceRun, Race.Ghost, Race.GhostName, EquippedBall());
    }

    private void OnGhostScore(int value)
    {
        RefreshDelta();
    }

    private void RefreshDelta()
    {
        if (Race == null || !Race.Racing || Game.State == SoloGameManager.GameState.GameOver)
        {
            hud.SetDelta(null);
            return;
        }

        int delta = Game.ScoreManager.Score - Race.GhostScore;
        hud.SetDelta(delta);
        ghostRace.SetLeading(delta < 0);
    }

    private static string GhostFailureText(OnlineGhosts.Outcome outcome)
    {
        return outcome switch
        {
            OnlineGhosts.Outcome.Offline => Loc.T("You're offline. Connect to race or share ghosts."),
            OnlineGhosts.Outcome.NotFound => Loc.T("No ghost with that code. Check the letters and try again."),
            OnlineGhosts.Outcome.Expired => Loc.T("That ghost code has expired. Ask your friend for a new one."),
            OnlineGhosts.Outcome.Rejected => Loc.T("This run can't be shared. Play another Rush run and try again."),
            _ => Loc.T("Ghosts are taking a break. Try again in a little while.")
        };
    }

    private void RetryRun()
    {
        if (Game.DailyRun) StartDaily();
        else Game.RestartRun();
    }

    private void HomeFromGameOver()
    {
        bool daily = Game.DailyRun;
        Game.ReturnToMenu();
        if (daily) OpenHubAt(2);
    }

    private void FinishDaily()
    {
        var now = DateTime.UtcNow;
        dailyScore = Game.ScoreManager.Score;
        dailyNewBest = DailyChallenge.Record(dailyScore, now);
        dailyBest = DailyChallenge.BestToday(now);
        dailyLabel = DailyChallenge.Label(now);
        ShowDailyResult();
        _ = SubmitDaily(dailyScore);
    }

    private void ShowDailyResult()
    {
        var ball = EquippedBall();
        gameOver.SetDaily(dailyLabel, dailyScore, dailyBest, dailyNewBest, ball?.Happy, ball?.Sad);
        gameOver.Show();
    }

    private async Task SubmitDaily(int score)
    {
        if (Scores == null || score <= 0) return;
        if (!await Scores.SubmitAsync(OnlineScores.DailyBoard, score, null, true)) return;

        int rank = await Scores.RankAsync(OnlineScores.DailyBoard);
        if (rank > 0 && gameOver.IsVisible && Game.DailyRun) gameOver.SetBestLine(Loc.T("Today's best {0} · Rank #{1}", dailyBest, rank));
    }

    private void SubmitSoloBest()
    {
        var score = Game.ScoreManager;
        if (Scores == null || Game.Kind != SoloGameManager.RunKind.Normal || score == null || !score.IsNewBest) return;
        _ = Scores.SubmitAsync(OnlineScores.BoardFor(Game.RunMode), score.Score);
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
            toast.Show(Loc.T("{0} needs a tablet. Gather {1}–{2} players around a bigger screen.", mode.Title, mode.MinPlayers, mode.MaxPlayers));
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
        EnterLobby(() => Lobby.QuickMatchAsync(mode.Id, mode.MaxPlayers, hub.LookId), Loc.T("Finding a player…"));
    }

    private void CreateCode(GameModeDefinition mode)
    {
        if (mode == null) return;
        EnterLobby(() => Lobby.CreateAsync(mode.Id, mode.MaxPlayers, hub.LookId), Loc.T("Creating a code…"));
    }

    private void JoinCode(string code)
    {
        EnterLobby(() => Lobby.JoinAsync(code, hub.LookId), Loc.T("Joining…"));
    }

    private async void EnterLobby(Func<Task<OnlineLobby.Failure>> open, string busyLabel)
    {
        if (Lobby == null || Lobby.Busy) return;

        hub.SetBusy(true, busyLabel);
        var result = await open();
        hub.SetBusy(false, null);

        string invitee = pendingInvite;
        pendingInvite = null;

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
        if (!string.IsNullOrEmpty(invitee)) await SendInvite(invitee);
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
        toast.Show(Loc.T("Your code expired. Make a new one any time."));
    }

    private void OnLobbyClosed(OnlineLobby.Failure reason)
    {
        if (OnlineRush != null && OnlineRush.IsActive)
        {
            bool inResult = OnlineRush.State == OnlineRushController.RushState.Result;
            if (inResult && reason == OnlineLobby.Failure.HostLeft) return;

            LeaveOnline();
            toast.Show(reason == OnlineLobby.Failure.HostLeft ? Loc.T("The host left, so the battle ended.") : FailureText(reason));
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
            OnlineLobby.Failure.Offline => Loc.T("You're offline. Check your connection and try again."),
            OnlineLobby.Failure.NotFound => Loc.T("No match with that code. Check the letters and try again."),
            OnlineLobby.Failure.Full => Loc.T("That match is already full."),
            OnlineLobby.Failure.VersionMismatch => Loc.T("Your friend has a different version of Pingi Pongi. Update both and try again."),
            OnlineLobby.Failure.ConnectionLost => Loc.T("Connection lost, so you left the match."),
            OnlineLobby.Failure.HostLeft => Loc.T("Your friend left the match."),
            _ => Loc.T("Something went wrong. Please try again.")
        };
    }

    private async void DeleteOnlineData()
    {
        if (Online == null) return;

        settings.SetOnlineBusy(true);
        try
        {
            if (Friends != null) await Friends.ClearAsync();
            if (Cloud != null) await Cloud.DeleteBackupAsync();
            bool deleted = await Online.DeleteDataAsync();
            toast.Show(deleted ? Loc.T("Online data deleted. You'll get a new name next time.") : Loc.T("There is no online data on this device."));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Deleting online data failed: {e.Message}");
            toast.Show(OnlineService.HasInternet ? Loc.T("Couldn't delete right now. Please try again later.") : Loc.T("You're offline. Connect to delete your online data."));
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
        if (Match != null && Match.IsActive)
        {
            Match.Exit();
            return;
        }

        if (Game.GhostRun)
        {
            HomeFromGhost();
            return;
        }

        Game.ReturnToMenu();
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
                ghostRace.Hide();
                hud.SetDelta(null);
                if (!settings.IsVisible && !shop.IsVisible && !scores.IsVisible && !hub.IsVisible && !lobbyScreen.IsVisible && !ghostResult.IsVisible
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
                if (Game.GhostRun && Race != null && Race.Active)
                {
                    ghostRace.Present(Race, Race.GhostBall()?.Idle);
                    RefreshDelta();
                }
                else
                {
                    ghostRace.Hide();
                    hud.SetDelta(null);
                }
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
                hud.SetDelta(null);
                if (Game.DailyRun) FinishDaily();
                else if (Game.GhostRun) FinishGhost();
                else
                {
                    ShowGameOverResult();
                    SubmitSoloBest();
                }
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
        gameOver.SetResult(Game.CurrentMode?.Title, score.Score, score.BestScore, score.PreviousBest, score.IsNewBest, ball?.Happy, ball?.Sad);
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
        if (Game.GhostRun) RefreshDelta();
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
        ghostResult.Hide();
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

        if (Game.DailyRun) ShowDailyResult();
        else if (Game.GhostRun) ShowGhostResult();
        else ShowGameOverResult();
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

        if (friendsScreen.IsVisible)
        {
            if (friendsScreen.IsDialogOpen) friendsScreen.CloseDialogs();
            else CloseFriends();
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
            if (settings.IsLanguageOpen) settings.CloseLanguages();
            else CloseSettings();
            return;
        }

        if (ghostCode.IsVisible)
        {
            ghostCode.Hide();
            return;
        }

        if (ghostResult.IsVisible)
        {
            ContinueAfterAd(HomeFromGhost, ghostResult.SetInteractable);
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
                ContinueAfterAd(HomeFromGameOver, gameOver.SetInteractable);
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

        var bannerRoot = root.Q("invite-banner");
        if (bannerRoot != null)
        {
            bannerRoot.style.top = ToastInset + top;
            bannerRoot.style.left = left;
            bannerRoot.style.right = right;
        }

        var toastRoot = root.Q("toast");
        if (toastRoot == null) return;
        toastRoot.style.top = ToastInset + top;
        toastRoot.style.left = left;
        toastRoot.style.right = right;
    }
}
