using System;
using System.Collections.Generic;

public static class Showcase
{
    public static List<CosmeticItem> Pick(IReadOnlyList<CosmeticItem> catalog, Func<CosmeticItem, bool> owned, string seed, int day, int size)
    {
        var exclusive = new List<CosmeticItem>();
        var regular = new List<CosmeticItem>();
        if (catalog != null)
        {
            foreach (var item in catalog)
            {
                if (item == null || owned(item)) continue;
                if (item.Unlock == UnlockKind.Showcase && item.GoldPrice > 0) exclusive.Add(item);
                else if (item.SoldForGold && item.Unlock != UnlockKind.Season) regular.Add(item);
            }
        }

        Order(exclusive, seed, day);
        Order(regular, seed, day);

        var result = new List<CosmeticItem>();
        if (size <= 0) return result;
        if (exclusive.Count > 0) result.Add(exclusive[0]);
        for (int i = 0; i < regular.Count && result.Count < size; i++) result.Add(regular[i]);
        for (int i = 1; i < exclusive.Count && result.Count < size; i++) result.Add(exclusive[i]);
        return result;
    }

    private static void Order(List<CosmeticItem> items, string seed, int day)
    {
        string prefix = seed + "|" + day + "|";
        items.Sort((a, b) =>
        {
            int byHash = ShopClock.Hash(prefix + a.Id).CompareTo(ShopClock.Hash(prefix + b.Id));
            return byHash != 0 ? byHash : string.CompareOrdinal(a.Id, b.Id);
        });
    }
}
