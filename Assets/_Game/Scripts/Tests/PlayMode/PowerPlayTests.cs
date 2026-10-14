using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class PowerPlayTests : GameSceneFixture
{
    private const int WaitFrames = 600;

    private PowerUps powers;

    private static IEnumerator Until(System.Func<bool> done)
    {
        for (int i = 0; i < WaitFrames && !done(); i++) yield return null;
    }

    private IEnumerator Load(string mode)
    {
        yield return LoadGame();
        powers = Object.FindFirstObjectByType<PowerUps>();
        Assert.IsNotNull(powers, "The scene has no PowerUps");
        Game.SelectMode(Game.FindMode(mode));
        Game.StartGameFromMenu();
        yield return Until(() => Game.Ball.WaitingForServe);
    }

    [UnityTest]
    public IEnumerator Classic_StaysFreeOfPowerUps()
    {
        yield return Load("classic");
        Assert.IsFalse(powers.Running);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Rush_RunsPowerUpsAndStopsThemAfter()
    {
        yield return Load("rush");
        Assert.IsTrue(powers.Running);

        powers.Activate(PowerKind.BigPaddle);
        powers.Activate(PowerKind.SlowBall);
        var paddle = Game.Player.Paddle;
        Assert.AreEqual(1.5f, paddle.LengthScale, 0.001f);
        Assert.Less(Game.Ball.SpeedBoost, 1f);

        Game.ReturnToMenu();
        Assert.IsFalse(powers.Running);
        Assert.AreEqual(1f, paddle.LengthScale, 0.001f);
        Assert.AreEqual(1f, Game.Ball.SpeedBoost, 0.001f);
    }

    [UnityTest]
    public IEnumerator DoublePoints_DoublesEveryHit()
    {
        yield return Load("rush");

        Game.RegisterPaddleHit(0.6f);
        Assert.AreEqual(1, Game.ScoreManager.Score);

        powers.Activate(PowerKind.DoublePoints);
        Game.RegisterPaddleHit(0.6f);
        Assert.AreEqual(3, Game.ScoreManager.Score);

        powers.EndAll();
        Game.RegisterPaddleHit(0.6f);
        Assert.AreEqual(4, Game.ScoreManager.Score);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Token_IsCollectedByTheBall()
    {
        yield return Load("rush");

        PowerKind? collected = null;
        powers.Collected += kind => collected = kind;
        var spot = new Vector2(0f, 0.5f);
        powers.Spawn(PowerKind.DoublePoints, spot);
        Assert.IsTrue(powers.TokenShown);

        Game.Ball.ServeToward(spot + Vector2.down * 1.2f, Vector2.up);
        yield return Until(() => collected.HasValue || !Game.Ball.InPlay);

        Assert.AreEqual(PowerKind.DoublePoints, collected);
        Assert.IsFalse(powers.TokenShown);
        Assert.IsTrue(powers.IsActive(PowerKind.DoublePoints));
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Shield_TakesOneMissAndBreaks()
    {
        yield return Load("rush");
        float timeBefore = Game.Rules.TimeRemaining;

        powers.Activate(PowerKind.Shield);
        Assert.IsTrue(powers.ShieldUp);

        var ball = Game.Ball;
        Vector2 home = Game.Player.Paddle.transform.position;
        ball.ServeToward(new Vector2(home.x + 1.5f, home.y + 1.4f), Vector2.down);
        yield return Until(() => !powers.ShieldUp || !ball.InPlay);

        Assert.IsFalse(powers.ShieldUp, "The shield never broke");
        Assert.IsTrue(ball.InPlay, "The ball still got through");
        yield return null;
        Assert.Greater(ball.LastVelocity.y, 0f);
        Assert.GreaterOrEqual(Game.Rules.TimeRemaining, timeBefore - 1f, "The miss still cost time");
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Hud_ShowsTheActivePower()
    {
        yield return Load("rush");

        var root = Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("hud");
        Assert.IsTrue(root.Q("power-2").ClassListContains("power--hidden"));

        powers.Activate(PowerKind.DoublePoints);
        yield return null;
        Assert.IsFalse(root.Q("power-2").ClassListContains("power--hidden"));

        powers.EndAll();
        yield return null;
        Assert.IsTrue(root.Q("power-2").ClassListContains("power--hidden"));
        Game.ReturnToMenu();
    }
}
