using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

public interface ICloudStore
{
    Task<string> LoadAsync(string key);
    Task SaveAsync(string key, string json);
    Task DeleteAsync(string key);
}

public sealed class UgsCloudStore : ICloudStore
{
    public async Task<string> LoadAsync(string key)
    {
        var items = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
        return items.TryGetValue(key, out var item) && item?.Value != null ? item.Value.GetAsString() : null;
    }

    public Task SaveAsync(string key, string json)
    {
        return CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { key, json } });
    }

    public Task DeleteAsync(string key)
    {
        return CloudSaveService.Instance.Data.Player.DeleteAsync(key);
    }
}

public sealed class CloudProgress : MonoBehaviour
{
    public const string Key = "progress";
    private const string ResetPref = "CloudBestsReset";

    public enum Status
    {
        Idle,
        Syncing,
        Synced,
        Failed
    }

    [SerializeField] private OnlineService online;
    [SerializeField] private RecordsService records;
    [SerializeField] private CosmeticsService cosmetics;
    [SerializeField] private SoloGameManager game;
    [SerializeField] private float UploadDelay = 8f;
    [SerializeField] private bool SyncInEditor = false;

    public static Func<ICloudStore> StoreFactory = () => new UgsCloudStore();

    private ICloudStore store;
    private ProgressSnapshot pendingApply;
    private bool dirty;
    private float dirtyAt;
    private bool busy;
    private bool bestsReset;
    private bool applying;
    private string syncedPlayer;

    private bool AutoSync => !Application.isEditor || SyncInEditor;

    public Status State { get; private set; } = Status.Idle;
    public DateTime LastSyncedUtc { get; private set; }
    public event Action Changed;

    private void Start()
    {
        if (online == null) online = FindFirstObjectByType<OnlineService>();
        if (records == null) records = FindFirstObjectByType<RecordsService>();
        if (cosmetics == null) cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (game == null) game = FindFirstObjectByType<SoloGameManager>();

        if (online != null)
        {
            online.StateChanged += OnOnlineState;
            online.AccountChanged += OnAccountChanged;
        }
        if (records != null) records.Saved += MarkDirty;
        if (cosmetics != null) cosmetics.Changed += MarkDirty;
        if (game != null) game.StateChanged += OnGameState;
        if (AutoSync && online != null && online.IsReady) _ = SyncAsync();
    }

    private void OnDestroy()
    {
        if (online != null)
        {
            online.StateChanged -= OnOnlineState;
            online.AccountChanged -= OnAccountChanged;
        }
        if (records != null) records.Saved -= MarkDirty;
        if (cosmetics != null) cosmetics.Changed -= MarkDirty;
        if (game != null) game.StateChanged -= OnGameState;
    }

    private void Update()
    {
        if (pendingApply != null && CanApply()) Apply();
        if (AutoSync && dirty && !busy && syncedPlayer != null && online != null && online.IsReady && Time.realtimeSinceStartup - dirtyAt >= UploadDelay) _ = SyncAsync();
    }

    private void OnApplicationPause(bool pause)
    {
        if (AutoSync && pause && dirty && !busy && online != null && online.IsReady) _ = SyncAsync();
    }

    public void UseStore(ICloudStore with)
    {
        store = with;
    }

    public void MarkDirty()
    {
        if (applying) return;
        dirty = true;
        dirtyAt = Time.realtimeSinceStartup;
    }

    public void BestsReset()
    {
        bestsReset = true;
        PlayerPrefs.SetInt(ResetPref, 1);
        PlayerPrefs.Save();
        MarkDirty();
    }

    public Task SyncAsync()
    {
        if (online == null || !online.IsReady) return Task.CompletedTask;
        return SyncAsync(online.PlayerId);
    }

