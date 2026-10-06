using System;
using System.Collections.Generic;

[Serializable]
public sealed class ProgressSnapshot
{
    public const int CurrentVersion = 1;

    public int v = CurrentVersion;
    public string records;
    public string cosmetics;
    public List<string> bestKeys = new List<string>();
    public List<int> bestValues = new List<int>();
    public string dailyDay;
    public int dailyBest;
    public string appVersion;
    public long savedAt;

    public int Best(string key)
    {
        int index = bestKeys.IndexOf(key);
        return index >= 0 && index < bestValues.Count ? bestValues[index] : 0;
    }

    public void SetBest(string key, int value)
    {
        int index = bestKeys.IndexOf(key);
        if (index < 0)
        {
            bestKeys.Add(key);
            bestValues.Add(value);
        }
        else bestValues[index] = value;
    }

    public bool SameProgress(ProgressSnapshot other)
    {
        if (other == null) return false;
        if (!Same(records, other.records) || !Same(cosmetics, other.cosmetics)) return false;
        if (!Same(dailyDay, other.dailyDay) || dailyBest != other.dailyBest) return false;
        if (bestKeys.Count != other.bestKeys.Count) return false;
        foreach (var key in bestKeys)
            if (!other.bestKeys.Contains(key) || Best(key) != other.Best(key)) return false;
        return true;
    }

    private static bool Same(string a, string b)
    {
        return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);
    }

    public static ProgressSnapshot Merge(ProgressSnapshot local, ProgressSnapshot cloud, bool localBestsWin)
    {
        if (cloud == null || cloud.v > CurrentVersion) return local;
        if (local == null) return cloud;

        var merged = new ProgressSnapshot { appVersion = local.appVersion };

        var records = PlayerRecords.FromJson(null, local.records);
        records.MergeFrom(PlayerRecords.FromJson(null, cloud.records), localBestsWin);
        merged.records = records.ToJson();

        var cosmetics = CosmeticInventory.FromJson(null, local.cosmetics);
        cosmetics.MergeFrom(CosmeticInventory.FromJson(null, cloud.cosmetics));
        merged.cosmetics = cosmetics.ToBackupJson();

        foreach (var key in local.bestKeys) merged.SetBest(key, local.Best(key));
        if (!localBestsWin)
            foreach (var key in cloud.bestKeys) merged.SetBest(key, Math.Max(merged.Best(key), cloud.Best(key)));

        int order = string.CompareOrdinal(cloud.dailyDay ?? string.Empty, local.dailyDay ?? string.Empty);
        if (order > 0)
        {
            merged.dailyDay = cloud.dailyDay;
            merged.dailyBest = cloud.dailyBest;
        }
        else
        {
            merged.dailyDay = local.dailyDay;
            merged.dailyBest = order == 0 ? Math.Max(local.dailyBest, cloud.dailyBest) : local.dailyBest;
        }

        return merged;
    }
}
