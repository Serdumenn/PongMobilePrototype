using System;
using System.Collections.Generic;
using UnityEngine;

public static class ShopRules
{
    public static bool OnSale(CosmeticItem item, IReadOnlyList<SeasonDef> seasons, IReadOnlyList<CosmeticItem> showcase, DateTime local)
    {
        if (item == null || item.GoldPrice <= 0) return false;

        switch (item.Unlock)
        {
            case UnlockKind.Score:
            case UnlockKind.Ads:
            case UnlockKind.Gold:
                return true;
            case UnlockKind.Season:
                if (seasons == null) return false;
                foreach (var season in seasons)
                    if (season != null && Contains(season.Items, item) && SeasonWindow.IsOpen(season, local)) return true;
                return false;
            case UnlockKind.Showcase:
                return Contains(showcase, item);
            default:
                return false;
        }
    }

    public static int Discounted(int price, float discount)
    {
        return Mathf.RoundToInt(price * (1f - Mathf.Clamp01(discount)));
    }

    private static bool Contains(IReadOnlyList<CosmeticItem> list, CosmeticItem item)
    {
        if (list == null) return false;
        foreach (var entry in list)
            if (entry == item) return true;
        return false;
    }
}
