using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class LevelPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private PlayerLevel level;
    private CosmeticsService cosmetics;

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private static void Press(Button button)
    {
        using (var e = NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    private IEnumerator Load()
    {
        yield return LoadGame();
        level = Object.FindFirstObjectByType<PlayerLevel>();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(level, "The scene has no PlayerLevel");
        Assert.IsNotNull(level.RoadAsset, "PlayerLevel has no road");
        Object.FindFirstObjectByType<DailyMissions>().enabled = false;
    }

    private IEnumerator PlayRun(string mode, int hits)
    {
        Game.SelectMode(Game.FindMode(mode));
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        for (int i = 0; i < hits; i++) Game.RegisterPaddleHit(0.6f);
        Game.GameOver();
        yield return null;
    }

    private int XpToReach(int target)
    {
        int xp = 0;
        for (int step = 1; step < target; step++) xp += level.RoadAsset.XpToNext(step);
        return xp;
    }

    [UnityTest]
    public IEnumerator Run_TurnsTheScoreIntoXp()
    {
        yield return Load();
        Assert.AreEqual(1, level.Level);

        yield return PlayRun("classic", 12);
        int score = Game.ScoreManager.Score;
        Assert.Greater(score, 0);
        Assert.AreEqual(score, level.RunXp);
        Assert.AreEqual(score, cosmetics.Xp);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator LevelUp_PaysGoldAndShowsOnGameOver()
    {
        yield return Load();
        level.Add(XpToReach(2) - 5);
        Assert.AreEqual(1, level.Level);

        int gold = cosmetics.Gold;
        yield return PlayRun("arcade", 10);
        Assert.AreEqual(2, level.Level);
        Assert.AreEqual(1, level.RunRewards.Count);

        var purse = Object.FindFirstObjectByType<RunPurse>();
        int reward = level.RoadAsset.RewardFor(2).Gold;
        Assert.AreEqual(gold + purse.LastRunGold + reward, cosmetics.Gold);

        yield return Until(() => Root.Q("game-over").resolvedStyle.display == DisplayStyle.Flex);
        Assert.AreEqual(DisplayStyle.Flex, Root.Q("xp-row").resolvedStyle.display);
        Assert.AreEqual("Level 2!  +" + reward + " gold", Root.Q<Label>("xp-text").text);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator SmallRun_ShowsTheXpItGave()
    {
        yield return Load();
        yield return PlayRun("arcade", 6);

        yield return Until(() => Root.Q("game-over").resolvedStyle.display == DisplayStyle.Flex);
        Assert.AreEqual("+" + level.RunXp + " XP", Root.Q<Label>("xp-text").text);
        Assert.AreEqual("1", Root.Q<Label>("xp-level").text);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Milestones_UnlockTheRoadCostumes()
    {
        yield return Load();
        var catalog = cosmetics.CatalogAsset;
        var comet = catalog.Find("paddle_comet");
        var champ = catalog.Find("ball_champ");
        Assert.IsFalse(cosmetics.IsUnlocked(comet));

        level.Add(XpToReach(10));
        Assert.AreEqual(10, level.Level);
        Assert.IsTrue(cosmetics.IsUnlocked(comet));
        Assert.IsFalse(cosmetics.IsUnlocked(champ));
        CollectionAssert.Contains(cosmetics.PendingRewards, comet);

        int stars = cosmetics.Stars;
        level.Add(XpToReach(15) - cosmetics.Xp);
        Assert.AreEqual(15, level.Level);
        Assert.AreEqual(stars + level.RoadAsset.RewardFor(15).Stars, cosmetics.Stars);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FinishedEvent_GivesXp()
    {
        yield return Load();
        var events = Object.FindFirstObjectByType<ArcadeEvents>();

        Game.SelectMode(Game.FindMode("arcade"));
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        Game.Ball.LaunchNow();
        yield return null;

        int before = cosmetics.Xp;
        events.Begin(EventKind.Targets);
        Object.FindFirstObjectByType<TargetBoard>().TakeAll();
        Assert.AreEqual(before + 15, cosmetics.Xp, "A finished event gives XP");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Menu_ShowsTheLevelAndOpensTheRoad()
    {
        yield return Load();
        level.Add(XpToReach(3) + 10);
        yield return null;

        Assert.AreEqual("3", Root.Q<Label>("level-number").text);
        Press(Root.Q<Button>("level-button"));
        yield return null;

        var road = Root.Q("road");
        Assert.AreEqual(DisplayStyle.Flex, road.resolvedStyle.display);
        Assert.AreEqual("3", road.Q<Label>("road-level").text);
        var rows = road.Q<ScrollView>("road-list").contentContainer;
        Assert.AreEqual(level.RoadAsset.CapLevel - 1, rows.childCount);
        Assert.IsTrue(rows[1].ClassListContains("road-step--done"));
        Assert.IsTrue(rows[2].ClassListContains("road-step--next"));
        Assert.IsTrue(rows[8].ClassListContains("road-step--item"), "Level 10 should show the costume");

        Press(road.Q<Button>("road-back-button"));
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.AreEqual(DisplayStyle.None, road.resolvedStyle.display);
    }

    [UnityTest]
    public IEnumerator ReturningPlayer_GetsXpForPastPlayOnce()
    {
        yield return Load();
        var records = Game.RecordsSource;
        for (int i = 0; i < 300; i++) records.AddHit();

        cosmetics.RestoreInventory("{}");
        Assert.AreEqual(300, level.SeedFromHistory());
        Assert.AreEqual(300, cosmetics.Xp);
        Assert.Greater(level.Level, 1);
        Assert.AreEqual(0, level.SeedFromHistory(), "The gift is given once");

        for (int i = 0; i < 5000; i++) records.AddHit();
        cosmetics.RestoreInventory("{}");
        level.SeedFromHistory();
        Assert.AreEqual(9, level.Level, "Past play stops before the first road costume");
        Assert.IsFalse(cosmetics.IsUnlocked(cosmetics.CatalogAsset.Find("paddle_comet")));
    }

    [UnityTest]
    public IEnumerator CloudMerge_KeepsTheHigherLevel()
    {
        yield return Load();
        level.Add(XpToReach(4));

        var cloud = CosmeticInventory.FromJson(null, "{}");
        cloud.AddXp(XpToReach(12));
        cloud.ReachLevel(12);
        var phone = CosmeticInventory.FromJson(null, cosmetics.Inventory.ToJson());
        phone.MergeFrom(cloud);
        cosmetics.RestoreInventory(phone.ToJson());

        Assert.AreEqual(12, level.Level);
        Assert.IsTrue(cosmetics.IsUnlocked(cosmetics.CatalogAsset.Find("paddle_comet")), "Road costumes come back from the cloud");
        yield return null;
    }
}
