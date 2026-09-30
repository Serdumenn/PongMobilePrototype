using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class GameUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;

    [Header("Onboarding")]
    [SerializeField] private int HintRuns = 3;

    [Header("Rewards")]
    [SerializeField] private long RewardDelayMs = 650;

    [Header("Layout")]
    [SerializeField] private float MaxContentWidth = 1080f;
    [SerializeField] private float HintInset = 56f;

    private VisualElement root;
    private MenuScreen menu;
    private HudScreen hud;
    private PauseScreen pause;
    private GameOverScreen gameOver;
    private SettingsScreen settings;
    private ShopScreen shop;
    private RewardScreen reward;
    private ScoresScreen scores;

    private SoloGameManager.GameState lastState;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private bool hintActive;
    private bool built;

    private void Start()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();

        root = GetComponent<UIDocument>().rootVisualElement;
        root.Query<Button>().ForEach(b => b.RemoveFromClassList(Button.ussClassName));

        menu = new MenuScreen(root.Q("menu"), Game.StartGameFromMenu, OpenSettings, OpenShop, OpenScores, StepMode);
        hud = new HudScreen(root.Q("hud"), () => Game.SetPaused(true));
        pause = new PauseScreen(root.Q("pause"), () => Game.SetPaused(false), Game.ReturnToMenu);
        gameOver = new GameOverScreen(root.Q("game-over"), () => ContinueAfterAd(Game.RestartRun), () => ContinueAfterAd(Game.ReturnToMenu));
        settings = new SettingsScreen(root.Q("settings"), CloseSettings, () => Game.ScoreManager.BestFor(SoloScoreManager.BestScoreKey), Game.ResetAllBests);
        shop = new ShopScreen(root.Q("shop"), Cosmetics, CloseShop);
        reward = new RewardScreen(root.Q("reward"), EquipReward, CloseReward);
        scores = new ScoresScreen(root.Q("scores"), Game, Cosmetics, CloseScores);

        root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());

        Game.StateChanged += OnStateChanged;
        Game.ModeChanged += OnModeChanged;
        Game.HitScored += OnHitScored;
        Game.ScoreManager.ScoreChanged += OnScoreChanged;
        Game.Ball.Launched += OnBallLaunched;
        if (Cosmetics != null) Cosmetics.Changed += RefreshMenuMode;

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
    }

    private void Update()
    {
        if (!built) return;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HandleBack();

        var runner = Game.Runner;
        if (Game.State == SoloGameManager.GameState.Playing && runner != null && runner.IsTimed)
            hud.SetTime(runner.TimeRemaining, runner.TimeLimit);

        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            ApplySafeArea();
    }

    private void OnModeChanged(GameModeDefinition mode)
    {
        RefreshMenuMode();
    }

    private void RefreshMenuMode()
    {
        var mode = Game.CurrentMode;
        if (mode == null) return;

        var modes = Game.ModeList;
        int index = 0;
        for (int i = 0; i < modes.Count; i++) if (modes[i] == mode) index = i;

        Sprite art = mode.Kind == GameModeKind.Classic ? EquippedBall()?.Happy : null;
        menu.SetMode(mode, index, modes.Count, Game.BestFor(mode), Game.IsModeUnlocked(mode), art);
    }

    private void StepMode(int direction)
    {
        var modes = Game.ModeList;
        if (modes.Count < 2) return;

        int index = 0;
        for (int i = 0; i < modes.Count; i++) if (modes[i] == Game.CurrentMode) index = i;

        index = (index + direction + modes.Count) % modes.Count;
        Game.SelectMode(modes[index]);
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
                if (!settings.IsVisible && !shop.IsVisible && !scores.IsVisible)
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
                hud.Hide();
                pause.Hide();
                SetHint(false);
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
        var runner = Game.Runner;
        bool timed = runner != null && runner.IsTimed;
        hud.SetTimed(timed);
        if (timed) hud.SetTime(runner.TimeRemaining, runner.TimeLimit);
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

    private void ShowNextReward()
    {
        if (Cosmetics == null || Game.State != SoloGameManager.GameState.GameOver || reward.IsVisible) return;

        var item = Cosmetics.TakeNextReward();
        if (item == null) return;

        gameOver.Hide();
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

    private void ContinueAfterAd(Action next)
    {
        gameOver.SetInteractable(false);

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

        switch (Game.State)
        {
            case SoloGameManager.GameState.Playing:
                Game.SetPaused(true);
                break;
            case SoloGameManager.GameState.Paused:
                Game.SetPaused(false);
                break;
            case SoloGameManager.GameState.GameOver:
                ContinueAfterAd(Game.ReturnToMenu);
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
    }
}
