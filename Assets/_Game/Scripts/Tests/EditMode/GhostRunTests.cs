using NUnit.Framework;
using UnityEngine;

public sealed class GhostRunTests
{
    private static GhostRun Sample()
    {
        var run = new GhostRun { Seed = 1234567, BallSkin = "ball_kitty", PaddleSkin = "paddle_mint" };
        run.AddBall(0f, new Vector2(0f, -0.6f), GhostRun.KeyKind.Launch);
        run.AddBall(1f, new Vector2(0.5f, 1f), GhostRun.KeyKind.Wall);
        run.AddBall(2f, new Vector2(0.2f, -0.8f), GhostRun.KeyKind.Paddle);
        run.AddScore(2f, 2);
        run.AddBall(3f, new Vector2(-0.4f, 1f), GhostRun.KeyKind.Wall);
        run.AddBall(4f, new Vector2(-0.1f, -1.1f), GhostRun.KeyKind.Out);
        run.AddBall(5f, new Vector2(0f, -0.6f), GhostRun.KeyKind.Appear);
        run.AddBall(5.5f, new Vector2(0f, -0.6f), GhostRun.KeyKind.Launch);
        run.AddBall(6.5f, new Vector2(0.3f, -0.8f), GhostRun.KeyKind.Paddle);
        run.AddScore(6.5f, 3);
        run.AddBall(7f, new Vector2(0.5f, 0f), GhostRun.KeyKind.End);
        for (int i = 0; i <= 70; i++) run.AddPaddle(Mathf.Sin(i * 0.1f));
        run.Finish(7f, 3);
        return run;
    }

    [Test]
    public void RoundTrip_KeepsTheWholeRun()
    {
        var run = Sample();
        var back = GhostRun.FromBytes(run.ToBytes());

        Assert.IsNotNull(back);
        Assert.AreEqual(run.Seed, back.Seed);
        Assert.AreEqual(run.Score, back.Score);
        Assert.AreEqual(run.Duration, back.Duration, 0.01f);
        Assert.AreEqual("ball_kitty", back.BallSkin);
        Assert.AreEqual("paddle_mint", back.PaddleSkin);
        Assert.AreEqual(run.Ball.Count, back.Ball.Count);
        for (int i = 0; i < run.Ball.Count; i++)
        {
            Assert.AreEqual(run.Ball[i].Kind, back.Ball[i].Kind);
            Assert.AreEqual(run.Ball[i].Time, back.Ball[i].Time, 0.01f);
            Assert.AreEqual(run.Ball[i].Position.x, back.Ball[i].Position.x, 0.0002f);
            Assert.AreEqual(run.Ball[i].Position.y, back.Ball[i].Position.y, 0.0002f);
        }
        Assert.AreEqual(run.Scores.Count, back.Scores.Count);
        Assert.AreEqual(run.Paddle.Count, back.Paddle.Count);
        Assert.AreEqual(run.Paddle[20], back.Paddle[20], 0.01f);
        Assert.IsNull(back.Problem());

        var text = GhostRun.FromBase64(run.ToBase64());
        Assert.IsNotNull(text);
        Assert.AreEqual(run.Score, text.Score);
    }

    [Test]
    public void Replay_FollowsTheBallBetweenBounces()
    {
        var run = Sample();

        Assert.IsTrue(run.BallAt(0.5f, out var mid));
        Assert.AreEqual(0.25f, mid.x, 0.001f);
        Assert.AreEqual(0.2f, mid.y, 0.001f);

        Assert.IsFalse(run.BallAt(4.5f, out _), "After a miss the ghost ball is gone");
        Assert.IsTrue(run.BallAt(5.2f, out var waiting), "It waits at the serve point");
        Assert.AreEqual(-0.6f, waiting.y, 0.001f);
        Assert.IsTrue(run.BallAt(6.9f, out _));
        Assert.IsFalse(run.BallAt(7.5f, out _), "Nothing after the run ended");

        Assert.AreEqual(0, run.ScoreAt(1.9f));
        Assert.AreEqual(2, run.ScoreAt(2f));
        Assert.AreEqual(3, run.ScoreAt(60f));
        Assert.AreEqual(Mathf.Sin(1f), run.PaddleAt(1f), 0.001f);
        Assert.AreEqual(run.Paddle[run.Paddle.Count - 1], run.PaddleAt(99f), 0.001f);
    }

