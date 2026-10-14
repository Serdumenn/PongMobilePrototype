using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class MissionsPlayTests : GameSceneFixture
{
    private DailyMissions missions;
    private CosmeticsService cosmetics;

    private IEnumerator Load(DateTime day)
    {
        yield return LoadGame();
        missions = UnityEngine.Object.FindFirstObjectByType<DailyMissions>();
        cosmetics = UnityEngine.Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(missions, "The scene has no DailyMissions");
        Assert.IsNotNull(cosmetics);
        missions.Clock = () => day;
        missions.Refresh();
    }

    private static DateTime DayWith(MissionKind kind)
    {
        var day = new DateTime(2026, 10, 15);
        for (int i = 0; i < 400; i++, day = day.AddDays(1))
            foreach (var mission in MissionBook.For(day, true))
                if (mission.Kind == kind) return day;

        Assert.Fail("No day has " + kind);
        return day;
    }

    private int IndexOf(MissionKind kind)
    {
        for (int i = 0; i < missions.Count; i++)
            if (missions.Mission(i).Kind == kind) return i;
        return -1;
    }

    private static VisualElement Root => UnityEngine.Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static void Press(Button button)
    {
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    [UnityTest]
    public IEnumerator FinishingAllMissions_PaysAStarEachAndABonus()
    {
        yield return Load(new DateTime(2026, 10, 15));

        int completed = 0;
        int bonus = 0;
        missions.Completed += _ => completed++;
        missions.AllCompleted += () => bonus++;
        int before = cosmetics.Stars;

        for (int i = 0; i < missions.Count; i++)
        {
            var mission = missions.Mission(i);
            missions.Add(mission.Kind, mission.Target);
        }

        Assert.AreEqual(3, missions.DoneCount);
        Assert.AreEqual(3, completed);
        Assert.AreEqual(1, bonus);
        Assert.AreEqual(before + 4, cosmetics.Stars);

        missions.Add(missions.Mission(0).Kind, 50);
        Assert.AreEqual(before + 4, cosmetics.Stars, "Stars were paid twice");
    }

    [UnityTest]
    public IEnumerator RealHits_MoveTheHitsMission()
    {
        yield return Load(DayWith(MissionKind.Hits));
        int index = IndexOf(MissionKind.Hits);

        Game.StartGameFromMenu();
        for (int i = 0; i < 6; i++) Game.RegisterPaddleHit(0.5f);

        Assert.AreEqual(6, missions.Progress(index));
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator RunsMission_CountsFinishedRuns()
    {
        yield return Load(DayWith(MissionKind.Runs));
        int index = IndexOf(MissionKind.Runs);

        Game.StartGameFromMenu();
        Game.OnBallMissed();
        Assert.AreEqual(1, missions.Progress(index));
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator NewDay_BringsFreshMissions()
    {
        var first = new DateTime(2026, 10, 15);
        yield return Load(first);
        missions.Add(missions.Mission(0).Kind, 1);
        Assert.AreEqual(MissionBook.DayKey(first), missions.Day);

        var next = first.AddDays(1);
        missions.Clock = () => next;
        missions.Refresh();

        Assert.AreEqual(MissionBook.DayKey(next), missions.Day);
        for (int i = 0; i < missions.Count; i++) Assert.AreEqual(0, missions.Progress(i));
        Assert.AreEqual(0, missions.DoneCount);
    }

    [UnityTest]
    public IEnumerator Menu_OpensTheMissionsCard()
    {
        yield return Load(new DateTime(2026, 10, 15));
        yield return null;

        var count = Root.Q("menu").Q<Label>("missions-count");
        Assert.AreEqual("0/3", count.text);

        Press(Root.Q("menu").Q<Button>("missions-button"));
        yield return null;

        var card = Root.Q("missions");
        Assert.AreEqual(DisplayStyle.Flex, card.resolvedStyle.display);
        for (int i = 0; i < 3; i++)
        {
            Assert.IsFalse(string.IsNullOrEmpty(card.Q<Label>($"mission-{i}-text").text), "Mission text " + i);
            Assert.AreEqual("0/" + missions.Mission(i).Target, card.Q<Label>($"mission-{i}-count").text);
        }

        Press(card.Q<Button>("back-button"));
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.AreEqual(DisplayStyle.None, card.resolvedStyle.display);
    }

    [UnityTest]
    public IEnumerator Stars_UnlockTheRewardsOneByOne()
    {
        yield return Load(new DateTime(2026, 10, 15));

        var starry = cosmetics.CatalogAsset.Find("paddle_starry");
        var stella = cosmetics.CatalogAsset.Find("ball_stella");
        Assert.AreEqual(starry, cosmetics.NextStarReward());

        cosmetics.AddStars(15);
        Assert.IsTrue(cosmetics.IsUnlocked(starry));
        Assert.IsFalse(cosmetics.IsUnlocked(stella));
        CollectionAssert.Contains(cosmetics.PendingRewards, starry);
        Assert.AreEqual(stella, cosmetics.NextStarReward());
    }
}
