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
    public void Warmth_RisesEarlyAndStaysBetweenZeroAndOne()
    {
        Assert.AreEqual(0f, FeelMath.Warmth(-1f), 0.0001f);
        Assert.AreEqual(0f, FeelMath.Warmth(0f), 0.0001f);
        Assert.AreEqual(1f, FeelMath.Warmth(1f), 0.0001f);
        Assert.AreEqual(1f, FeelMath.Warmth(3f), 0.0001f);
        Assert.Greater(FeelMath.Warmth(0.16f), 0.39f);

        float last = 0f;
        for (int i = 1; i <= 20; i++)
        {
            float value = FeelMath.Warmth(i / 20f);
            Assert.Greater(value, last);
            last = value;
        }
    }

    [Test]
    public void Warm_MovesTowardHotAndKeepsAlpha()
    {
        var accent = new Color(0.2f, 0.4f, 0.9f, 0.5f);
        var hot = new Color(1f, 0.86f, 0.45f, 1f);

        Assert.AreEqual(accent, FeelMath.Warm(accent, hot, 0f));
        var full = FeelMath.Warm(accent, hot, 1f);
        Assert.AreEqual(hot.r, full.r, 0.0001f);
        Assert.AreEqual(0.5f, full.a, 0.0001f);
        Assert.Greater(FeelMath.Warm(accent, hot, 0.5f).r, accent.r);
    }

    [Test]
    public void Drift_StaysInsideItsReach()
    {
        var reach = new Vector2(0.16f, 0.22f);
        for (int i = 0; i < 400; i++)
        {
            var offset = FeelMath.Drift(i * 0.173f, 2.4f, reach);
            Assert.LessOrEqual(Mathf.Abs(offset.x), reach.x + 0.0001f);
            Assert.LessOrEqual(Mathf.Abs(offset.y), reach.y + 0.0001f);
        }
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
