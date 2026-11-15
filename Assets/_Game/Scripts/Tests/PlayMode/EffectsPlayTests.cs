using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EffectsPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private CosmeticsService cosmetics;
    private BallFeel feel;
    private TrailRenderer trail;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private IEnumerator Load()
    {
        yield return LoadGame();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        feel = Game.Ball.GetComponent<BallFeel>();
        trail = Game.Ball.GetComponentInChildren<TrailRenderer>(true);
        Assert.IsNotNull(cosmetics);
        Assert.IsNotNull(feel);
        Assert.IsNotNull(trail);
    }

    private CosmeticItem Wear(string id)
    {
        var item = cosmetics.CatalogAsset.Find(id);
        Assert.IsNotNull(item, id);
        cosmetics.Inventory.Grant(id);
        Assert.IsTrue(cosmetics.Equip(item), id);
        return item;
    }

    private static void Near(Color expected, Color actual, string what)
    {
        Assert.AreEqual(expected.r, actual.r, 0.02f, what + " red");
        Assert.AreEqual(expected.g, actual.g, 0.02f, what + " green");
        Assert.AreEqual(expected.b, actual.b, 0.02f, what + " blue");
    }

    private IEnumerator Serve()
    {
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
        Assert.IsTrue(Game.Ball.WaitingForServe, "The ball never got ready to serve");
        Game.Ball.LaunchNow();
    }

    [UnityTest]
    public IEnumerator DefaultTrail_KeepsTheBallsOwnColour()
    {
        yield return Load();
        Assert.IsTrue(feel.TrailLook == null || feel.TrailLook.Unlock == UnlockKind.Free);
        Near(feel.AccentColor, trail.colorGradient.colorKeys[trail.colorGradient.colorKeys.Length - 1].color, "tail");
    }

    [UnityTest]
    public IEnumerator HeartTrail_PaintsItsColoursAndDropsHearts()
    {
        yield return Load();
        var hearts = Wear("trail_hearts");
        yield return null;

        Assert.AreSame(hearts, feel.TrailLook);
        var keys = trail.colorGradient.colorKeys;
        Near(hearts.TrailHead, keys[0].color, "head");
        Near(hearts.TrailTail, keys[keys.Length - 1].color, "tail");

        yield return Serve();
        GameFeel.Instance.Clear();
        float until = Time.unscaledTime + 1f;
        while (Time.unscaledTime < until) yield return null;
        Assert.Greater(GameFeel.Instance.CountUsing(hearts.TrailShape), 0, "No hearts behind the ball");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator RainbowTrail_HasFiveColours()
    {
        yield return Load();
        Wear("trail_rainbow");
        yield return null;
        Assert.AreEqual(5, trail.colorGradient.colorKeys.Length);
    }

    [UnityTest]
    public IEnumerator StarHit_BurstsItsOwnShapes()
    {
        yield return Load();
        var stars = Wear("hit_stars");
        yield return null;
        Assert.AreSame(stars, feel.HitLook);

        GameFeel.Instance.Clear();
        GameFeel.Instance.StyledImpact(Vector2.zero, Vector2.up, stars);
        Assert.AreEqual(stars.HitCount, GameFeel.Instance.CountUsing(stars.HitShape));
    }

    [UnityTest]
    public IEnumerator DefaultParty_StaysConfetti()
    {
        yield return Load();
        Assert.IsFalse(GameFeel.Instance.PartyStyled);
    }

    [UnityTest]
    public IEnumerator Fireworks_ReplaceTheConfettiAtANewBest()
    {
        yield return Load();
        var fireworks = Wear("party_fireworks");
        yield return null;
        Assert.IsTrue(GameFeel.Instance.PartyStyled);

        PlayerPrefs.SetInt(Game.CurrentMode.BestScoreKey, 3);
        Game.RefreshBest();
        Game.StartGameFromMenu();
        GameFeel.Instance.Clear();
        for (int i = 0; i < 4; i++) Game.RegisterPaddleHit(0.5f);

        Assert.GreaterOrEqual(GameFeel.Instance.CountUsing(fireworks.CelebrationShape), 30, "No fireworks");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator PingiDance_UsesTheEquippedFace()
    {
        yield return Load();
        Wear("ball_kitty");
        Wear("party_pingi");
        yield return null;

        Assert.AreSame(cosmetics.Equipped(CosmeticCategory.Ball).Happy, GameFeel.Instance.PartyFace);
        GameFeel.Instance.Clear();
        GameFeel.Instance.Party(Vector2.zero, 10);
        Assert.GreaterOrEqual(GameFeel.Instance.CountUsing(GameFeel.Instance.PartyFace), 6);
    }
}
