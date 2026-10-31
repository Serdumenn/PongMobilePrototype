using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AdventureTests
{
    private const string BookPath = "Assets/_Game/Data/adventure_book.asset";

    private static AdventureBook Book()
    {
        var book = AssetDatabase.LoadAssetAtPath<AdventureBook>(BookPath);
        Assert.IsNotNull(book, "Missing adventure book");
        return book;
    }

    [Test]
    public void Book_HasThreeWorldsOfTenStages()
    {
        var book = Book();
        Assert.AreEqual(3, book.Worlds.Count);
        foreach (var world in book.Worlds)
        {
            Assert.AreEqual(10, world.Levels.Count, world.Name);
            Assert.IsFalse(string.IsNullOrEmpty(world.Icon), world.Name);
        }

        Assert.AreEqual(30, book.Count);
        Assert.AreEqual(90, book.MaxStars);
        Assert.AreEqual("1-1", book.Label(0));
        Assert.AreEqual("2-1", book.Label(10));
        Assert.AreEqual("3-10", book.Label(29));
        Assert.IsTrue(book.IsBoss(9));
        Assert.IsFalse(book.IsBoss(10));
        Assert.IsNull(book.Level(30));
    }

    [Test]
    public void Stages_AreWinnableAndStayOnTheField()
    {
        var book = Book();
        for (int i = 0; i < book.Count; i++)
        {
            var level = book.Level(i);
            string id = book.Label(i);
            Assert.Greater(level.Goals, 0, id);
            Assert.AreEqual(3, level.Lives, id);
            Assert.Greater(level.Gold, 0, id);
            Assert.That(level.Speed, Is.InRange(1f, 1.3f), id);
            Assert.That(level.BallScale, Is.InRange(1f, 1.7f), id);
            Assert.LessOrEqual(level.Bumpers.Count, 6, id);
            Assert.That(level.TargetScale, Is.InRange(0.5f, 1f), id);
            Assert.LessOrEqual(level.Goals, level.Goal == AdventureGoal.Targets ? 7 : 40, id);
            if (level.Goal == AdventureGoal.Targets && level.TargetSpots.Count > 0)
                Assert.AreEqual(level.Target, level.TargetSpots.Count, id + " target count should match its spots");

            foreach (var spot in level.TargetSpots)
                Assert.IsTrue(spot.x >= -1f && spot.x <= 1f && spot.y >= 0f && spot.y <= 1f, id);
            foreach (var spot in level.Bumpers)
                Assert.IsTrue(spot.x >= -1f && spot.x <= 1f && spot.y >= 0f && spot.y <= 1f, id);
        }
    }

    [Test]
    public void Worlds_IntroduceTheirMechanics()
    {
        var book = Book();
        for (int i = 0; i < 10; i++) Assert.AreEqual(0, book.Level(i).Bumpers.Count, "World 1 has no bumpers");

        int bumpers = 0;
        for (int i = 10; i < 20; i++) if (book.Level(i).Bumpers.Count > 0) bumpers++;
        Assert.GreaterOrEqual(bumpers, 8, "World 2 is about bumpers");

        int moving = 0;
        for (int i = 20; i < 30; i++) if (book.Level(i).TargetSway > 0f || book.Level(i).Speed > 1.05f) moving++;
        Assert.GreaterOrEqual(moving, 7, "World 3 is about moving targets and speed");
    }

    [Test]
    public void Map_OpensStagesOneByOne()
    {
        var stars = new Dictionary<int, int>();
        System.Func<int, int> starsAt = i => stars.TryGetValue(i, out int s) ? s : 0;

        Assert.IsTrue(AdventureMap.IsOpen(0, starsAt));
        Assert.IsFalse(AdventureMap.IsOpen(1, starsAt));
        Assert.AreEqual(0, AdventureMap.Next(30, starsAt));

        stars[0] = 1;
        Assert.IsTrue(AdventureMap.IsOpen(1, starsAt));
        Assert.IsFalse(AdventureMap.IsOpen(2, starsAt));
        Assert.AreEqual(1, AdventureMap.Next(30, starsAt));

        for (int i = 0; i < 30; i++) stars[i] = 3;
        Assert.AreEqual(29, AdventureMap.Next(30, starsAt));
    }

    [Test]
    public void Stars_FollowTheTriesLeft()
    {
        Assert.AreEqual(3, AdventureMap.StarsFor(3));
        Assert.AreEqual(2, AdventureMap.StarsFor(2));
        Assert.AreEqual(1, AdventureMap.StarsFor(1));
        Assert.AreEqual(1, AdventureMap.StarsFor(0));
        Assert.AreEqual(3, AdventureMap.StarsFor(5));
    }

    [Test]
    public void Inventory_KeepsTheBestStarsThroughAMerge()
    {
        var phone = CosmeticInventory.FromJson(null, "{}");
        Assert.IsTrue(phone.RecordLevel(0, 2));
        Assert.IsFalse(phone.RecordLevel(0, 1), "Fewer stars never replace more");
        Assert.IsTrue(phone.RecordLevel(4, 3));
        Assert.AreEqual(0, phone.LevelStarsAt(2));
        Assert.AreEqual(5, phone.TotalLevelStars);

        var cloud = CosmeticInventory.FromJson(null, "{}");
        cloud.RecordLevel(0, 3);
        cloud.RecordLevel(1, 1);
        phone.MergeFrom(cloud);
        Assert.AreEqual(3, phone.LevelStarsAt(0));
        Assert.AreEqual(1, phone.LevelStarsAt(1));
        Assert.AreEqual(3, phone.LevelStarsAt(4));

        var copy = CosmeticInventory.FromJson(null, phone.ToBackupJson());
        Assert.AreEqual(7, copy.TotalLevelStars);
    }

    [Test]
    public void Rules_CountTriesAndTheClock()
    {
        var mode = TestData.LoadMode(TestData.ClassicModePath);
        var level = Book().Level(6);
        Assert.AreEqual(AdventureGoal.Survive, level.Goal);

        var rules = new AdventureRules(mode, level);
        var player = new Participant(0, FieldSide.Bottom, null);
        rules.Begin(new[] { player });
        Assert.AreEqual(3, rules.Lives);
        Assert.IsTrue(rules.HasClock);

        Assert.IsFalse(rules.Tick(5f), "The clock waits for the first serve");
        rules.OnLaunched();
        Assert.IsFalse(rules.Tick(10f));
        Assert.AreEqual(level.Target - 10f, rules.TimeRemaining, 0.001f);

        Assert.IsFalse(rules.OnMiss(player));
        Assert.IsFalse(rules.OnMiss(player));
        Assert.IsTrue(rules.MissWouldEnd(player));
        Assert.IsTrue(rules.OnMiss(player));
        Assert.AreEqual(0, rules.Lives);

        rules.OnRevived();
        Assert.AreEqual(1, rules.Lives);
        Assert.IsTrue(rules.Tick(level.Target));
        Assert.IsTrue(rules.Survived);
    }
}
