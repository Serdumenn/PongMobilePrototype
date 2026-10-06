using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class CosmeticInventory
{
    private const int CurrentVersion = 1;

    [Serializable]
    private sealed class SaveData
    {
        public int Version = CurrentVersion;
        public List<string> Owned = new List<string>();
        public List<string> AdItems = new List<string>();
        public List<int> AdViews = new List<int>();
        public List<string> ProcessedOrders = new List<string>();
        public string Ball;
        public string Paddle;
        public string Theme;
        public bool HasPass;
        public bool NoAds;
    }

    private readonly string path;
    private SaveData data;

    public bool HasPass => data.HasPass;
    public bool NoAds => data.NoAds;

    private CosmeticInventory(string path, SaveData data)
    {
        this.path = path;
        this.data = data;
    }

    public static CosmeticInventory FromJson(string path, string json)
    {
        SaveData loaded = null;
        try
        {
            if (!string.IsNullOrEmpty(json)) loaded = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cosmetics] Backup could not be read: {e.Message}");
        }

        return new CosmeticInventory(path, loaded ?? new SaveData());
    }

    public string ToJson()
    {
        data.Version = CurrentVersion;
        return JsonUtility.ToJson(data);
    }

    public string ToBackupJson()
    {
        var copy = JsonUtility.FromJson<SaveData>(ToJson());
        copy.ProcessedOrders.Clear();
        return JsonUtility.ToJson(copy);
    }

    public void KeepOrdersFrom(CosmeticInventory other)
    {
        if (other == null) return;
        foreach (var order in other.data.ProcessedOrders)
            if (!string.IsNullOrEmpty(order) && !data.ProcessedOrders.Contains(order)) data.ProcessedOrders.Add(order);
    }

    public void MergeFrom(CosmeticInventory other)
    {
        if (other == null) return;
        var o = other.data;

        foreach (var id in o.Owned)
            if (!string.IsNullOrEmpty(id) && !data.Owned.Contains(id)) data.Owned.Add(id);

        for (int i = 0; i < o.AdItems.Count && i < o.AdViews.Count; i++)
        {
            int index = data.AdItems.IndexOf(o.AdItems[i]);
            if (index < 0)
            {
                data.AdItems.Add(o.AdItems[i]);
                data.AdViews.Add(o.AdViews[i]);
            }
            else data.AdViews[index] = Math.Max(data.AdViews[index], o.AdViews[i]);
        }

        data.HasPass |= o.HasPass;
        data.NoAds |= o.NoAds;
        if (string.IsNullOrEmpty(data.Ball)) data.Ball = o.Ball;
        if (string.IsNullOrEmpty(data.Paddle)) data.Paddle = o.Paddle;
        if (string.IsNullOrEmpty(data.Theme)) data.Theme = o.Theme;
    }

    public static CosmeticInventory Load(string path)
    {
        SaveData loaded = null;
        try
        {
            if (File.Exists(path)) loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Cosmetics] Save could not be read, starting fresh: {e.Message}");
        }

        return new CosmeticInventory(path, loaded ?? new SaveData());
    }

    public bool IsUnlocked(CosmeticItem item)
    {
        if (item == null) return false;
        if (item.Unlock == UnlockKind.Free) return true;
        if (item.Unlock == UnlockKind.Streak) return data.Owned.Contains(item.Id);
        return data.HasPass || data.Owned.Contains(item.Id);
    }

    public bool Grant(string itemId)
    {
        if (string.IsNullOrEmpty(itemId) || data.Owned.Contains(itemId)) return false;
        data.Owned.Add(itemId);
        return true;
    }

    public int AdViews(CosmeticItem item)
    {
        int index = data.AdItems.IndexOf(item.Id);
        return index >= 0 ? data.AdViews[index] : 0;
    }

    public bool AddAdView(CosmeticItem item)
    {
        int index = data.AdItems.IndexOf(item.Id);
        if (index < 0)
        {
            data.AdItems.Add(item.Id);
            data.AdViews.Add(0);
            index = data.AdItems.Count - 1;
        }

        data.AdViews[index]++;
        return data.AdViews[index] >= item.UnlockValue && Grant(item.Id);
    }

    public string GetEquipped(CosmeticCategory category)
    {
        return category switch
        {
            CosmeticCategory.Ball => data.Ball,
            CosmeticCategory.Paddle => data.Paddle,
            _ => data.Theme
        };
    }

    public void SetEquipped(CosmeticCategory category, string itemId)
    {
        switch (category)
        {
            case CosmeticCategory.Ball: data.Ball = itemId; break;
            case CosmeticCategory.Paddle: data.Paddle = itemId; break;
            default: data.Theme = itemId; break;
        }
    }

    public void GrantPass()
    {
        data.HasPass = true;
    }

    public void GrantNoAds()
    {
        data.NoAds = true;
    }

    public bool IsOrderProcessed(string orderId)
    {
        return !string.IsNullOrEmpty(orderId) && data.ProcessedOrders.Contains(orderId);
    }

    public void MarkOrderProcessed(string orderId)
    {
        if (!string.IsNullOrEmpty(orderId) && !data.ProcessedOrders.Contains(orderId))
            data.ProcessedOrders.Add(orderId);
    }

    public bool Save()
    {
        try
        {
            data.Version = CurrentVersion;
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Cosmetics] Save failed: {e.Message}");
            return false;
        }
    }
}
