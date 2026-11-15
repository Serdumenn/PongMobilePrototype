using System;
using System.Collections.Generic;
using UnityEngine;

public static class SurpriseBox
{
    private static readonly CosmeticTier[] Tiers = { CosmeticTier.Common, CosmeticTier.Rare, CosmeticTier.Epic };

    public static bool Eligible(CosmeticItem item)
    {
        if (item == null) return false;
        bool tier = item.Tier == CosmeticTier.Common || item.Tier == CosmeticTier.Rare || item.Tier == CosmeticTier.Epic;
        bool sold = item.Unlock == UnlockKind.Score || item.Unlock == UnlockKind.Ads || item.Unlock == UnlockKind.Gold || item.Unlock == UnlockKind.Showcase;
        return tier && sold;
    }

    public static Dictionary<CosmeticTier, float> Odds(IReadOnlyList<CosmeticItem> catalog, Func<CosmeticItem, bool> owned, int common, int rare, int epic)
    {
        var weights = new Dictionary<CosmeticTier, float>();
        float total = 0f;
        foreach (var tier in Tiers)
        {
            float weight = WeightOf(tier, common, rare, epic);
            if (weight <= 0f || Left(catalog, owned, tier).Count == 0) continue;
            weights[tier] = weight;
            total += weight;
        }

        var odds = new Dictionary<CosmeticTier, float>();
        if (total <= 0f) return odds;
        foreach (var pair in weights) odds[pair.Key] = pair.Value / total * 100f;
        return odds;
    }

    public static CosmeticItem Draw(IReadOnlyList<CosmeticItem> catalog, Func<CosmeticItem, bool> owned, int common, int rare, int epic, float tierRoll, float itemRoll)
    {
        var odds = Odds(catalog, owned, common, rare, epic);
        if (odds.Count == 0) return null;

        float roll = Mathf.Clamp01(tierRoll) * 100f;
        float sum = 0f;
        var chosen = CosmeticTier.None;
        foreach (var tier in Tiers)
        {
            if (!odds.TryGetValue(tier, out float share)) continue;
            chosen = tier;
            sum += share;
            if (roll < sum) break;
        }

        var left = Left(catalog, owned, chosen);
        if (left.Count == 0) return null;
        int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(itemRoll) * left.Count), 0, left.Count - 1);
        return left[index];
    }

    public static List<CosmeticItem> Left(IReadOnlyList<CosmeticItem> catalog, Func<CosmeticItem, bool> owned, CosmeticTier tier)
    {
        var left = new List<CosmeticItem>();
        if (catalog == null) return left;
        foreach (var item in catalog)
            if (Eligible(item) && item.Tier == tier && !owned(item)) left.Add(item);
        left.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        return left;
    }

    private static float WeightOf(CosmeticTier tier, int common, int rare, int epic)
    {
        return tier switch
        {
            CosmeticTier.Common => common,
            CosmeticTier.Rare => rare,
            CosmeticTier.Epic => epic,
            _ => 0
        };
    }
}
