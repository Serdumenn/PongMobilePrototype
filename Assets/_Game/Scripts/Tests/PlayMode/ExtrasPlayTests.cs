using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ExtrasPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private void Hits(int count, float offset = 0.5f)
    {
        for (int i = 0; i < count; i++) Game.RegisterPaddleHit(offset);
    }

    private static CosmeticsService Cosmetics()
    {
        var cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(cosmetics, "CosmeticsService missing in scene");
        return cosmetics;
    }

    private static PingiBubbles Bubbles()
    {
        var bubbles = Object.FindFirstObjectByType<PingiBubbles>();
        Assert.IsNotNull(bubbles, "The scene has no PingiBubbles");
        return bubbles;
    }

    [UnityTest]
    public IEnumerator Scene_PlaysMusicalHits()
    {
        yield return LoadGame();

        var audio = Object.FindFirstObjectByType<AudioManager>();
        Assert.AreEqual(Melody.Semitones(0, 2f, 6f), audio.RallyNote(0), 0.0001f);
        Assert.AreEqual(Melody.Semitones(1, 2f, 6f), audio.RallyNote(1), 0.0001f);
        Assert.AreEqual(Melody.Semitones(Melody.Length + 2, 2f, 6f), audio.RallyNote(Melody.Length + 2), 0.0001f);
    }

    [UnityTest]
    public IEnumerator Costume_BringsItsBurstAndPremiumSparkle()
    {
        yield return LoadGame();

        var cosmetics = Cosmetics();
        var feel = Game.Ball.GetComponent<BallFeel>();
        Assert.IsNull(feel.BurstSprite);
        Assert.IsFalse(feel.Sparkles);

        var golden = cosmetics.CatalogAsset.Find("ball_golden");
        cosmetics.Inventory.Grant(golden.Id);
        Assert.IsTrue(cosmetics.Equip(golden));
        yield return null;

        Assert.AreEqual(golden.Burst, feel.BurstSprite);
        Assert.IsTrue(feel.Sparkles);
        Assert.AreEqual(golden.Accent.r, feel.AccentColor.r, 0.001f);

        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        GameFeel.Instance.Clear();
        Game.Ball.ServeToward(Vector2.zero, Vector2.up);
        Game.Ball.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        yield return Until(() => GameFeel.Instance.ActiveCount >= 3);
        Assert.GreaterOrEqual(GameFeel.Instance.ActiveCount, 3, "The premium ball left no sparkles");

        var pingi = cosmetics.CatalogAsset.Find("ball_pingi");
        Assert.IsTrue(cosmetics.Equip(pingi));
        yield return null;
        Assert.IsNull(feel.BurstSprite);
        Assert.IsFalse(feel.Sparkles);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Impact_DrawsTheCostumeShape()
    {
        yield return LoadGame();

        var leaf = Cosmetics().CatalogAsset.Find("ball_minty").Burst;
        var feel = GameFeel.Instance;
        feel.Clear();
        feel.Impact(Vector2.zero, Vector2.up, Color.white, true, leaf);

        int leaves = 0;
        foreach (var view in feel.GetComponentsInChildren<SpriteRenderer>())
            if (view.enabled && view.name.StartsWith("fx_") && view.sprite == leaf) leaves++;

        Assert.GreaterOrEqual(leaves, 5);
    }

    [UnityTest]
    public IEnumerator World_GrowsWithTheScoreAndCalmsDownAfter()
    {
        yield return LoadGame();

        var world = Object.FindFirstObjectByType<WorldLevels>();
        Assert.IsNotNull(world, "The scene has no WorldLevels");
        int changes = 0;
        world.LevelChanged += _ => changes++;

        Game.StartGameFromMenu();
        Hits(9);
        Assert.AreEqual(0, world.Level);

        Hits(1);
        Assert.AreEqual(1, world.Level);
        yield return Until(() => world.VisibleMotes >= 5);
        Assert.GreaterOrEqual(world.VisibleMotes, 5);

        Hits(15);
        Assert.AreEqual(2, world.Level);
        Assert.AreEqual(2, changes);

        Game.ReturnToMenu();
        Assert.AreEqual(0, world.Level);
        yield return Until(() => world.VisibleMotes == 0);
        Assert.AreEqual(0, world.VisibleMotes);
    }

    [UnityTest]
    public IEnumerator Bubble_PopsUpThenGoesAway()
    {
        yield return LoadGame();

        var bubbles = Bubbles();
        bubbles.Show(BubbleKind.Heart);
        Assert.IsTrue(bubbles.Showing);
        Assert.AreEqual(BubbleKind.Heart, bubbles.Current);

        bubbles.Show(BubbleKind.Note);
        Assert.AreEqual(BubbleKind.Heart, bubbles.Current, "Smaller news replaced a bigger one");

        bubbles.Show(BubbleKind.Star);
        Assert.AreEqual(BubbleKind.Star, bubbles.Current);

        yield return Until(() => !bubbles.Showing);
        Assert.IsFalse(bubbles.Showing);
    }

    [UnityTest]
    public IEnumerator PerfectStreak_ShowsAHeart()
    {
        yield return LoadGame();

        var bubbles = Bubbles();
        Game.SelectMode(Game.FindMode("rush"));
        Game.StartGameFromMenu();

        Hits(2, 0f);
        Assert.IsFalse(bubbles.Showing && bubbles.Current == BubbleKind.Heart);

        Hits(1, 0f);
        Assert.IsTrue(bubbles.Showing);
        Assert.AreEqual(BubbleKind.Heart, bubbles.Current);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Bubble_StaysOnScreenInTheCorner()
    {
        yield return LoadGame();

        var cam = Camera.main;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector2 center = cam.transform.position;
        Game.Ball.SnapTo(center + new Vector2(halfWidth - 0.2f, halfHeight - 0.2f));

        var bubbles = Bubbles();
        bubbles.Show(BubbleKind.Star);
        for (int i = 0; i < 20; i++) yield return null;

        Bounds shown = bubbles.ShownBounds;
        Assert.GreaterOrEqual(shown.min.x, center.x - halfWidth - 0.001f);
        Assert.LessOrEqual(shown.max.x, center.x + halfWidth + 0.001f);
        Assert.GreaterOrEqual(shown.min.y, center.y - halfHeight - 0.001f);
        Assert.LessOrEqual(shown.max.y, center.y + halfHeight + 0.001f);
    }
}
