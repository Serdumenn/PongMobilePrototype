using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SoloGameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    private const string SelectedModeKey = "SelectedMode";

    [Header("Refs")]
    [SerializeField] private SoloBall SoloBall;
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private RacketController Paddle;
    [SerializeField] private RecordsService Records;

    [Header("Entrance")]
    [SerializeField] private GameObjectEntrance BallEntrance;
    [SerializeField] private GameObjectEntrance RacketEntrance;

    [Header("Menu")]
    [SerializeField] private float MenuBallOffset = 0.45f;

    [Header("Modes")]
    [SerializeField] private List<GameModeDefinition> Modes = new List<GameModeDefinition>();

    public event Action<GameState> StateChanged;
    public event Action<GameModeDefinition> ModeChanged;
    public event Action<HitResult> HitScored;

    public GameState State { get; private set; } = GameState.Menu;
    public bool IsGameOver => State == GameState.GameOver;
    public SoloScoreManager ScoreManager => Score;
    public SoloBall Ball => SoloBall;
    public RecordsService RecordsSource => Records;
    public IReadOnlyList<GameModeDefinition> ModeList => Modes;
    public GameModeDefinition CurrentMode { get; private set; }
    public ModeRunner Runner { get; private set; }

    private int runStreak;
    private int runLongestStreak;
    private float runSeconds;
    private Coroutine respawnRoutine;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        if (SoloBall == null) SoloBall = FindFirstObjectByType<SoloBall>();
        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Paddle == null) Paddle = FindFirstObjectByType<RacketController>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();

        if (BallEntrance == null && SoloBall != null)
            BallEntrance = SoloBall.GetComponent<GameObjectEntrance>();
        if (RacketEntrance == null && Paddle != null)
            RacketEntrance = Paddle.GetComponent<GameObjectEntrance>();

        if (Paddle != null) Paddle.InputEnabled = false;

        CurrentMode = FindMode(PlayerPrefs.GetString(SelectedModeKey, null));
        if (CurrentMode == null || !IsModeUnlocked(CurrentMode)) CurrentMode = Modes.Count > 0 ? Modes[0] : null;
        if (Score != null && CurrentMode != null) Score.SetBestKey(CurrentMode.BestScoreKey);
    }

    private void Start()
    {
        if (SoloBall != null) SoloBall.Launched += OnBallLaunched;

        Time.timeScale = 1f;
        PlayEntranceAnimations(false);
        SetState(GameState.Menu);
        ModeChanged?.Invoke(CurrentMode);
    }

    private void OnDestroy()
    {
        if (SoloBall != null) SoloBall.Launched -= OnBallLaunched;
    }

    private void Update()
    {
        if (State != GameState.Playing || Runner == null) return;

        runSeconds += Time.deltaTime;
        if (Runner.Tick(Time.deltaTime)) GameOver();
    }

    public GameModeDefinition FindMode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Modes.Count; i++)
            if (Modes[i] != null && Modes[i].Id == id) return Modes[i];
        return null;
    }

    public bool IsModeUnlocked(GameModeDefinition mode)
    {
        if (mode == null) return false;
        if (mode.RequiredClassicBest <= 0) return true;
        return Score != null && Score.BestFor(SoloScoreManager.BestScoreKey) >= mode.RequiredClassicBest;
    }

    public int BestFor(GameModeDefinition mode)
    {
        return Score != null && mode != null ? Score.BestFor(mode.BestScoreKey) : 0;
    }

    public void SelectMode(GameModeDefinition mode)
    {
        if (State != GameState.Menu || mode == null || mode == CurrentMode) return;

        CurrentMode = mode;
        PlayerPrefs.SetString(SelectedModeKey, mode.Id);
        PlayerPrefs.Save();
        if (Score != null) Score.SetBestKey(mode.BestScoreKey);
        ModeChanged?.Invoke(mode);
    }

    public void ResetAllBests()
    {
        if (Score == null) return;

        var keys = new string[Modes.Count];
        for (int i = 0; i < Modes.Count; i++) keys[i] = Modes[i] != null ? Modes[i].BestScoreKey : null;
        Score.ResetBests(keys);
        if (Records != null) Records.ClearBestDates();
    }

    public void StartGameFromMenu()
    {
        StartNewRun();
    }

    public void RestartRun()
    {
        StartNewRun();
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        StopRespawn();

        if (SoloBall != null) SoloBall.StopRoundKeepVisible();
        if (Paddle != null) Paddle.InputEnabled = false;

        ShowGameObjects();
        PlayEntranceAnimations(false);
        SetState(GameState.Menu);
    }

    public void SetPaused(bool paused)
    {
        if (paused && State != GameState.Playing) return;
        if (!paused && State != GameState.Paused) return;

        Time.timeScale = paused ? 0f : 1f;
        if (Paddle != null) Paddle.InputEnabled = !paused;

        SetState(paused ? GameState.Paused : GameState.Playing);
    }

    public void RegisterPaddleHit(float paddleOffset)
    {
        if (State != GameState.Playing || Runner == null) return;

        var hit = Runner.ScoreHit(paddleOffset);
        if (Score != null) Score.AddPoints(hit.Points);
        if (Records != null) Records.AddHit();

        runStreak++;
        runLongestStreak = Mathf.Max(runLongestStreak, runStreak);
        HitScored?.Invoke(hit);
    }

    public void OnBallMissed()
    {
        if (State != GameState.Playing) return;

        runStreak = 0;
        if (Runner == null || Runner.OnMiss())
        {
            GameOver();
            return;
        }

        StopRespawn();
        respawnRoutine = StartCoroutine(RespawnBall());
    }

    public void GameOver()
    {
        if (State == GameState.GameOver) return;

        Time.timeScale = 0f;
        StopRespawn();

        if (SoloBall != null) SoloBall.StopRound();
        if (Paddle != null) Paddle.InputEnabled = false;

        if (Score != null) Score.GameOver();
        if (Records != null) Records.RecordRun(CurrentMode, Score != null && Score.IsNewBest, runLongestStreak, runSeconds);

        SetState(GameState.GameOver);
    }

    public void HideGameObjects()
    {
        if (SoloBall != null) SoloBall.SetBallVisible(false);
        if (Paddle != null) Paddle.SetVisible(false);
    }

    public void ShowGameObjects()
    {
        if (SoloBall != null) SoloBall.SetBallVisible(true);
        if (Paddle != null) Paddle.SetVisible(true);
    }

    private void StartNewRun()
    {
        Time.timeScale = 1f;
        StopRespawn();

        if (Score != null)
        {
            if (CurrentMode != null) Score.SetBestKey(CurrentMode.BestScoreKey);
            Score.ResetScore();
        }

        Runner = ModeRunner.Create(CurrentMode);
        Runner.Begin();
        runStreak = 0;
        runLongestStreak = 0;
        runSeconds = 0f;

        if (SoloBall != null) SoloBall.StartRound();
        if (Paddle != null)
        {
            Paddle.InputEnabled = true;
            Paddle.SetVisible(true);
        }

        PlayEntranceAnimations(true);
        SetState(GameState.Playing);
    }

    private IEnumerator RespawnBall()
    {
        float delay = CurrentMode != null ? CurrentMode.RespawnDelaySeconds : 1f;
        yield return new WaitForSeconds(delay);

        respawnRoutine = null;
        if (State != GameState.Playing || SoloBall == null) yield break;

        SoloBall.StartRound();
        SoloBall.EnableServe();
    }

    private void StopRespawn()
    {
        if (respawnRoutine == null) return;
        StopCoroutine(respawnRoutine);
        respawnRoutine = null;
    }

    private void OnBallLaunched()
    {
        if (State == GameState.Playing) Runner?.OnLaunched();
    }

    private void PlayEntranceAnimations(bool forGameplay)
    {
        if (Paddle != null) Paddle.ResetPosition();

        if (SoloBall != null)
        {
            if (forGameplay || Paddle == null)
                SoloBall.SnapToServePoint();
            else
                SoloBall.SnapTo((Vector2)Paddle.transform.position + Vector2.up * MenuBallOffset);
        }

        var racketDir = forGameplay ? GameObjectEntrance.Direction.Right : GameObjectEntrance.Direction.Left;
        var ballDir = forGameplay ? GameObjectEntrance.Direction.Left : GameObjectEntrance.Direction.Right;

        if (RacketEntrance != null)
            RacketEntrance.PlayEntrance(racketDir);

        if (BallEntrance != null)
        {
            BallEntrance.PlayEntrance(ballDir, () =>
            {
                if (forGameplay && SoloBall != null && State == GameState.Playing) SoloBall.EnableServe();
            });
        }
        else if (forGameplay && SoloBall != null)
        {
            SoloBall.EnableServe();
        }
    }

    private void SetState(GameState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
