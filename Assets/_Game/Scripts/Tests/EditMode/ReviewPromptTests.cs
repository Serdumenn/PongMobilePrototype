using System;
using NUnit.Framework;
using UnityEngine;

public sealed class ReviewPromptTests
{
    private static readonly DateTime Now = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);

    private bool hadDays;
    private int days;
    private string lastDay;
    private string lastAsked;
    private int requests;

    [SetUp]
    public void SetUp()
    {
        hadDays = PlayerPrefs.HasKey(ReviewPrompt.DaysKey);
        days = PlayerPrefs.GetInt(ReviewPrompt.DaysKey, 0);
        lastDay = PlayerPrefs.GetString(ReviewPrompt.LastDayKey, null);
        lastAsked = PlayerPrefs.GetString(ReviewPrompt.LastAskedKey, null);
        PlayerPrefs.DeleteKey(ReviewPrompt.DaysKey);
        PlayerPrefs.DeleteKey(ReviewPrompt.LastDayKey);
        PlayerPrefs.DeleteKey(ReviewPrompt.LastAskedKey);
        requests = 0;
        InAppReview.Requested += Count;
    }

    [TearDown]
    public void TearDown()
    {
        InAppReview.Requested -= Count;
        Restore(ReviewPrompt.LastDayKey, lastDay);
        Restore(ReviewPrompt.LastAskedKey, lastAsked);
        if (hadDays) PlayerPrefs.SetInt(ReviewPrompt.DaysKey, days);
        else PlayerPrefs.DeleteKey(ReviewPrompt.DaysKey);
        PlayerPrefs.Save();
    }

    private void Count()
    {
        requests++;
    }

    private static void Restore(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(key);
        else PlayerPrefs.SetString(key, value);
    }

    [Test]
    public void NoteRun_CountsEachDayOnce()
    {
        ReviewPrompt.NoteRun(new DateTime(2026, 10, 8, 9, 0, 0));
        ReviewPrompt.NoteRun(new DateTime(2026, 10, 8, 21, 0, 0));
        ReviewPrompt.NoteRun(new DateTime(2026, 10, 9, 9, 0, 0));

        Assert.AreEqual(2, ReviewPrompt.DaysPlayed);
    }

    [Test]
    public void TryAsk_RequestsOnce_ThenWaitsForCooldown()
    {
        PlayerPrefs.SetInt(ReviewPrompt.DaysKey, ReviewGate.MinDays);

        Assert.IsTrue(ReviewPrompt.TryAsk(true, ReviewGate.MinGames, Now));
        Assert.IsFalse(ReviewPrompt.TryAsk(true, ReviewGate.MinGames, Now.AddDays(1)));
        Assert.IsTrue(ReviewPrompt.TryAsk(true, ReviewGate.MinGames, Now.AddDays(ReviewGate.CooldownDays)));

        Assert.AreEqual(2, requests);
    }

    [Test]
    public void TryAsk_DoesNothing_ForNewPlayers()
    {
        ReviewPrompt.NoteRun(new DateTime(2026, 10, 8, 9, 0, 0));

        Assert.IsFalse(ReviewPrompt.TryAsk(true, 1, Now));
        Assert.AreEqual(0, requests);
        Assert.AreEqual(default(DateTime), ReviewPrompt.LastAskedUtc);
    }
}
