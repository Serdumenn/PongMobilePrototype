using NUnit.Framework;
using UnityEngine;

public sealed class BackdropMotionTests
{
    [Test]
    public void Wrap_StaysInRange()
    {
        Assert.AreEqual(0.25f, BackdropMotion.Wrap(1.25f, 0f, 1f), 0.0001f);
        Assert.AreEqual(0.75f, BackdropMotion.Wrap(-0.25f, 0f, 1f), 0.0001f);
        Assert.AreEqual(-0.1f, BackdropMotion.Wrap(1.1f, -0.2f, 1f), 0.0001f);
        for (float v = -5f; v < 5f; v += 0.37f)
        {
            float w = BackdropMotion.Wrap(v, -0.2f, 1.2f);
            Assert.That(w, Is.InRange(-0.2f, 1.2f));
        }
    }

    [Test]
    public void Twinkle_StaysBetweenZeroAndOne()
    {
        for (float t = 0f; t < 20f; t += 0.13f)
            Assert.That(BackdropMotion.Twinkle(t, 1.7f), Is.InRange(0f, 1f));
    }

    [Test]
    public void Rally_StaysInsideTheTable()
    {
        for (float t = 0f; t < 60f; t += 0.21f)
        {
            Vector2 p = BackdropMotion.Rally(t, 5f);
            Assert.That(p.x, Is.InRange(0.1f, 0.9f));
            Assert.That(p.y, Is.InRange(0.1f, 0.9f));
        }
    }

    [Test]
    public void Pulse_GrowsFromZeroAndRepeats()
    {
        Assert.AreEqual(0f, BackdropMotion.Pulse(0f, 2f), 0.0001f);
        Assert.AreEqual(0.5f, BackdropMotion.Pulse(1f, 2f), 0.0001f);
        Assert.AreEqual(0.5f, BackdropMotion.Pulse(5f, 2f), 0.0001f);
    }

    [Test]
    public void Wobble_StaysNearOne()
    {
        for (float t = 0f; t < 40f; t += 0.29f)
        {
            Vector2 s = BackdropMotion.Wobble(t, 0.8f);
            Assert.That(s.x, Is.InRange(0.88f, 1.12f));
            Assert.That(s.y, Is.InRange(0.88f, 1.12f));
        }
    }

    [Test]
    public void For_MapsEachMode()
    {
        Assert.AreEqual(BackdropScene.Arcade, BackdropScenes.For(GameModeKind.Arcade));
        Assert.AreEqual(BackdropScene.Adventure, BackdropScenes.For(GameModeKind.Adventure));
        Assert.AreEqual(BackdropScene.Rush, BackdropScenes.For(GameModeKind.Rush));
        Assert.AreEqual(BackdropScene.Rush, BackdropScenes.For(GameModeKind.RushBattle));
        Assert.AreEqual(BackdropScene.Classic, BackdropScenes.For(GameModeKind.Classic));
        Assert.AreEqual(BackdropScene.Classic, BackdropScenes.For(GameModeKind.LiveDuel));
    }
}
