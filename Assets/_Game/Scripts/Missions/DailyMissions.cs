using System;
using UnityEngine;

public sealed class DailyMissions : MonoBehaviour
{
    private const string SaveFileName = "missions.json";
    private const string RushModeId = "rush";

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private RunMoments Moments;
    [SerializeField] private PowerUps Powers;

    public event Action Changed;
    public event Action<int> Completed;
    public event Action AllCompleted;

    private MissionLog log;
    private MissionDef[] today = Array.Empty<MissionDef>();

    public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
    public int Count => today.Length;
    public int DoneCount => log != null ? log.DoneCount : 0;
    public bool BonusEarned => log != null && log.Bonus;
    public string Day => log != null ? log.Day : null;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Moments == null) Moments = FindFirstObjectByType<RunMoments>();
        if (Powers == null) Powers = FindFirstObjectByType<PowerUps>();
        Reload();
    }

    private void OnEnable()
    {
        if (Game != null)
        {
            Game.HitScored += OnHitScored;
            Game.StateChanged += OnStateChanged;
        }

        if (Moments != null) Moments.NearMissed += OnNearMissed;
        if (Powers != null) Powers.Collected += OnPowerCollected;
    }

    private void OnDisable()
    {
        if (Game != null)
        {
            Game.HitScored -= OnHitScored;
            Game.StateChanged -= OnStateChanged;
        }

        if (Moments != null) Moments.NearMissed -= OnNearMissed;
        if (Powers != null) Powers.Collected -= OnPowerCollected;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) Refresh();
    }

    public void Reload()
    {
        log = MissionLog.Load(SaveLocation.PathFor(SaveFileName));
        today = Array.Empty<MissionDef>();
        Refresh();
    }

    public void Refresh()
    {
        if (log == null) return;

        DateTime now = Clock();
        bool rolled = log.Roll(MissionBook.DayKey(now));
        if (rolled || today.Length == 0) today = MissionBook.For(now, RushOpen());
        if (rolled) log.Save();
        if (rolled) Changed?.Invoke();
    }

    public MissionDef Mission(int index)
    {
        return today[index];
    }

    public int Progress(int index)
    {
        return log != null ? log.Progress(index) : 0;
    }

    public bool Done(int index)
    {
        return log != null && log.Done(index);
    }

    private bool RushOpen()
    {
        if (Game == null) return true;

        var rush = Game.FindMode(RushModeId);
        return rush == null || Game.IsModeUnlocked(rush);
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Menu) Refresh();
        if (state == SoloGameManager.GameState.GameOver) Add(MissionKind.Runs, 1);
    }

    private void OnHitScored(HitResult hit)
    {
        if (Game == null || Game.State != SoloGameManager.GameState.Playing) return;

        Add(MissionKind.Hits, 1);
        if (hit.Perfect) Add(MissionKind.Perfects, 1);
        if (Game.Player != null) Reach(MissionKind.Streak, Game.Player.HitStreak);

        var mode = Game.RunMode;
        int score = Game.ScoreManager != null ? Game.ScoreManager.Score : 0;
        if (mode != null && mode.Kind == GameModeKind.Classic) Reach(MissionKind.ClassicHits, score);
        if (mode != null && mode.Kind == GameModeKind.Rush) Reach(MissionKind.RushScore, score);
    }

    private void OnNearMissed()
    {
        Add(MissionKind.CloseSaves, 1);
    }

    private void OnPowerCollected(PowerKind kind)
    {
        Add(MissionKind.PowerUps, 1);
    }

    public void Add(MissionKind kind, int amount)
    {
        Track(kind, amount, true);
    }

    public void Reach(MissionKind kind, int value)
    {
        Track(kind, value, false);
    }

    private void Track(MissionKind kind, int value, bool additive)
    {
        if (log == null || value <= 0) return;

        Refresh();
        bool changed = false;
        int completed = 0;
        for (int i = 0; i < today.Length; i++)
        {
            if (today[i].Kind != kind || log.Done(i)) continue;

            int before = log.Progress(i);
            int next = additive ? before + value : value;
            if (next <= before) continue;

            changed = true;
            if (!log.Raise(i, next, today[i].Target)) continue;

            completed++;
            Completed?.Invoke(i);
        }

        if (!changed) return;

        bool bonus = log.ClaimBonus();
        log.Save();
        if (Cosmetics != null && completed + (bonus ? 1 : 0) > 0) Cosmetics.AddStars(completed + (bonus ? 1 : 0));
        if (bonus) AllCompleted?.Invoke();
        Changed?.Invoke();
    }
}
