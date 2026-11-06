using NUnit.Framework;
using UnityEngine;

public sealed class ModeSwipeTests
{
    [Test]
    public void Decide_CommitsPastAQuarter()
    {
        Assert.AreEqual(1, ModeSwipe.Decide(-160f, 0f, 600f));
        Assert.AreEqual(-1, ModeSwipe.Decide(160f, 0f, 600f));
        Assert.AreEqual(0, ModeSwipe.Decide(-140f, 0f, 600f));
    }

    [Test]
    public void Decide_FlickCountsWhenFastEnough()
    {
        Assert.AreEqual(1, ModeSwipe.Decide(-30f, -1200f, 600f));
        Assert.AreEqual(-1, ModeSwipe.Decide(30f, 1200f, 600f));
        Assert.AreEqual(0, ModeSwipe.Decide(-30f, -500f, 600f));
        Assert.AreEqual(0, ModeSwipe.Decide(-10f, -2000f, 600f));
    }

    [Test]
    public void IsTap_OnlyForTinyMoves()
    {
        Assert.IsTrue(ModeSwipe.IsTap(5f));
        Assert.IsTrue(ModeSwipe.IsTap(-5f));
        Assert.IsFalse(ModeSwipe.IsTap(20f));
    }

    [Test]
    public void Settings_MotionOnAndHintNotDoneByDefault()
    {
        string[] keys = { "MotionBackdrop", "SwipeHintDone" };
        var saved = new int?[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            saved[i] = PlayerPrefs.HasKey(keys[i]) ? PlayerPrefs.GetInt(keys[i]) : (int?)null;
            PlayerPrefs.DeleteKey(keys[i]);
        }

        try
        {
            Assert.IsTrue(GameSettings.MotionBackdrop);
            Assert.IsFalse(GameSettings.SwipeHintDone);
            GameSettings.MotionBackdrop = false;
            GameSettings.SwipeHintDone = true;
            Assert.IsFalse(GameSettings.MotionBackdrop);
            Assert.IsTrue(GameSettings.SwipeHintDone);
        }
        finally
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (saved[i].HasValue) PlayerPrefs.SetInt(keys[i], saved[i].Value);
                else PlayerPrefs.DeleteKey(keys[i]);
            }
        }
    }
}
