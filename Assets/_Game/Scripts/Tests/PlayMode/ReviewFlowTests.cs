using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ReviewFlowTests : GameSceneFixture
{
    private const float PromptWait = 2.4f;

    private int requests;

    private void Count()
    {
        requests++;
    }

    private IEnumerator PlayToNewBest(int daysPlayed, int earlierGames)
    {
        yield return LoadGame();

        PlayerPrefs.SetInt(ReviewPrompt.DaysKey, daysPlayed);
        for (int i = 0; i < earlierGames; i++) Game.RecordsSource.RecordRun(Game.CurrentMode, false, 0, 1f);

        requests = 0;
        InAppReview.Requested += Count;

        Game.StartGameFromMenu();
        Game.RegisterPaddleHit(0f);
        Game.RegisterPaddleHit(0f);
        Game.OnBallMissed();
        Assert.IsTrue(Game.ScoreManager.IsNewBest);

        yield return new WaitForSecondsRealtime(PromptWait);
        InAppReview.Requested -= Count;
    }

    [UnityTest]
    public IEnumerator NewBest_AsksEngagedPlayersOnce()
    {
        yield return PlayToNewBest(ReviewGate.MinDays, ReviewGate.MinGames);

        Assert.AreEqual(1, requests);
        Assert.AreNotEqual(default(System.DateTime), ReviewPrompt.LastAskedUtc);
    }

    [UnityTest]
    public IEnumerator NewBest_DoesNotAskNewPlayers()
    {
        yield return PlayToNewBest(0, 0);

        Assert.AreEqual(0, requests);
        Assert.AreEqual(1, ReviewPrompt.DaysPlayed);
    }
}
