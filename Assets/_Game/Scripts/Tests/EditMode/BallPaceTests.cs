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
    [Test]
    public void Aim_RightEdgeSendsRightAndLeftEdgeSendsLeft()
    {
        var straight = Vector2.up;
        var right = BallPace.Aim(0.9f, Vector2.up, Vector2.right, straight, 0.6f, 0.25f);
        var left = BallPace.Aim(-0.9f, Vector2.up, Vector2.right, straight, 0.6f, 0.25f);

        Assert.Greater(right.x, 0.3f);
        Assert.Less(left.x, -0.3f);
        Assert.Greater(right.y, 0f);
        Assert.Greater(left.y, 0f);
        Assert.AreEqual(1f, right.magnitude, 0.0001f);
    }

    [Test]
    public void Aim_FartherFromTheCenterTurnsMore()
    {
        float last = 0f;
        for (int i = 1; i <= 10; i++)
        {
            var dir = BallPace.Aim(i / 10f, Vector2.up, Vector2.right, Vector2.up, 0.6f, 0f);
            Assert.Greater(dir.x / dir.y, last);
            last = dir.x / dir.y;
        }
    }

    [Test]
    public void Aim_CenterKeepsALittleOfTheIncomingDrift()
    {
        var fromLeft = BallPace.Aim(0f, Vector2.up, Vector2.right, new Vector2(0.6f, 0.8f), 0.6f, 0.25f);
        var fromRight = BallPace.Aim(0f, Vector2.up, Vector2.right, new Vector2(-0.6f, 0.8f), 0.6f, 0.25f);

        Assert.Greater(fromLeft.x, 0f);
        Assert.Less(fromRight.x, 0f);
        Assert.Less(Mathf.Abs(fromLeft.x), 0.25f);
    }

    [Test]
    public void Aim_WorksForTheTopPaddleInWorldDirections()
    {
        var dir = BallPace.Aim(0.8f, Vector2.down, Vector2.right, Vector2.down, 0.6f, 0f);

        Assert.Less(dir.y, 0f);
        Assert.Greater(dir.x, 0.3f);
    }
}
