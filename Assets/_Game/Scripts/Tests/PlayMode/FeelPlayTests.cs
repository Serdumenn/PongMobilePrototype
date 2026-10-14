using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class FeelPlayTests : GameSceneFixture
{
    private const int SettleFrames = 600;

    private static IEnumerator Settle(System.Func<bool> busy)
    {
        for (int i = 0; i < SettleFrames && busy(); i++) yield return null;
    }

    [UnityTest]
    public IEnumerator Scene_HasFeelOnBallPaddleAndCamera()
    {
        yield return LoadGame();

        Assert.IsNotNull(Game.Ball.GetComponent<BallFeel>(), "The ball has no BallFeel");
        Assert.IsNotNull(Object.FindFirstObjectByType<Paddle>().GetComponent<PaddleFeel>(), "The paddle has no PaddleFeel");
        Assert.IsNotNull(Camera.main.GetComponent<CameraPunch>(), "The camera has no CameraPunch");
        Assert.IsNotNull(GameFeel.Instance, "The scene has no GameFeel");
    }

    [UnityTest]
    public IEnumerator Bounce_SquashesTheBallThenSettles()
    {
        yield return LoadGame();

        var feel = Game.Ball.GetComponent<BallFeel>();
        Vector3 rest = feel.RestScale;

        float lowest = rest.y;
        float widest = rest.x;
        feel.Bounce(Vector2.up, true);
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            lowest = Mathf.Min(lowest, feel.VisualTransform.localScale.y);
            widest = Mathf.Max(widest, feel.VisualTransform.localScale.x);
        }

        Assert.Less(lowest, rest.y * 0.95f);
        Assert.Greater(widest, rest.x * 1.02f);

        yield return Settle(() => feel.Animating);
        yield return null;
        Assert.IsFalse(feel.Animating);
        Vector3 settled = feel.VisualTransform.localScale;
        Assert.AreEqual(rest.x, settled.x, 0.001f);
        Assert.AreEqual(rest.y, settled.y, 0.001f);
    }

    [UnityTest]
    public IEnumerator PaddleKick_MovesOnlyTheVisualAndReturns()
    {
        yield return LoadGame();

        var paddle = Object.FindFirstObjectByType<Paddle>();
        var kick = paddle.GetComponent<PaddleFeel>();
        var visual = paddle.Visual.transform;
        Vector3 rest = visual.localPosition;
        Vector3 body = paddle.transform.position;

        float lowest = rest.y;
        kick.Kick();
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            lowest = Mathf.Min(lowest, visual.localPosition.y);
        }

        Assert.Less(lowest, rest.y - 0.02f);
        Assert.AreEqual(body.y, paddle.transform.position.y, 0.0001f);

        yield return Settle(() => kick.Animating);
        Assert.IsFalse(kick.Animating);
        Assert.AreEqual(rest.y, visual.localPosition.y, 0.001f);
    }

    [UnityTest]
    public IEnumerator Clone_MadeMidSquashStillRestsAtTheOriginalSize()
    {
        yield return LoadGame();

        var feel = Game.Ball.GetComponent<BallFeel>();
        Vector3 rest = feel.RestScale;

        feel.Bounce(Vector2.up, true);
        yield return null;
        Assert.Less(feel.VisualTransform.localScale.y, rest.y * 0.95f);

        var clone = Object.Instantiate(Game.Ball.gameObject, Game.Ball.transform.parent);
        var cloneFeel = clone.GetComponent<BallFeel>();
        Assert.AreEqual(rest, cloneFeel.RestScale);
        Assert.AreEqual(rest, cloneFeel.VisualTransform.localScale);
        Assert.AreNotSame(feel.VisualTransform, cloneFeel.VisualTransform);

        Object.Destroy(clone);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PerfectHit_BurstsWhereTheBallWasStruck()
    {
        yield return LoadGame();

        Time.timeScale = 1f;
        var feel = GameFeel.Instance;
        var struck = new Vector2(1.5f, -3.8f);
        feel.Clear();
        feel.Impact(struck, Vector2.up, Color.white, true);
        int afterImpact = feel.ActiveCount;

        Game.SelectMode(Game.FindMode("rush"));
        Game.StartGameFromMenu();
        Game.RegisterPaddleHit(0f);
        yield return null;

        Assert.Greater(feel.ActiveCount, afterImpact, "A perfect hit added no sparks");
        foreach (var view in feel.GetComponentsInChildren<SpriteRenderer>())
            if (view.enabled && view.name.StartsWith("fx_")) Assert.Less(Vector2.Distance(view.transform.position, struck), 1f, view.name + " appeared away from the hit");

        yield return Settle(() => feel.Slowed);
        Assert.AreEqual(1f, Time.timeScale, 0.0001f);
        Game.ReturnToMenu();
    }

    [UnityTest]
    public IEnumerator Impact_UsesThePoolAndGivesItBack()
    {
        yield return LoadGame();

        var feel = GameFeel.Instance;
        feel.Clear();
        feel.Impact(Vector2.zero, Vector2.up, Color.white, true);
        Assert.GreaterOrEqual(feel.ActiveCount, 5);

        yield return Settle(() => feel.ActiveCount > 0);
        Assert.AreEqual(0, feel.ActiveCount);
    }

    [UnityTest]
    public IEnumerator Perfect_FreezesBrieflyThenTimeRunsAgain()
    {
        yield return LoadGame();

        Time.timeScale = 1f;
        var feel = GameFeel.Instance;
        feel.Perfect(Vector2.zero, Vector2.up);
        Assert.IsTrue(feel.Slowed);
        Assert.Less(Time.timeScale, 0.5f);

        yield return new WaitForSecondsRealtime(0.3f);
        Assert.IsFalse(feel.Slowed);
        Assert.AreEqual(1f, Time.timeScale, 0.0001f);
    }

    [UnityTest]
    public IEnumerator Freeze_LeavesAPausedGameAlone()
    {
        yield return LoadGame();

        Time.timeScale = 0f;
        GameFeel.Instance.Freeze(0.2f);
        Assert.IsFalse(GameFeel.Instance.Slowed);

        yield return new WaitForSecondsRealtime(0.3f);
        Assert.AreEqual(0f, Time.timeScale, 0.0001f);
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator Trail_RunsOnlyWhileTheBallIsInPlay()
    {
        yield return LoadGame();

        var ball = Game.Ball;
        var feel = ball.GetComponent<BallFeel>();
        Assert.IsFalse(feel.TrailEmitting);

        Game.StartGameFromMenu();
        ball.ServeToward(ball.Position, Vector2.up);
        Assert.IsTrue(feel.TrailEmitting);

        Game.ReturnToMenu();
        yield return null;
        Assert.IsFalse(feel.TrailEmitting);
    }

    [UnityTest]
    public IEnumerator CameraPunch_NeverLeavesTheCameraMoved()
    {
        yield return LoadGame();

        var cam = Camera.main;
        var punch = cam.GetComponent<CameraPunch>();
        Vector3 rest = cam.transform.localPosition;

        punch.Kick(Vector2.down, 0.1f);
        Assert.AreNotEqual(Vector2.zero, punch.Offset);
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            Assert.AreEqual(rest, cam.transform.localPosition, "Game logic saw a moved camera");
        }

        yield return Settle(() => punch.Animating);
        Assert.IsFalse(punch.Animating);
        Assert.AreEqual(Vector2.zero, punch.Offset);
    }
}
