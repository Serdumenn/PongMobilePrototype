using NUnit.Framework;
using UnityEngine;

public sealed class BallPaceTests
{
    private const float Pace = 9f;
    private const float MinSide = 1.5f;
    private const float MaxSide = 5.5f;

    [Test]
    public void Velocity_KeepsVerticalSpeedAtAnyAngle()
    {
        foreach (float angle in new[] { 2f, 20f, 45f, 70f, 88f })
        {
            float radians = angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));

            var up = BallPace.Velocity(direction, Pace, MinSide, MaxSide);
            var down = BallPace.Velocity(new Vector2(direction.x, -direction.y), Pace, MinSide, MaxSide);

            Assert.AreEqual(Pace, up.y, 0.0001f, "angle " + angle);
            Assert.AreEqual(-Pace, down.y, 0.0001f, "angle " + angle);
        }
    }

    [Test]
    public void Velocity_LimitsSideSpeed()
    {
        var shallow = BallPace.Velocity(new Vector2(0.95f, 0.3f), Pace, MinSide, MaxSide);
        var steep = BallPace.Velocity(new Vector2(-0.01f, 1f), Pace, MinSide, MaxSide);

        Assert.AreEqual(MaxSide, shallow.x, 0.0001f);
        Assert.AreEqual(-MinSide, steep.x, 0.0001f);
    }

    [Test]
    public void Velocity_KeepsAngleBetweenLimits()
    {
        var velocity = BallPace.Velocity(new Vector2(1f, 3f), Pace, MinSide, MaxSide);

        Assert.AreEqual(3f, velocity.x, 0.0001f);
        Assert.AreEqual(Pace, velocity.y, 0.0001f);
    }

    [Test]
    public void Velocity_IsStableWhenAppliedAgain()
    {
        var first = BallPace.Velocity(new Vector2(0.4f, -0.9f), Pace, MinSide, MaxSide);
        var again = BallPace.Velocity(first.normalized, Pace, MinSide, MaxSide);

        Assert.AreEqual(first.x, again.x, 0.0001f);
        Assert.AreEqual(first.y, again.y, 0.0001f);
    }

    [Test]
    public void Velocity_FasterPaceKeepsSideLimit()
    {
        var fast = BallPace.Velocity(new Vector2(0.8f, 0.6f), 17f, MinSide, MaxSide);

        Assert.AreEqual(17f, fast.y, 0.0001f);
        Assert.AreEqual(MaxSide, fast.x, 0.0001f);
    }

    [Test]
    public void Velocity_FlatDirectionStillClimbs()
    {
        var velocity = BallPace.Velocity(new Vector2(-1f, 0f), Pace, MinSide, MaxSide);

        Assert.AreEqual(-MaxSide, velocity.x, 0.0001f);
        Assert.AreEqual(Pace, velocity.y, 0.0001f);
    }

    [Test]
    public void ServeDirection_GoesUpTowardTheChosenSide()
    {
        var left = BallPace.Velocity(BallPace.ServeDirection(-1f, 0f, Pace, MaxSide), Pace, MinSide, MaxSide);
        var right = BallPace.Velocity(BallPace.ServeDirection(1f, 1f, Pace, MaxSide), Pace, MinSide, MaxSide);

        Assert.AreEqual(Pace, left.y, 0.0001f);
        Assert.Less(left.x, -MinSide);
        Assert.Greater(right.x, MinSide);
        Assert.Greater(Mathf.Abs(left.x), Mathf.Abs(right.x));
        Assert.LessOrEqual(Mathf.Abs(left.x), MaxSide);
    }
}
