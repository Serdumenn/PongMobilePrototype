using NUnit.Framework;
using UnityEngine;

public sealed class SoloScoreManagerTests
{
    private const string Key = "__pingi_test_best";

    private GameObject host;
    private SoloScoreManager score;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(Key);
        host = new GameObject("ScoreTest");
        score = host.AddComponent<SoloScoreManager>();
        score.SetBestKey(Key);
        score.ResetScore();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    [Test]
    public void AddPoints_AccumulatesAndNotifies()
    {
        int notified = -1;
        score.ScoreChanged += value => notified = value;

        score.AddPoints(1);
        score.AddPoints(3);

        Assert.AreEqual(4, score.Score);
        Assert.AreEqual(4, notified);
    }

    [Test]
    public void AddPoints_IgnoresNonPositiveAndAfterGameOver()
    {
        score.AddPoints(2);
        score.AddPoints(0);
        score.AddPoints(-5);
        score.GameOver();
        score.AddPoints(10);

        Assert.AreEqual(2, score.Score);
        Assert.IsTrue(score.IsGameOver);
    }

    [Test]
    public void BeatingBest_PersistsImmediatelyAndFlagsNewBest()
    {
        PlayerPrefs.SetInt(Key, 3);
        score.SetBestKey(Key);
        score.ResetScore();

        score.AddPoints(2);
        Assert.AreEqual(3, score.BestScore);
        Assert.AreEqual(3, PlayerPrefs.GetInt(Key));

        score.AddPoints(2);
        Assert.AreEqual(4, score.BestScore);
        Assert.AreEqual(4, PlayerPrefs.GetInt(Key));

        score.GameOver();
        Assert.IsTrue(score.IsNewBest);
        Assert.AreEqual(3, score.PreviousBest);
    }

    [Test]
    public void TyingBest_IsNotNewBest()
    {
        PlayerPrefs.SetInt(Key, 5);
        score.SetBestKey(Key);
        score.ResetScore();

        score.AddPoints(5);
        score.GameOver();

        Assert.IsFalse(score.IsNewBest);
        Assert.AreEqual(5, PlayerPrefs.GetInt(Key));
    }

    [Test]
    public void ZeroScore_IsNeverNewBest()
    {
        score.GameOver();

        Assert.IsFalse(score.IsNewBest);
        Assert.IsFalse(PlayerPrefs.HasKey(Key));
    }

    [Test]
    public void ResetScore_StartsNextRunFromNewBest()
    {
        score.AddPoints(7);
        score.GameOver();

        score.ResetScore();

        Assert.AreEqual(0, score.Score);
        Assert.IsFalse(score.IsGameOver);
        Assert.IsFalse(score.IsNewBest);
        Assert.AreEqual(7, score.PreviousBest);
        Assert.AreEqual(7, score.BestFor(Key));
    }

    [Test]
    public void ResetBests_ClearsStoredBest()
    {
        score.AddPoints(9);

        score.ResetBests(Key);

        Assert.IsFalse(PlayerPrefs.HasKey(Key));
        Assert.AreEqual(0, score.BestScore);
        Assert.AreEqual(0, score.PreviousBest);
        Assert.AreEqual(0, score.BestFor(Key));
    }
}
