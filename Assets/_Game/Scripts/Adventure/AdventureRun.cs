using System;
using UnityEngine;

public sealed class AdventureRun : MonoBehaviour
{
    public const string ModeId = "adventure";

    [Header("Data")]
    [SerializeField] private AdventureBook Book;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private TargetBoard Board;
    [SerializeField] private GoldCoins Coins;
    [SerializeField] private FieldObstacles Obstacles;
    [SerializeField] private GameFeel Feel;
    [SerializeField] private Camera WorldCamera;

    [Header("Coins")]
    [SerializeField] private float RainEvery = 0.9f;

    [Header("Celebration")]
    [SerializeField] private int Confetti = 16;

    public event Action Started;
    public event Action Changed;
    public event Action<bool> Finished;

    private SoloGameManager.GameState lastState;
    private Vector3 restScale;
    private bool pendingSetup;
    private bool fieldReady;

    public AdventureBook BookAsset => Book;
    public int Index { get; private set; } = -1;
    public AdventureLevel Level => Book != null && Index >= 0 ? Book.Level(Index) : null;
    public AdventureRules Rules => Game != null ? Game.Rules as AdventureRules : null;
    public bool Running => Game != null && Game.InAdventure && Game.State == SoloGameManager.GameState.Playing && Level != null;
    public int Progress { get; private set; }
    public int Target { get; private set; }
    public bool Won { get; private set; }
    public int StarsEarned { get; private set; }
    public int GoldEarned { get; private set; }
    public bool FirstClear { get; private set; }
    public bool HasNext => Book != null && Index + 1 < Book.Count;
    public int TotalStars => Cosmetics != null && Cosmetics.Inventory != null ? Cosmetics.Inventory.TotalLevelStars : 0;

    private Ball MainBall => Game != null ? Game.Ball : null;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Board == null) Board = FindFirstObjectByType<TargetBoard>();
        if (Coins == null) Coins = FindFirstObjectByType<GoldCoins>();
        if (Obstacles == null) Obstacles = FindFirstObjectByType<FieldObstacles>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();
        if (WorldCamera == null) WorldCamera = Camera.main;
        if (MainBall != null) restScale = MainBall.transform.localScale;
    }

    private void OnEnable()
    {
        if (Game != null)
        {
            Game.StateChanged += OnGameState;
            Game.HitScored += OnHit;
        }
        if (Board != null) Board.Hit += OnTarget;
        if (Coins != null) Coins.Collected += OnCoin;
    }

    private void OnDisable()
    {
        if (Game != null)
        {
            Game.StateChanged -= OnGameState;
            Game.HitScored -= OnHit;
        }
        if (Board != null) Board.Hit -= OnTarget;
        if (Coins != null) Coins.Collected -= OnCoin;
    }

    public int StarsAt(int index)
    {
        return Cosmetics != null ? Cosmetics.LevelStarsAt(index) : 0;
    }

    public bool IsOpen(int index)
    {
        return Book != null && index >= 0 && index < Book.Count && AdventureMap.IsOpen(index, StarsAt);
    }

    public int NextIndex => Book != null ? AdventureMap.Next(Book.Count, StarsAt) : 0;

    public bool Play(int index)
    {
        if (Game == null || Book == null || !IsOpen(index)) return false;

        var mode = Game.FindMode(ModeId);
        if (mode == null) return false;

        Index = index;
        Game.StartAdventureRun(mode, Book.Level(index));
        return Game.InAdventure && Game.State == SoloGameManager.GameState.Playing;
    }

    private void OnGameState(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused && Game.InAdventure)
        {
            Progress = 0;
            Target = Level != null ? Level.Goals : 0;
            Won = false;
            StarsEarned = 0;
            GoldEarned = 0;
            FirstClear = false;
            pendingSetup = true;
        }
        else if (state == SoloGameManager.GameState.GameOver && Game.InAdventure && Level != null)
        {
            Settle();
        }
        else if (state == SoloGameManager.GameState.Menu)
        {
            pendingSetup = false;
            ClearField();
        }

        lastState = state;
    }

    private void Update()
    {
        if (pendingSetup && Running) SetupField();
        if (!Running || !fieldReady || Level.Goal != AdventureGoal.Survive || Rules == null) return;

        int seconds = Mathf.FloorToInt(Rules.Elapsed);
        if (seconds == Progress) return;
        Progress = Mathf.Min(seconds, Target);
        Changed?.Invoke();
    }

    private void SetupField()
    {
        pendingSetup = false;
        fieldReady = true;
        var level = Level;

        if (MainBall != null)
        {
            MainBall.transform.localScale = restScale * Mathf.Max(0.5f, level.BallScale);
            MainBall.SetSpeedBoost(Mathf.Max(0.5f, level.Speed));
        }

        if (Board != null)
        {
            if (level.Goal == AdventureGoal.Targets && level.TargetSpots.Count > 0) Board.Show(level.TargetSpots, level.TargetSway, level.TargetScale);
            else if (level.Goal == AdventureGoal.Targets) Board.Show(level.Target);
            else Board.Hide();
        }

        if (Coins != null && level.Goal == AdventureGoal.Coins)
        {
            Coins.Open();
            Coins.StartRain(float.MaxValue / 4f, RainEvery);
        }

        if (Obstacles != null) Obstacles.Show(level.Bumpers, level.BumperSway);

        Started?.Invoke();
        Changed?.Invoke();
    }

    private void OnHit(HitResult hit)
    {
        if (!Running || !fieldReady) return;

        var goal = Level.Goal;
        if (goal == AdventureGoal.Hits || goal == AdventureGoal.Perfects && hit.Perfect) Step();
    }

    private void OnTarget(int index)
    {
        if (Running && fieldReady && Level.Goal == AdventureGoal.Targets) Step();
    }

    private void OnCoin(int value)
    {
        if (Running && fieldReady && Level.Goal == AdventureGoal.Coins) Step();
    }

    private void Step()
    {
        Progress = Mathf.Min(Progress + 1, Target);
        Changed?.Invoke();
        if (Progress < Target) return;

        Won = true;
        Celebrate();
        Game.GameOver();
    }

    private void Settle()
    {
        var rules = Rules;
        if (!Won && rules != null && rules.Survived) Won = true;

        if (Won)
        {
            StarsEarned = AdventureMap.StarsFor(rules != null ? rules.Lives : 1);
            FirstClear = StarsAt(Index) <= 0;
            GoldEarned = FirstClear ? Level.Gold : 0;
            if (Cosmetics != null) Cosmetics.RecordLevel(Index, StarsEarned, GoldEarned);
        }

        ClearField();
        Finished?.Invoke(Won);
    }

    private void ClearField()
    {
        fieldReady = false;
        if (Board != null) Board.Hide();
        if (Obstacles != null) Obstacles.Hide();
        if (MainBall != null)
        {
            MainBall.transform.localScale = restScale;
            MainBall.SetSpeedBoost(1f);
        }
    }

    private void Celebrate()
    {
        if (Feel == null || WorldCamera == null) return;

        Vector2 origin = WorldCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.78f, -WorldCamera.transform.position.z));
        Feel.Celebrate(origin, Vector2.up, Confetti, 150f, 5.5f);
    }
}
