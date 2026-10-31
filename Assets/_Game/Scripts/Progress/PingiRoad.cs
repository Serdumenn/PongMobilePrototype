using System.Collections.Generic;
using UnityEngine;

public struct RoadStanding
{
    public int Level;
    public int Into;
    public int Need;

    public float Fraction => Need > 0 ? Mathf.Clamp01((float)Into / Need) : 1f;
}

public struct RoadReward
{
    public int Level;
    public int Gold;
    public int Stars;
    public CosmeticItem Item;
}

[CreateAssetMenu(menuName = "Pingi/Pingi Road", fileName = "road_pingi")]
public sealed class PingiRoad : ScriptableObject
{
    [field: Header("Curve")]
    [field: SerializeField] public int BaseXp { get; private set; } = 60;
    [field: SerializeField] public int StepXp { get; private set; } = 10;
    [field: SerializeField] public int CapLevel { get; private set; } = 50;

    [field: Header("Rewards")]
    [field: SerializeField] public int GoldBase { get; private set; } = 30;
    [field: SerializeField] public int GoldStep { get; private set; } = 5;
    [field: SerializeField] public int StarEvery { get; private set; } = 5;
    [field: SerializeField] public int StarCount { get; private set; } = 2;

    [field: Header("Items")]
    [field: SerializeField] public CosmeticCatalog Catalog { get; private set; }

    public int XpToNext(int level)
    {
        return RoadMath.XpToNext(level, BaseXp, StepXp, CapLevel);
    }

    public RoadStanding StandingAt(int xp)
    {
        return RoadMath.StandingAt(xp, BaseXp, StepXp, CapLevel);
    }

    public RoadReward RewardFor(int level)
    {
        var reward = new RoadReward
        {
            Level = level,
            Gold = RoadMath.GoldFor(level, GoldBase, GoldStep, CapLevel),
            Stars = RoadMath.StarsFor(level, StarEvery, StarCount)
        };

        reward.Item = ItemFor(level);
        if (reward.Item != null) reward.Stars = 0;
        return reward;
    }

    public CosmeticItem ItemFor(int level)
    {
        if (Catalog == null) return null;
        foreach (var item in Catalog.Items)
            if (item != null && item.Unlock == UnlockKind.Level && item.UnlockValue == level) return item;
        return null;
    }

    public List<int> ItemLevels()
    {
        var levels = new List<int>();
        if (Catalog == null) return levels;
        foreach (var item in Catalog.Items)
            if (item != null && item.Unlock == UnlockKind.Level) levels.Add(item.UnlockValue);
        levels.Sort();
        return levels;
    }
}

public static class RoadMath
{
    public static int XpToNext(int level, int baseXp, int stepXp, int capLevel)
    {
        int capped = Mathf.Clamp(level, 1, Mathf.Max(1, capLevel));
        return Mathf.Max(1, baseXp + stepXp * (capped - 1));
    }

    public static RoadStanding StandingAt(int xp, int baseXp, int stepXp, int capLevel)
    {
        int left = Mathf.Max(0, xp);
        int level = 1;
        int need = XpToNext(level, baseXp, stepXp, capLevel);
        while (left >= need)
        {
            left -= need;
            level++;
            need = XpToNext(level, baseXp, stepXp, capLevel);
        }

        return new RoadStanding { Level = level, Into = left, Need = need };
    }

    public static int GoldFor(int level, int goldBase, int goldStep, int capLevel)
    {
        if (level < 2) return 0;
        return goldBase + goldStep * Mathf.Min(level, Mathf.Max(2, capLevel));
    }

    public static int StarsFor(int level, int every, int count)
    {
        return every > 0 && level >= every && level % every == 0 ? count : 0;
    }
}