    [Test]
    public void Check_RefusesImpossibleRuns()
    {
        Assert.IsNull(Sample().Problem());

        var wrongTotal = Sample();
        wrongTotal.Finish(7f, 9);
        Assert.AreEqual("score", wrongTotal.Problem());

        var bigStep = Sample();
        bigStep.AddScore(6.8f, 30);
        bigStep.Finish(7f, 30);
        Assert.AreEqual("score", bigStep.Problem(), "No hit is worth more than a combo");

        var tooLong = Sample();
        tooLong.Finish(90f, 3);
        Assert.AreEqual("duration", tooLong.Problem());

        var noHits = new GhostRun();
        noHits.AddBall(0f, Vector2.zero, GhostRun.KeyKind.Launch);
        noHits.AddScore(1f, 1);
        noHits.AddScore(2f, 2);
        noHits.Finish(3f, 2);
        Assert.AreEqual("hits", noHits.Problem(), "Points need paddle hits");

        var empty = Sample();
        empty.Finish(7f, 0);
        Assert.AreEqual("empty", empty.Problem());
    }

    [Test]
    public void Decode_RejectsBrokenData()
    {
        Assert.IsNull(GhostRun.FromBytes(null));
        Assert.IsNull(GhostRun.FromBytes(new byte[5]));

        var bytes = Sample().ToBytes();
        var cut = new byte[bytes.Length - 3];
        System.Array.Copy(bytes, cut, cut.Length);
        Assert.IsNull(GhostRun.FromBytes(cut));

        bytes[0] = 99;
        Assert.IsNull(GhostRun.FromBytes(bytes), "Unknown version");
        Assert.IsNull(GhostRun.FromBase64("not base64!"));
    }

    [Test]
    public void FullMinute_FitsTheSizeBudget()
    {
        var run = new GhostRun { Seed = 42, BallSkin = "ball_pingi", PaddleSkin = "paddle_coral" };
        float t = 0f;
        int score = 0;
        run.AddBall(0f, new Vector2(0f, -0.6f), GhostRun.KeyKind.Launch);
        for (int i = 0; i < 160; i++)
        {
            t += 0.37f;
            bool paddle = i % 3 == 2;
            run.AddBall(t, new Vector2(Mathf.Sin(i), paddle ? -0.8f : 1f), paddle ? GhostRun.KeyKind.Paddle : GhostRun.KeyKind.Wall);
            if (paddle)
            {
                score += 1 + i % 3;
                run.AddScore(t, score);
            }
        }
        for (int i = 0; i <= 600; i++) run.AddPaddle(Mathf.Cos(i * 0.05f));
        run.AddBall(60f, Vector2.zero, GhostRun.KeyKind.End);
        run.Finish(60f, score);

        Assert.IsNull(run.Problem());
        int size = run.ToBytes().Length;
        Assert.Less(size, 2500, $"A one-minute ghost is {size} bytes");
    }

    [Test]
    public void Field_MapsAcrossScreenSizes()
    {
        var tall = new Rect(-2.3f, -4.85f, 4.6f, 9.7f);
        var wide = new Rect(-2.8f, -5f, 5.6f, 10f);
        var world = new Vector2(1.15f, -2.425f);

        var normalized = GhostRun.ToField(tall, world);
        Assert.AreEqual(0.5f, normalized.x, 0.0001f);
        Assert.AreEqual(-0.5f, normalized.y, 0.0001f);

        var there = GhostRun.FromField(wide, normalized);
        Assert.AreEqual(1.4f, there.x, 0.0001f);
        Assert.AreEqual(-2.5f, there.y, 0.0001f);
    }

    [Test]
    public void Codes_SkipLookalikeCharacters()
    {
        foreach (char c in "O0I1") Assert.AreEqual(-1, GhostCode.Alphabet.IndexOf(c));
        Assert.AreEqual(32, GhostCode.Alphabet.Length);
        Assert.AreEqual("K7M3QX", GhostCode.Normalize(" k7m-3qx "));
        Assert.IsTrue(GhostCode.IsValid("K7M3QX"));
        Assert.IsFalse(GhostCode.IsValid("K7M3Q0"), "Zero is never used");
        Assert.IsFalse(GhostCode.IsValid("K7M3Q"));
    }
}
