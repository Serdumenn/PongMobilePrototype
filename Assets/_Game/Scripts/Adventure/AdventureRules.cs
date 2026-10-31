using System.Collections.Generic;
using UnityEngine;

public sealed class AdventureRules : MatchRules
{
    private readonly AdventureLevel level;
    private float remaining;
    private bool started;

    public AdventureRules(GameModeDefinition mode, AdventureLevel level) : base(mode)
    {
        this.level = level;
    }

    public int Lives { get; private set; }
    public int StartLives => level != null ? Mathf.Max(1, level.Lives) : 3;
    public override float TimeLimit => level != null ? level.Seconds : 0f;
    public override float TimeRemaining => remaining;
    public bool HasClock => TimeLimit > 0f;
    public bool Survived => level != null && level.Goal == AdventureGoal.Survive && started && remaining <= 0f && Lives > 0;
    public float Elapsed => HasClock ? TimeLimit - remaining : 0f;

    public override void Begin(IReadOnlyList<Participant> participants)
    {
        base.Begin(participants);
        Lives = StartLives;
        remaining = TimeLimit;
        started = false;
    }

    public override void OnLaunched()
    {
        started = true;
    }

    public override bool Tick(float deltaTime)
    {
        if (!started || !HasClock) return false;
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
        Lives = Mathf.Max(0, Lives - 1);
        return Lives <= 0;
    }

    public override bool MissWouldEnd(Participant conceded)
    {
        return Lives <= 1;
    }

    public override void OnRevived()
    {
        Lives = Mathf.Max(Lives, 1);
    }
}
