using System.Collections.Generic;
using UnityEngine;

public readonly struct HitResult
{
    public readonly int Points;
    public readonly bool Perfect;
    public readonly int PerfectStreak;

    public HitResult(int points, bool perfect, int perfectStreak)
    {
        Points = points;
        Perfect = perfect;
        PerfectStreak = perfectStreak;
    }
}

public abstract class MatchRules
{
    protected readonly GameModeDefinition Mode;

    protected MatchRules(GameModeDefinition mode)
    {
        Mode = mode;
    }

    public virtual bool IsTimed => false;
    public virtual float TimeRemaining => 0f;
    public virtual float TimeLimit => 0f;
    public virtual Participant Winner => null;
    public virtual bool IsTeamMatch => false;
    public virtual int TeamScore => 0;
    public virtual int TeamLives => 0;
    public virtual int BallsInPlay => 1;

    public virtual void Begin(IReadOnlyList<Participant> participants)
    {
        for (int i = 0; i < participants.Count; i++) participants[i].BeginRun();
    }

    public virtual void OnLaunched()
    {
    }

    public virtual bool Tick(float deltaTime)
    {
        return false;
    }

    public abstract HitResult ScoreHit(Participant hitter, float paddleOffset);

    public abstract bool OnMiss(Participant conceded);

    public static MatchRules Create(GameModeDefinition mode)
    {
        if (mode == null) return new ClassicRules(null);

        return mode.Kind switch
        {
            GameModeKind.Rush => new RushRules(mode),
            GameModeKind.RushBattle => new RushRules(mode),
            GameModeKind.TableDuel => new TableDuelRules(mode),
            GameModeKind.CoopRally => new CoopRallyRules(mode),
            GameModeKind.PartyTable => new PartyRules(mode),
            _ => new ClassicRules(mode)
        };
    }
}

public sealed class ClassicRules : MatchRules
{
    public ClassicRules(GameModeDefinition mode) : base(mode)
    {
    }

    public override HitResult ScoreHit(Participant hitter, float paddleOffset)
    {
        return new HitResult(1, false, 0);
    }

    public override bool OnMiss(Participant conceded)
    {
        return true;
    }
}

public sealed class RushRules : MatchRules
{
    private float remaining;
    private bool started;

    public RushRules(GameModeDefinition mode) : base(mode)
    {
    }

    public override bool IsTimed => true;
    public override float TimeRemaining => remaining;
    public override float TimeLimit => Mode.DurationSeconds;

    public override void Begin(IReadOnlyList<Participant> participants)
    {
        base.Begin(participants);
        remaining = Mode.DurationSeconds;
        started = false;
    }

    public override void OnLaunched()
    {
        started = true;
    }

    public override bool Tick(float deltaTime)
    {
        if (!started) return false;
        remaining = Mathf.Max(0f, remaining - deltaTime);
        return remaining <= 0f;
    }

    public override HitResult ScoreHit(Participant hitter, float paddleOffset)
    {
        bool perfect = Mathf.Abs(paddleOffset) <= Mode.PerfectZone;
        hitter.PerfectStreak = perfect ? hitter.PerfectStreak + 1 : 0;

        int points = !perfect ? 1 : hitter.PerfectStreak >= Mode.ComboThreshold ? Mode.ComboPoints : Mode.PerfectPoints;
        return new HitResult(points, perfect, hitter.PerfectStreak);
    }

    public override bool OnMiss(Participant conceded)
    {
        conceded.PerfectStreak = 0;
        remaining = Mathf.Max(0f, remaining - Mode.MissPenaltySeconds);
        return remaining <= 0f;
    }
}

public sealed class TableDuelRules : MatchRules
{
    private IReadOnlyList<Participant> players;
    private Participant winner;

    public TableDuelRules(GameModeDefinition mode) : base(mode)
    {
    }

    public override Participant Winner => winner;

    public override void Begin(IReadOnlyList<Participant> participants)
    {
        base.Begin(participants);
        players = participants;
        winner = null;
    }

    public override HitResult ScoreHit(Participant hitter, float paddleOffset)
    {
        return new HitResult(0, false, 0);
    }

    public override bool OnMiss(Participant conceded)
    {
        if (winner != null) return true;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == conceded) continue;

            player.AddPoint();
            if (player.Score >= Mode.PointsToWin && (winner == null || player.Score > winner.Score)) winner = player;
        }

        return winner != null;
    }
}

public sealed class CoopRallyRules : MatchRules
{
    private int passes;
    private int lives;

    public CoopRallyRules(GameModeDefinition mode) : base(mode)
    {
    }

    public override bool IsTeamMatch => true;
    public override int TeamScore => passes;
    public override int TeamLives => lives;

    public override void Begin(IReadOnlyList<Participant> participants)
    {
        base.Begin(participants);
        passes = 0;
        lives = Mode.Lives;
    }

    public override HitResult ScoreHit(Participant hitter, float paddleOffset)
    {
        passes++;
        return new HitResult(1, Mathf.Abs(paddleOffset) <= Mode.PerfectZone, 0);
    }

    public override bool OnMiss(Participant conceded)
    {
        lives = Mathf.Max(0, lives - 1);
        return lives <= 0;
    }
}

public sealed class PartyRules : MatchRules
{
    private IReadOnlyList<Participant> players;
    private Participant winner;
    private int hits;

    public PartyRules(GameModeDefinition mode) : base(mode)
    {
    }

    public override Participant Winner => winner;
    public override int TeamScore => hits;
    public override int BallsInPlay => Mode.ExtraBallAtHits > 0 && hits >= Mode.ExtraBallAtHits ? 2 : 1;

    public override void Begin(IReadOnlyList<Participant> participants)
    {
        base.Begin(participants);
        players = participants;
        winner = null;
        hits = 0;
        for (int i = 0; i < participants.Count; i++) participants[i].Lives = Mode.Lives;
    }

    public override HitResult ScoreHit(Participant hitter, float paddleOffset)
    {
        hits++;
        return new HitResult(0, false, 0);
    }

    public override bool OnMiss(Participant conceded)
    {
        if (winner != null) return true;
        if (conceded.Eliminated) return false;

        conceded.Lives = Mathf.Max(0, conceded.Lives - 1);
        if (conceded.Lives > 0) return false;

        conceded.Eliminated = true;

        Participant last = null;
        int alive = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].Eliminated) continue;
            alive++;
            last = players[i];
        }

        if (alive > 1) return false;

        winner = last;
        return true;
    }
}
