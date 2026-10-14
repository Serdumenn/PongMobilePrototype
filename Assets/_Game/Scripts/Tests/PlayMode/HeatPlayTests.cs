using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class HeatPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private Ball ServeAndHold()
    {
        var ball = Game.Ball;
        Game.StartGameFromMenu();
        ball.ServeToward(Vector2.zero, Vector2.up);
        Hold(ball);
        return ball;
    }

    private static void Hold(Ball ball)
    {
        ball.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }

    private static void Heat(Ball ball)
    {
        ball.ScaleSpeed(100f);
        Hold(ball);
    }

    [UnityTest]
    public IEnumerator Scene_HasBackdropGlowAndWowFace()
    {
        yield return LoadGame();

        var backdrop = Object.FindFirstObjectByType<BackdropFeel>();
        Assert.IsNotNull(backdrop, "The scene has no BackdropFeel");
        Assert.AreEqual(2, Object.FindObjectsByType<ViewportAnchor>(FindObjectsSortMode.None).Length);
        Assert.IsNotNull(Game.Ball.transform.Find("Glow"), "The ball has no glow");
        Assert.IsNotNull(Game.Ball.transform.Find("Glow").GetComponent<SpriteRenderer>().sprite, "The glow has no sprite");
    }

    [UnityTest]
    public IEnumerator Heat_FollowsThePaceAndResetsOnTheNextServe()
    {
        yield return LoadGame();

        var ball = ServeAndHold();
        Assert.AreEqual(0f, ball.Heat, 0.0001f);

        ball.ScaleSpeed(1.2f);
        Assert.Greater(ball.Heat, 0.1f);
        Assert.Less(ball.Heat, 1f);

        Heat(ball);
        Assert.AreEqual(1f, ball.Heat, 0.0001f);

        ball.ServeToward(Vector2.zero, Vector2.up);
        Assert.AreEqual(0f, ball.Heat, 0.0001f);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Glow_ShowsOnlyOnAFastBallAndGoesWithIt()
    {
        yield return LoadGame();

        var ball = ServeAndHold();
        var feel = ball.GetComponent<BallFeel>();
        float calmTrail = feel.TrailTime;
        yield return null;
        yield return null;
        Assert.IsFalse(feel.GlowVisible, "A slow ball glows");

        Heat(ball);
        yield return Until(() => feel.Heat > 0.6f);
        Assert.IsTrue(feel.GlowVisible, "A fast ball does not glow");
        Assert.Greater(feel.TrailTime, calmTrail);
        Assert.AreNotEqual(feel.AccentColor, feel.HeatColor);

        Game.ReturnToMenu();
        yield return null;
        Assert.IsFalse(feel.GlowVisible, "The glow stayed after the ball left");
        Assert.AreEqual(0f, feel.Heat, 0.0001f);
        Assert.AreEqual(calmTrail, feel.TrailTime, 0.0001f);
    }

    [UnityTest]
    public IEnumerator Face_IsThrilledWhenFastAndCalmAfterANewServe()
    {
        yield return LoadGame();

        var ball = ServeAndHold();
        var face = ball.GetComponent<BallFace>();
        var view = ball.GetComponentInChildren<SpriteRenderer>();
        var look = Object.FindFirstObjectByType<CosmeticsService>().Equipped(CosmeticCategory.Ball);
        yield return null;
        Assert.IsFalse(face.Thrilled);
        Assert.AreEqual(look.Idle, view.sprite);

        Heat(ball);
        yield return null;
        Assert.IsTrue(face.Thrilled);
        Assert.AreEqual(look.Wow, view.sprite);

        ball.ServeToward(Vector2.zero, Vector2.up);
        Hold(ball);
        yield return null;
        Assert.IsFalse(face.Thrilled);
        Assert.AreEqual(look.Idle, view.sprite);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Backdrop_PulsesOnAHitAndSettlesNearItsAnchors()
    {
        yield return LoadGame();
        yield return null;

        var backdrop = Object.FindFirstObjectByType<BackdropFeel>();
        var blobs = Object.FindObjectsByType<ViewportAnchor>(FindObjectsSortMode.None);
        Vector2 reach = backdrop.MaxReach;

        backdrop.Pulse();
        Assert.IsTrue(backdrop.Pulsing);
        float widest = 0f;
        for (int i = 0; i < 4; i++)
        {
            yield return null;
            foreach (var blob in blobs)
                widest = Mathf.Max(widest, blob.transform.localScale.x / blob.AnchoredScale.x);
        }

        Assert.Greater(widest, 1.015f, "The backdrop did not swell on a hit");

        yield return Until(() => !backdrop.Pulsing);
        Assert.IsFalse(backdrop.Pulsing);
        yield return null;
        foreach (var blob in blobs)
        {
            Vector2 offset = blob.transform.position - blob.AnchoredPosition;
            Assert.LessOrEqual(Mathf.Abs(offset.x), reach.x + 0.001f, blob.name);
            Assert.LessOrEqual(Mathf.Abs(offset.y), reach.y + 0.001f, blob.name);
            Assert.AreEqual(1f, blob.transform.localScale.x / blob.AnchoredScale.x, 0.03f, blob.name);
        }
    }

    [UnityTest]
    public IEnumerator Backdrop_KeepsTheThemeColors()
    {
        yield return LoadGame();

        var theme = Object.FindFirstObjectByType<CosmeticsService>().Equipped(CosmeticCategory.Theme);
        var backdrop = Object.FindFirstObjectByType<BackdropFeel>();
        backdrop.Pulse();
        yield return null;
        yield return null;

        int matched = 0;
        foreach (var blob in Object.FindObjectsByType<ViewportAnchor>(FindObjectsSortMode.None))
        {
            Color color = blob.GetComponent<SpriteRenderer>().color;
            if (color == theme.BlobPrimary || color == theme.BlobSecondary) matched++;
        }

        Assert.AreEqual(2, matched);
        Assert.AreEqual(theme.BackgroundColor, Camera.main.backgroundColor);
    }
}