    public async Task SyncAsync(string player)
    {
        if (busy || string.IsNullOrEmpty(player)) return;

        busy = true;
        dirty = false;
        SetState(Status.Syncing);
        try
        {
            store ??= StoreFactory();
            string json = await store.LoadAsync(Key);
            var cloud = Parse(json);
            var local = Capture();
            bool resetBests = bestsReset || PlayerPrefs.GetInt(ResetPref, 0) == 1;
            var merged = ProgressSnapshot.Merge(local, cloud, resetBests);
            merged.appVersion = Application.version;
            merged.savedAt = DateTime.UtcNow.Ticks;

            if (cloud == null || !merged.SameProgress(cloud) || resetBests) await store.SaveAsync(Key, JsonUtility.ToJson(merged));
            if (online != null && online.IsReady && player != online.PlayerId) return;

            if (resetBests)
            {
                bestsReset = false;
                PlayerPrefs.DeleteKey(ResetPref);
                PlayerPrefs.Save();
            }
            if (!merged.SameProgress(local)) pendingApply = merged;
            syncedPlayer = player;
            LastSyncedUtc = DateTime.UtcNow;
            SetState(Status.Synced);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Progress backup failed: {e.Message}");
            dirty = true;
            dirtyAt = Time.realtimeSinceStartup + 30f;
            SetState(Status.Failed);
        }
        finally
        {
            busy = false;
        }
    }

    public async Task DeleteBackupAsync()
    {
        if (online == null || !online.IsReady) return;
        try
        {
            store ??= StoreFactory();
            await store.DeleteAsync(Key);
            syncedPlayer = null;
            dirty = false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Deleting the progress backup failed: {e.Message}");
        }
    }

    public ProgressSnapshot Capture()
    {
        var snapshot = new ProgressSnapshot
        {
            records = records != null && records.Records != null ? records.Records.ToJson() : null,
            cosmetics = cosmetics != null && cosmetics.Inventory != null ? cosmetics.Inventory.ToBackupJson() : null,
            appVersion = Application.version
        };

        if (game != null)
            foreach (var mode in game.ModeList)
                if (mode != null && !string.IsNullOrEmpty(mode.BestScoreKey) && !snapshot.bestKeys.Contains(mode.BestScoreKey))
                    snapshot.SetBest(mode.BestScoreKey, PlayerPrefs.GetInt(mode.BestScoreKey, 0));

        DailyChallenge.Saved(out snapshot.dailyDay, out snapshot.dailyBest);
        return snapshot;
    }

    private bool CanApply()
    {
        return game == null || game.State == SoloGameManager.GameState.Menu;
    }

    private void Apply()
    {
        var snapshot = pendingApply;
        pendingApply = null;
        if (snapshot == null) return;

        var current = Capture();
        var final = ProgressSnapshot.Merge(current, snapshot, false);
        if (final.SameProgress(current)) return;

        applying = true;
        try
        {
            if (records != null && !string.IsNullOrEmpty(final.records)) records.Replace(final.records);

            foreach (var key in final.bestKeys) PlayerPrefs.SetInt(key, final.Best(key));
            DailyChallenge.Restore(final.dailyDay, final.dailyBest);
            PlayerPrefs.Save();
            if (game != null) game.RefreshBest();

            if (cosmetics != null && !string.IsNullOrEmpty(final.cosmetics)) cosmetics.RestoreInventory(final.cosmetics);
        }
        finally
        {
            applying = false;
        }

        if (!final.SameProgress(snapshot)) MarkDirty();
        Changed?.Invoke();
    }

    private void OnOnlineState(OnlineService.Status status)
    {
        if (status == OnlineService.Status.Ready && AutoSync) _ = SyncAsync();
        else if (status == OnlineService.Status.Offline) syncedPlayer = null;
    }

    private void OnAccountChanged()
    {
        if (AutoSync && online != null && online.IsReady && online.PlayerId != syncedPlayer) _ = SyncAsync();
    }

    private void OnGameState(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.GameOver) MarkDirty();
    }

    private static ProgressSnapshot Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<ProgressSnapshot>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void SetState(Status state)
    {
        State = state;
        Changed?.Invoke();
    }
}
