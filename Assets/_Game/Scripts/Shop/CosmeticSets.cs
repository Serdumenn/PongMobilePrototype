using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CosmeticSet
{
    [SerializeField] private string id;
    [SerializeField] private string name;
    [SerializeField] private List<CosmeticItem> items = new List<CosmeticItem>();
    [SerializeField] private CosmeticItem reward;

    public CosmeticSet(string id, string name, List<CosmeticItem> items, CosmeticItem reward)
    {
        this.id = id;
        this.name = name;
        this.items = items ?? new List<CosmeticItem>();
        this.reward = reward;
    }

    public string Id => id;
    public string Name => name;
    public string Title => Loc.T(name);
    public IReadOnlyList<CosmeticItem> Items => items;
    public CosmeticItem Reward => reward;
}

public static class CosmeticSets
{
    public static int Progress(CosmeticSet set, Func<CosmeticItem, bool> owned)
    {
        int count = 0;
        if (set == null) return 0;
        foreach (var item in set.Items)
            if (item != null && owned(item)) count++;
        return count;
    }

    public static bool Complete(CosmeticSet set, Func<CosmeticItem, bool> owned)
    {
        return set != null && set.Items.Count > 0 && Progress(set, owned) == set.Items.Count;
    }

    public static List<CosmeticItem> RewardsDue(IReadOnlyList<CosmeticSet> sets, Func<CosmeticItem, bool> owned)
    {
        var due = new List<CosmeticItem>();
        if (sets == null) return due;
        foreach (var set in sets)
            if (set != null && set.Reward != null && !owned(set.Reward) && Complete(set, owned)) due.Add(set.Reward);
        return due;
    }

    public static CosmeticSet SetOf(IReadOnlyList<CosmeticSet> sets, CosmeticItem item)
    {
        if (sets == null || item == null) return null;
        foreach (var set in sets)
        {
            if (set == null) continue;
            if (set.Reward == item) return set;
            foreach (var part in set.Items)
                if (part == item) return set;
        }
        return null;
    }
}
