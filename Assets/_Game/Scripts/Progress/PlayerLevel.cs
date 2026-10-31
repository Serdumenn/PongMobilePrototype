using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerLevel : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PingiRoad Road;

    [Header("Refs")]
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private ArcadeEvents Events;
    [SerializeField] private DailyMissions Missions;
    [SerializeField] private LocalMatchController LocalMatch;
    [SerializeField] private OnlineMatchController OnlineMatch;
    [SerializeField] private AdventureRun Adventure;

    [Header("XP")]
    [SerializeField] private int EventXp = 15;
    [SerializeField] private int MissionXp = 20;
    [SerializeField] private int BonusXp = 30;
    [SerializeField] private int MatchXp = 15;
    [SerializeField] private int StageXp = 25;

    [Header("Returning players")]
    [SerializeField] private int SeedCapLevel = 9;

    public event Action Changed;
    public event Action<IReadOnlyList<RoadReward>> LevelsReached;

    private SoloGameManager.GameState lastState;
    private bool runOpen;
    private bool seedChecked;

    public PingiRoad RoadAsset => Road;
    public RoadStanding Standing => Road != null ? Road.StandingAt(Cosmetics != null ? Cosmetics.Xp : 0) : default;
    public int Level => Standing.Level;
    public int RunXp { get; private set; }
    public RoadStanding RunStart { get; private set; }
    public List<RoadReward> RunRewards { get; } = new List<RoadReward>();

    private void Awake()
    {
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Events == null) Events = FindFirstObjectByType<ArcadeEvents>();
        if (Missions == null) Missions = FindFirstObjectByType<DailyMissions>();
        if (LocalMatch == null) LocalMatch = FindFirstObjectByType<LocalMatchController>();
        if (OnlineMatch == null) OnlineMatch = FindFirstObjectByType<OnlineMatchController>();
        if (Adventure == null) Adventure = FindFirstObjectByType<AdventureRun>();
    }

    private void OnEnable()
    {
        if (Game != null) Game.StateChanged += OnGameState;
        if (Events != null) Events.Finished += OnEventFinished;
        if (Missions != null)
        {
            Missions.Completed += OnMissionCompleted;
            Missions.AllCompleted += OnAllMissionsCompleted;
        }
        if (LocalMatch != null) LocalMatch.StateChanged += OnLocalMatch;
        if (OnlineMatch != null) OnlineMatch.StateChanged += OnOnlineMatch;
        if (Adventure != null) Adventure.Finished += OnStageFinished;
    }

    private void OnDisable()
    {
        if (Game != null) Game.StateChanged -= OnGameState;
        if (Events != null) Events.Finished -= OnEventFinished;
        if (Missions != null)
        {
            Missions.Completed -= OnMissionCompleted;
            Missions.AllCompleted -= OnAllMissionsCompleted;
        }
        if (LocalMatch != null) LocalMatch.StateChanged -= OnLocalMatch;
        if (OnlineMatch != null) OnlineMatch.StateChanged -= OnOnlineMatch;
        if (Adventure != null) Adventure.Finished -= OnStageFinished;
    }

    private void Update()
    {
        if (seedChecked) return;

        seedChecked = true;
        SeedFromHistory();
    }

    public int SeedFromHistory()
    {
        if (Cosmetics == null || Cosmetics.Inventory == null || Road == null || Cosmetics.Inventory.XpSeeded) return 0;

        var records = Game != null && Game.RecordsSource != null ? Game.RecordsSource.Records : null;
        int history = records != null ? records.TotalHits + records.MatchesPlayed * MatchXp : 0;
        int cap = 0;
        for (int level = 1; level < SeedCapLevel; level++) cap += Road.XpToNext(level);

        Cosmetics.MarkXpSeeded();
        int seed = Mathf.Min(history, cap) - Cosmetics.Xp;
        if (seed <= 0) return 0;

        Add(seed);
        return seed;
    }

    public void Add(int amount)
    {
        if (amount <= 0 || Cosmetics == null || Road == null) return;

        int from = Cosmetics.RoadLevel;
        var after = Road.StandingAt(Cosmetics.Xp + amount);
        var reached = new List<RoadReward>();
        int gold = 0;
        int stars = 0;
        for (int level = from + 1; level <= after.Level; level++)
        {
            var reward = Road.RewardFor(level);
            gold += reward.Gold;
            stars += reward.Stars;
            reached.Add(reward);
        }

        Cosmetics.AddProgress(amount, after.Level, gold, stars);
        if (runOpen)
        {
            RunXp += amount;
            RunRewards.AddRange(reached);
        }

        Changed?.Invoke();
        if (reached.Count > 0) LevelsReached?.Invoke(reached);
    }

    private void OnGameState(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused)
        {
            runOpen = true;
            RunXp = 0;
            RunRewards.Clear();
            RunStart = Standing;
        }
        else if (state == SoloGameManager.GameState.GameOver)
        {
            int score = Game != null && Game.ScoreManager != null ? Game.ScoreManager.Score : 0;
            Add(score);
        }
        else if (state == SoloGameManager.GameState.Menu)
        {
            runOpen = false;
        }

        lastState = state;
    }

    private void OnEventFinished(EventKind kind, bool success, int gold)
    {
        if (success) Add(EventXp);
    }

    private void OnMissionCompleted(int index)
    {
        Add(MissionXp);
    }

    private void OnAllMissionsCompleted()
    {
        Add(BonusXp);
    }

    private void OnStageFinished(bool won)
    {
        if (won) Add(StageXp);
    }

    private void OnLocalMatch(LocalMatchController.MatchState state)
    {
        if (state == LocalMatchController.MatchState.Result) Add(MatchXp);
    }

    private void OnOnlineMatch(OnlineMatchController.MatchState state)
    {
        if (state == OnlineMatchController.MatchState.Result) Add(MatchXp);
    }
}
