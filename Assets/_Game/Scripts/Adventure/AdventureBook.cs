using System;
using System.Collections.Generic;
using UnityEngine;

public enum AdventureGoal
{
    Hits,
    Targets,
    Coins,
    Survive,
    Perfects
}

[Serializable]
public sealed class AdventureLevel
{
    [field: SerializeField] public AdventureGoal Goal { get; private set; }
    [field: SerializeField] public int Target { get; private set; } = 10;
    [field: SerializeField] public float TimeLimit { get; private set; }
    [field: SerializeField] public int Lives { get; private set; } = 3;
    [field: SerializeField] public float Speed { get; private set; } = 1f;
    [field: SerializeField] public float BallScale { get; private set; } = 1f;
    [field: SerializeField] public int Gold { get; private set; } = 20;
    [field: SerializeField] public List<Vector2> TargetSpots { get; private set; } = new List<Vector2>();
    [field: SerializeField] public float TargetSway { get; private set; }
    [field: SerializeField] public float TargetScale { get; private set; } = 1f;
    [field: SerializeField] public List<Vector2> Bumpers { get; private set; } = new List<Vector2>();
    [field: SerializeField] public float BumperSway { get; private set; }

    public float Seconds => Goal == AdventureGoal.Survive ? Target : TimeLimit;
    public int Goals => Goal == AdventureGoal.Targets && TargetSpots.Count > 0 ? TargetSpots.Count : Target;
}

[Serializable]
public sealed class AdventureWorld
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public string Icon { get; private set; }
    [field: SerializeField] public Color Tint { get; private set; } = Color.white;
    [field: SerializeField] public List<AdventureLevel> Levels { get; private set; } = new List<AdventureLevel>();

    public string Title => Loc.T(Name);
}

[CreateAssetMenu(menuName = "Pingi/Adventure Book", fileName = "adventure_book")]
public sealed class AdventureBook : ScriptableObject
{
    [field: SerializeField] public List<AdventureWorld> Worlds { get; private set; } = new List<AdventureWorld>();

    public int Count
    {
        get
        {
            int count = 0;
            foreach (var world in Worlds) count += world.Levels.Count;
            return count;
        }
    }

    public int MaxStars => Count * 3;

    public AdventureLevel Level(int index)
    {
        int world = WorldOf(index);
        return world < 0 ? null : Worlds[world].Levels[index - FirstOf(world)];
    }

    public int WorldOf(int index)
    {
        if (index < 0) return -1;
        int start = 0;
        for (int i = 0; i < Worlds.Count; i++)
        {
            int count = Worlds[i].Levels.Count;
            if (index < start + count) return i;
            start += count;
        }
        return -1;
    }

    public int FirstOf(int world)
    {
        int start = 0;
        for (int i = 0; i < world && i < Worlds.Count; i++) start += Worlds[i].Levels.Count;
        return start;
    }

    public string Label(int index)
    {
        int world = WorldOf(index);
        return world < 0 ? "" : $"{world + 1}-{index - FirstOf(world) + 1}";
    }

    public bool IsBoss(int index)
    {
        int world = WorldOf(index);
        return world >= 0 && index == FirstOf(world) + Worlds[world].Levels.Count - 1;
    }
}

public static class AdventureMap
{
    public const int MaxStars = 3;

    public static bool IsOpen(int index, Func<int, int> starsAt)
    {
        return index == 0 || index > 0 && starsAt(index - 1) > 0;
    }

    public static int Next(int count, Func<int, int> starsAt)
    {
        for (int i = 0; i < count; i++)
            if (starsAt(i) <= 0) return i;
        return Mathf.Max(0, count - 1);
    }

    public static int StarsFor(int livesLeft)
    {
        return Mathf.Clamp(livesLeft, 1, MaxStars);
    }
}
