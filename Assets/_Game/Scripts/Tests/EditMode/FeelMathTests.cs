using NUnit.Framework;
using UnityEngine;

public sealed class FeelMathTests
{
    [Test]
    public void Spring_StartsFullOvershootsAndSettles()
    {
        Assert.AreEqual(1f, FeelMath.Spring(0f, 3.2f, 14f), 0.0001f);
        Assert.Less(FeelMath.Spring(0.15f, 3.2f, 14f), 0f);
        Assert.AreEqual(0f, FeelMath.Spring(1f, 3.2f, 14f), 0.0001f);
    }

    [Test]
    public void Spring_NeverGrowsPastItsStart()
    {
        for (int i = 0; i <= 100; i++)
            Assert.LessOrEqual(Mathf.Abs(FeelMath.Spring(i * 0.01f, 3.2f, 14f)), 1f);
    }

    [Test]
    public void Step_CapsLongFramesSoAHitchCannotSkipTheAnimation()
    {
        Assert.AreEqual(0.016f, FeelMath.Step(0.016f), 0.0001f);
        Assert.AreEqual(0f, FeelMath.Step(-1f), 0.0001f);
        Assert.Less(FeelMath.Step(2f), 0.1f);
        Assert.Greater(FeelMath.Spring(FeelMath.Step(2f), 3.2f, 14f), 0.2f);
    }

    [Test]
    public void Squash_FlattensAlongTheHitAxis()
    {
        var floor = FeelMath.Squash(Vector2.up, 0.3f);
        var wall = FeelMath.Squash(Vector2.left, 0.3f);

        Assert.AreEqual(0.7f, floor.y, 0.0001f);
        Assert.Greater(floor.x, 1f);
        Assert.AreEqual(0.7f, wall.x, 0.0001f);
        Assert.Greater(wall.y, 1f);
    }

    [Test]
    public void Squash_KeepsTheBallVisibleAtAnyAmount()
    {
        var crushed = FeelMath.Squash(Vector2.up, 5f);

        Assert.Greater(crushed.y, 0f);
        Assert.Less(crushed.x, 2f);
    }

    [Test]
    public void Blend_GoesFromSquashedToRest()
    {
        var squashed = new Vector2(1.2f, 0.7f);

        Assert.AreEqual(squashed, FeelMath.Blend(squashed, 1f));
        Assert.AreEqual(Vector2.one, FeelMath.Blend(squashed, 0f));
        Assert.Greater(FeelMath.Blend(squashed, -0.2f).y, 1f);
    }

    [Test]
    public void Fan_SpreadsEvenlyAroundTheNormal()
    {
        var left = FeelMath.Fan(Vector2.up, 0, 5, 120f);
        var middle = FeelMath.Fan(Vector2.up, 2, 5, 120f);
        var right = FeelMath.Fan(Vector2.up, 4, 5, 120f);

        Assert.AreEqual(0f, middle.x, 0.0001f);
        Assert.AreEqual(1f, middle.y, 0.0001f);
        Assert.AreEqual(-left.x, right.x, 0.0001f);
        Assert.AreEqual(left.y, right.y, 0.0001f);
        Assert.AreEqual(1f, left.magnitude, 0.0001f);
    }

    [Test]
    public void Jitter_StaysBetweenZeroAndOne()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            float value = FeelMath.Jitter(seed);
            Assert.GreaterOrEqual(value, 0f);
            Assert.Less(value, 1f);
        }
    }
}
