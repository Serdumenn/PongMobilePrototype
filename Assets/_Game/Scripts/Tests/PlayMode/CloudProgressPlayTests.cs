using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CloudProgressPlayTests : GameSceneFixture
{
    private sealed class FakeCloudStore : ICloudStore
    {
        public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
        public int Saves;

        public Task<string> LoadAsync(string key)
        {
            return Task.FromResult(Data.TryGetValue(key, out var json) ? json : null);
        }

        public Task SaveAsync(string key, string json)
        {
            Saves++;
            Data[key] = json;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key)
        {
            Data.Remove(key);
            return Task.CompletedTask;
        }
    }

    private static CloudProgress Cloud => Object.FindFirstObjectByType<CloudProgress>();
    private static CosmeticsService Cosmetics => Object.FindFirstObjectByType<CosmeticsService>();

    private static IEnumerator Await(Task task)
    {
        while (!task.IsCompleted) yield return null;
        if (task.IsFaulted) throw task.Exception;
    }

    private static ProgressSnapshot Stored(FakeCloudStore store)
    {
        return JsonUtility.FromJson<ProgressSnapshot>(store.Data[CloudProgress.Key]);
    }

    private FakeCloudStore CloudWith(int classicBest, string ownedItem, bool noAds)
    {
        var inventory = CosmeticInventory.FromJson(null, null);
        if (ownedItem != null) inventory.Grant(ownedItem);
        if (noAds) inventory.GrantNoAds();

        var snapshot = new ProgressSnapshot { records = PlayerRecords.FromJson(null, null).ToJson(), cosmetics = inventory.ToJson() };
        snapshot.SetBest(SoloScoreManager.BestScoreKey, classicBest);

        var store = new FakeCloudStore();
        store.Data[CloudProgress.Key] = JsonUtility.ToJson(snapshot);
        return store;
    }

    [UnityTest]
    public IEnumerator FirstSync_UploadsThisPhonesProgress()
    {
        yield return LoadGame();
        PlayerPrefs.SetInt("bestScore_rush", 21);
        var store = new FakeCloudStore();
        Cloud.UseStore(store);

        yield return Await(Cloud.SyncAsync("player-1"));
        Assert.AreEqual(CloudProgress.Status.Synced, Cloud.State);
        Assert.AreEqual(1, store.Saves, "The first backup is written");
        Assert.AreEqual(21, Stored(store).Best("bestScore_rush"));
    }

    [UnityTest]
    public IEnumerator Restore_BringsBackScoresAndItems_AndKeepsLocalProgress()
    {
        yield return LoadGame();
        PlayerPrefs.SetInt("bestScore_rush", 21);
        var astro = Cosmetics.CatalogAsset.Find("ball_astro");
        Assert.IsFalse(Cosmetics.IsUnlocked(astro), "A clean profile does not own Astro");

        var store = CloudWith(40, "ball_astro", true);
        Cloud.UseStore(store);
        yield return Await(Cloud.SyncAsync("player-1"));
        yield return null;

        Assert.AreEqual(40, PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey), "The classic best comes back");
        Assert.AreEqual(21, PlayerPrefs.GetInt("bestScore_rush"), "This phone's rush best stays");
        Assert.IsTrue(Cosmetics.IsUnlocked(astro), "Owned items come back");
        Assert.IsTrue(Cosmetics.Inventory.NoAds, "Purchases come back");
        Assert.AreEqual(21, Stored(store).Best("bestScore_rush"), "The backup now has both");
        Assert.AreEqual(40, Game.ScoreManager.BestFor(SoloScoreManager.BestScoreKey));
    }

    [UnityTest]
    public IEnumerator Restore_WaitsUntilTheRunEnds()
    {
        yield return LoadGame();
        Game.StartGameFromMenu();
        yield return null;

        var store = CloudWith(40, null, false);
        Cloud.UseStore(store);
        yield return Await(Cloud.SyncAsync("player-1"));
        for (int i = 0; i < 5; i++) yield return null;
        Assert.AreEqual(0, PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey), "Nothing changes during a run");

        Game.GameOver();
        Game.ReturnToMenu();
        yield return null;
        yield return null;
        Assert.AreEqual(40, PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey), "Applied back in the menu");
    }

    [UnityTest]
    public IEnumerator ResettingBests_AlsoResetsTheBackup()
    {
        yield return LoadGame();
        var store = CloudWith(40, null, false);
        Cloud.UseStore(store);
        yield return Await(Cloud.SyncAsync("player-1"));
        yield return null;
        Assert.AreEqual(40, PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey));

        Game.ResetAllBests();
        Cloud.BestsReset();
        yield return Await(Cloud.SyncAsync("player-1"));
        yield return null;

        Assert.AreEqual(0, Stored(store).Best(SoloScoreManager.BestScoreKey), "The backup forgets the reset scores");
        Assert.AreEqual(0, PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey), "They do not come back");
        Assert.AreEqual(0, PlayerPrefs.GetInt("CloudBestsReset", 0), "The reset is done");
    }
}
