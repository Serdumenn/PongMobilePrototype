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
    [SerializeField] private Ball SoloBall;
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private Paddle Paddle;
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
    public Ball Ball => SoloBall;
    public RecordsService RecordsSource => Records;
    public IReadOnlyList<GameModeDefinition> ModeList => Modes;
    public GameModeDefinition CurrentMode { get; private set; }
    public MatchRules Rules { get; private set; }
    public Participant Player { get; private set; }
    public int RunSeed { get; private set; }

    private readonly List<Participant> participants = new List<Participant>();
    private int? nextSeed;
    private float runSeconds;
    private Coroutine respawnRoutine;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        if (SoloBall == null) SoloBall = FindFirstObjectByType<Ball>();
        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Paddle == null) Paddle = FindFirstObjectByType<Paddle>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();

        if (BallEntrance == null && SoloBall != null)
            BallEntrance = SoloBall.GetComponent<GameObjectEntrance>();
        if (RacketEntrance == null && Paddle != null)
            RacketEntrance = Paddle.GetComponent<GameObjectEntrance>();

        if (Paddle != null) Paddle.InputEnabled = false;

        Player = new Participant(0, Paddle != null ? Paddle.Side : FieldSide.Bottom, Paddle);
        participants.Add(Player);

        CurrentMode = FindMode(PlayerPrefs.GetString(SelectedModeKey, null));
        if (CurrentMode == null || !IsModeUnlocked(CurrentMode)) CurrentMode = Modes.Count > 0 ? Modes[0] : null;
        if (Score != null && CurrentMode != null) Score.SetBestKey(CurrentMode.BestScoreKey);
    }

    private void Start()
    {
        if (SoloBall != null)
        {
            SoloBall.Launched += OnBallLaunched;
            SoloBall.PaddleStruck += OnPaddleStruck;
            SoloBall.GoalEntered += OnGoalEntered;
        }

        Time.timeScale = 1f;
        PlayEntranceAnimations(false);
        SetState(GameState.Menu);
        ModeChanged?.Invoke(CurrentMode);
    }

    private void OnDestroy()
    {
        if (SoloBall == null) return;

        SoloBall.Launched -= OnBallLaunched;
        SoloBall.PaddleStruck -= OnPaddleStruck;
        SoloBall.GoalEntered -= OnGoalEntered;
    }

    private void Update()
    {
        if (State != GameState.Playing || Rules == null) return;

        runSeconds += Time.deltaTime;
        if (Rules.Tick(Time.deltaTime)) GameOver();
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

    public void SetSeedForNextRun(int seed)
    {
        nextSeed = seed;
    }

    public void RegisterPaddleHit(float paddleOffset)
    {
        RegisterHit(Player, paddleOffset);
    }

    public void OnBallMissed()
    {
        Concede(Player);
    }

    private void RegisterHit(Participant hitter, float paddleOffset)
    {
        if (State != GameState.Playing || Rules == null || hitter == null) return;

        var hit = Rules.ScoreHit(hitter, paddleOffset);
        if (Score != null) Score.AddPoints(hit.Points);
        if (Records != null) Records.AddHit();

        hitter.RegisterHit();
        HitScored?.Invoke(hit);
    }

    private void Concede(Participant conceded)
    {
        if (State != GameState.Playing || conceded == null) return;

        conceded.BreakStreak();
        if (Rules == null || Rules.OnMiss(conceded))
        {
            GameOver();
            return;
        }

        StopRespawn();
        respawnRoutine = StartCoroutine(RespawnBall());
    }

    private void OnPaddleStruck(Paddle paddle, float paddleOffset)
    {
        RegisterHit(FindParticipant(paddle), paddleOffset);
    }

    private void OnGoalEntered(Goal goal)
    {
        Concede(FindParticipant(goal.Side));
    }

    private Participant FindParticipant(Paddle paddle)
    {
        for (int i = 0; i < participants.Count; i++)
            if (participants[i].Paddle == paddle) return participants[i];
        return null;
    }

    private Participant FindParticipant(FieldSide side)
    {
        for (int i = 0; i < participants.Count; i++)
            if (participants[i].Side == side) return participants[i];
        return null;
    }

    public void GameOver()
    {
        if (State == GameState.GameOver) return;

        Time.timeScale = 0f;
        StopRespawn();

        if (SoloBall != null) SoloBall.StopRound();
        if (Paddle != null) Paddle.InputEnabled = false;

        if (Score != null) Score.GameOver();
        if (Records != null) Records.RecordRun(CurrentMode, Score != null && Score.IsNewBest, Player.LongestStreak, runSeconds);

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

        Rules = MatchRules.Create(CurrentMode);
        Rules.Begin(participants);
        runSeconds = 0f;

        RunSeed = nextSeed ?? unchecked((int)DateTime.UtcNow.Ticks);
        nextSeed = null;

        if (SoloBall != null)
        {
            SoloBall.Rng = new MatchRandom(RunSeed);
            SoloBall.SetServer(Paddle);
            SoloBall.StartRound();
        }
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
        if (State == GameState.Playing) Rules?.OnLaunched();
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
