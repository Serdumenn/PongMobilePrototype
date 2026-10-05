using System;
using System.Collections.Generic;

public enum RushAttack : byte
{
    MiniPaddle = 0,
    FastBall = 1,
    Fog = 2
}

public sealed class RushBattlePlayer
{
    public byte Slot;
    public string Id;
    public string Name;
    public string Look;
    public int Score;
    public int SecondsLeft;
    public bool Finished;
    public bool Left;
    public bool WantsRematch;

    public bool Active => !Finished && !Left;
}

public sealed class RushBattleRules
{
    public const int PerfectsPerAttack = 3;
    public const int AttackKinds = 3;

    private readonly List<RushBattlePlayer> players = new List<RushBattlePlayer>();

    public RushBattleRules(IEnumerable<RushBattlePlayer> roster, byte mySlot)
    {
        players.AddRange(roster);
        MySlot = mySlot;
    }

    public IReadOnlyList<RushBattlePlayer> Players => players;
    public byte MySlot { get; }
    public RushBattlePlayer Me => Find(MySlot);

    public static string BuildRoster(IEnumerable<string> ids)
    {
        var list = new List<string>();
        foreach (var id in ids)
            if (!string.IsNullOrEmpty(id) && !list.Contains(id)) list.Add(id);
        list.Sort(string.CompareOrdinal);
        return string.Join(",", list);
    }

    public static string[] ParseRoster(string roster)
    {
        return string.IsNullOrEmpty(roster) ? Array.Empty<string>() : roster.Split(',');
    }

    public static bool ShouldAttack(int perfectStreak)
    {
        return perfectStreak > 0 && perfectStreak % PerfectsPerAttack == 0;
    }

    public static RushAttack KindFor(uint roll)
    {
        return (RushAttack)(roll % AttackKinds);
    }

    public RushBattlePlayer Find(byte slot)
    {
        foreach (var player in players)
            if (player.Slot == slot) return player;
        return null;
    }

    public RushBattlePlayer FindById(string id)
    {
        foreach (var player in players)
            if (player.Id == id) return player;
        return null;
    }

    public RushBattlePlayer PickTarget()
    {
        RushBattlePlayer best = null;
        foreach (var player in players)
        {
            if (player.Slot == MySlot || !player.Active) continue;
            if (best == null || player.Score > best.Score || (player.Score == best.Score && player.Slot < best.Slot)) best = player;
        }
        return best;
    }

    public void SetScore(byte slot, int score, int secondsLeft)
    {
        var player = Find(slot);
        if (player == null || player.Finished) return;
        player.Score = Math.Max(player.Score, score);
        player.SecondsLeft = secondsLeft;
    }

    public void SetFinal(byte slot, int score)
    {
        var player = Find(slot);
        if (player == null) return;
        player.Score = score;
        player.SecondsLeft = 0;
        player.Finished = true;
    }

    public void MarkLeft(byte slot)
    {
        var player = Find(slot);
        if (player != null && !player.Finished) player.Left = true;
    }

    public bool AllDone
    {
        get
        {
            foreach (var player in players)
                if (player.Active) return false;
            return true;
        }
    }

    public bool AloneLeft
    {
        get
        {
            foreach (var player in players)
                if (player.Slot != MySlot && !player.Left) return false;
            return true;
        }
    }

    public List<RushBattlePlayer> Ranking()
    {
        var ranked = new List<RushBattlePlayer>(players);
        ranked.Sort((a, b) =>
        {
            if (a.Left != b.Left) return a.Left ? 1 : -1;
            if (a.Score != b.Score) return b.Score.CompareTo(a.Score);
            return a.Slot.CompareTo(b.Slot);
        });
        return ranked;
    }

    public int PlaceOf(byte slot)
    {
        var ranked = Ranking();
        var target = Find(slot);
        if (target == null) return 0;

        int place = 1;
        foreach (var player in ranked)
        {
            if (player.Slot == slot) return place;
            if (!player.Left && player.Score > target.Score) place++;
        }
        return place;
    }

    public void ClearRematch()
    {
        foreach (var player in players) player.WantsRematch = false;
    }

    public bool EveryoneWantsRematch
    {
        get
        {
            int count = 0;
            foreach (var player in players)
            {
                if (player.Left) continue;
                if (!player.WantsRematch) return false;
                count++;
            }
            return count >= 2;
        }
    }
}
