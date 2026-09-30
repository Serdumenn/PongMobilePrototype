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

public abstract class ModeRunner
{
    protected readonly GameModeDefinition Mode;

    protected ModeRunner(GameModeDefinition mode)
    {
        Mode = mode;
    }

    public virtual bool IsTimed => false;
    public virtual float TimeRemaining => 0f;
    public virtual float TimeLimit => 0f;

    public virtual void Begin()
    {
    }

    public virtual void OnLaunched()
    {
    }

    public virtual bool Tick(float deltaTime)
    {
        return false;
    }

    public abstract HitResult ScoreHit(float paddleOffset);

    public abstract bool OnMiss();

    public static ModeRunner Create(GameModeDefinition mode)
    {
        return mode != null && mode.Kind == GameModeKind.Rush ? new RushRunner(mode) : new ClassicRunner(mode);
    }
}

public sealed class ClassicRunner : ModeRunner
{
    public ClassicRunner(GameModeDefinition mode) : base(mode)
    {
    }

    public override HitResult ScoreHit(float paddleOffset)
    {
        return new HitResult(1, false, 0);
    }

    public override bool OnMiss()
    {
        return true;
    }
}

public sealed class RushRunner : ModeRunner
{
    private float remaining;
    private bool started;
    private int perfectStreak;

    public RushRunner(GameModeDefinition mode) : base(mode)
    {
    }

    public override bool IsTimed => true;
    public override float TimeRemaining => remaining;
    public override float TimeLimit => Mode.DurationSeconds;

    public override void Begin()
    {
        remaining = Mode.DurationSeconds;
        started = false;
        perfectStreak = 0;
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

    public override HitResult ScoreHit(float paddleOffset)
    {
        bool perfect = Mathf.Abs(paddleOffset) <= Mode.PerfectZone;
        perfectStreak = perfect ? perfectStreak + 1 : 0;

        int points = !perfect ? 1 : perfectStreak >= Mode.ComboThreshold ? Mode.ComboPoints : Mode.PerfectPoints;
        return new HitResult(points, perfect, perfectStreak);
    }

    public override bool OnMiss()
    {
        perfectStreak = 0;
        remaining = Mathf.Max(0f, remaining - Mode.MissPenaltySeconds);
        return remaining <= 0f;
    }
}
