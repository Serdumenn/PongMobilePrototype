using System;
using System.Collections.Generic;

public enum MissionKind
{
    ClassicHits,
    RushScore,
    Perfects,
    Hits,
    Streak,
    Runs,
    CloseSaves,
    PowerUps
}

public readonly struct MissionDef
{
    public readonly MissionKind Kind;
    public readonly int Target;

    public MissionDef(MissionKind kind, int target)
    {
        Kind = kind;
        Target = target;
    }

    public bool NeedsRush => Kind == MissionKind.RushScore || Kind == MissionKind.Perfects || Kind == MissionKind.PowerUps;
}

public static class MissionBook
{
    public const int Count = 3;

    private static readonly MissionDef[] Easy =
    {
        new MissionDef(MissionKind.Runs, 3),
        new MissionDef(MissionKind.Hits, 30),
        new MissionDef(MissionKind.ClassicHits, 8),
        new MissionDef(MissionKind.Streak, 8),
        new MissionDef(MissionKind.Perfects, 3),
        new MissionDef(MissionKind.PowerUps, 1)
    };

    private static readonly MissionDef[] Medium =
    {
        new MissionDef(MissionKind.Hits, 80),
        new MissionDef(MissionKind.Streak, 15),
        new MissionDef(MissionKind.ClassicHits, 15),
        new MissionDef(MissionKind.RushScore, 25),
        new MissionDef(MissionKind.Perfects, 8),
        new MissionDef(MissionKind.CloseSaves, 1),
        new MissionDef(MissionKind.PowerUps, 3)
    };

    private static readonly MissionDef[] Hard =
    {
        new MissionDef(MissionKind.Hits, 160),
        new MissionDef(MissionKind.Streak, 25),
        new MissionDef(MissionKind.ClassicHits, 25),
        new MissionDef(MissionKind.RushScore, 45),
        new MissionDef(MissionKind.Perfects, 15),
        new MissionDef(MissionKind.CloseSaves, 3),
        new MissionDef(MissionKind.Runs, 8)
    };

    public static string DayKey(DateTime date)
    {
        return date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static MissionDef[] For(DateTime date, bool rushOpen)
    {
        int seed = date.Year * 10000 + date.Month * 100 + date.Day;
        var picked = new List<MissionDef>(Count);
        Pick(Easy, seed, 1, rushOpen, picked);
        Pick(Medium, seed, 2, rushOpen, picked);
        Pick(Hard, seed, 3, rushOpen, picked);
        return picked.ToArray();
    }

    private static void Pick(MissionDef[] pool, int seed, int tier, bool rushOpen, List<MissionDef> picked)
    {
        int start = (int)(Mix(seed, tier) % (uint)pool.Length);
        for (int step = 0; step < pool.Length; step++)
        {
            var candidate = pool[(start + step) % pool.Length];
            if (!rushOpen && candidate.NeedsRush) continue;
            if (picked.Exists(m => m.Kind == candidate.Kind)) continue;

            picked.Add(candidate);
            return;
        }
    }

    private static uint Mix(int seed, int tier)
    {
        unchecked
        {
            uint z = (uint)seed * 2654435761u ^ (uint)tier * 2246822519u;
            z ^= z >> 15;
            z *= 2246822507u;
            z ^= z >> 13;
            z *= 3266489909u;
            return z ^ (z >> 16);
        }
    }
}
