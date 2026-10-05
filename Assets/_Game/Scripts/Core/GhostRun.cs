using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class GhostRun
{
    public const byte Version = 1;
    public const float PaddleStep = 0.1f;
    public const int MaxScore = 300;
    public const float MaxDuration = 62f;
    public const int MaxStep = 3;
    public const int MaxBallKeys = 1500;
    public const int MaxScoreKeys = 400;
    public const int MaxBytes = 12000;
    public const int MaxSkinLength = 40;

    private const float PositionScale = 10000f;
    private const float TimeScale = 100f;
    private const float Slack = 0.5f;

    public enum KeyKind : byte
    {
        Appear,
        Launch,
        Wall,
        Paddle,
        Out,
        End
    }

    public readonly struct BallKey
    {
        public readonly float Time;
        public readonly Vector2 Position;
        public readonly KeyKind Kind;

        public BallKey(float time, Vector2 position, KeyKind kind)
        {
            Time = time;
            Position = position;
            Kind = kind;
        }
    }

    public readonly struct ScoreKey
    {
        public readonly float Time;
        public readonly int Score;

        public ScoreKey(float time, int score)
        {
            Time = time;
            Score = score;
        }
    }

    public int Seed;
    public int Score;
    public float Duration;
    public string BallSkin = string.Empty;
    public string PaddleSkin = string.Empty;

    public readonly List<BallKey> Ball = new List<BallKey>();
    public readonly List<ScoreKey> Scores = new List<ScoreKey>();
    public readonly List<float> Paddle = new List<float>();

    public static Vector2 ToField(Rect field, Vector2 world)
    {
        if (field.width <= 0f || field.height <= 0f) return world;
        return new Vector2((world.x - field.center.x) / (field.width * 0.5f), (world.y - field.center.y) / (field.height * 0.5f));
    }

    public static Vector2 FromField(Rect field, Vector2 normalized)
    {
        if (field.width <= 0f || field.height <= 0f) return normalized;
        return new Vector2(field.center.x + normalized.x * field.width * 0.5f, field.center.y + normalized.y * field.height * 0.5f);
    }

    public void AddBall(float time, Vector2 position, KeyKind kind)
    {
        if (Ball.Count > 0) time = Mathf.Max(time, Ball[Ball.Count - 1].Time);
        Ball.Add(new BallKey(Mathf.Max(0f, time), position, kind));
    }

    public void AddScore(float time, int score)
    {
        if (Scores.Count > 0)
        {
            var last = Scores[Scores.Count - 1];
            if (score <= last.Score) return;
            time = Mathf.Max(time, last.Time);
        }
        Scores.Add(new ScoreKey(Mathf.Max(0f, time), score));
    }

    public void AddPaddle(float x)
    {
        Paddle.Add(Mathf.Clamp(x, -1f, 1f));
    }

    public void Finish(float duration, int score)
    {
        Duration = Mathf.Max(0f, duration);
        Score = Mathf.Max(0, score);
    }

    public int PaddleHits
    {
        get
        {
            int hits = 0;
            foreach (var key in Ball) if (key.Kind == KeyKind.Paddle) hits++;
            return hits;
        }
    }

    public bool BallAt(float time, out Vector2 position)
    {
        position = default;
        if (Ball.Count == 0 || time < Ball[0].Time || time > Duration) return false;

        int i = LastBallAtOrBefore(time);
        var key = Ball[i];
        switch (key.Kind)
        {
            case KeyKind.Out:
            case KeyKind.End:
                return false;
            case KeyKind.Appear:
                position = key.Position;
                return true;
        }

        if (i + 1 >= Ball.Count)
        {
            position = key.Position;
            return true;
        }

        var next = Ball[i + 1];
        if (next.Kind == KeyKind.Appear || next.Kind == KeyKind.Launch)
        {
            position = key.Position;
            return true;
        }

        float span = next.Time - key.Time;
        float t = span > 0.0001f ? (time - key.Time) / span : 1f;
        position = Vector2.Lerp(key.Position, next.Position, Mathf.Clamp01(t));
        return true;
    }

    public int ScoreAt(float time)
    {
        int lo = 0;
        int hi = Scores.Count - 1;
        int found = -1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (Scores[mid].Time <= time)
            {
                found = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }
        return found >= 0 ? Scores[found].Score : 0;
    }

    public float PaddleAt(float time)
    {
        if (Paddle.Count == 0) return 0f;

        float f = Mathf.Max(0f, time) / PaddleStep;
        int i = Mathf.FloorToInt(f);
        if (i >= Paddle.Count - 1) return Paddle[Paddle.Count - 1];
        return Mathf.Lerp(Paddle[i], Paddle[i + 1], f - i);
    }

    private int LastBallAtOrBefore(float time)
    {
        int lo = 0;
        int hi = Ball.Count - 1;
        int found = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            if (Ball[mid].Time <= time)
            {
                found = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }
        return found;
    }

    public string Problem()
    {
        if (Score < 1) return "empty";
        if (Score > MaxScore) return "score";
        if (Duration <= 0f || Duration > MaxDuration) return "duration";
        if (Ball.Count < 1 || Ball.Count > MaxBallKeys) return "ball";
        if (Scores.Count < 1 || Scores.Count > MaxScoreKeys) return "score";
        if (Paddle.Count > Mathf.CeilToInt(Duration / PaddleStep) + 20) return "paddle";
        if (BallSkin.Length > MaxSkinLength || PaddleSkin.Length > MaxSkinLength) return "skin";

        float previous = 0f;
        foreach (var key in Ball)
        {
            if (key.Time < previous || key.Time > Duration + Slack) return "ball";
            previous = key.Time;
        }

        previous = 0f;
        int last = 0;
        foreach (var key in Scores)
        {
            int step = key.Score - last;
            if (key.Time < previous || key.Time > Duration + Slack || step < 1 || step > MaxStep) return "score";
            previous = key.Time;
            last = key.Score;
        }

        if (last != Score) return "score";
        if (PaddleHits < Scores.Count) return "hits";
        return null;
    }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Version);
        writer.Write(Seed);
        writer.Write((ushort)Mathf.Clamp(Score, 0, ushort.MaxValue));
        writer.Write(PackTime(Duration));
        WriteText(writer, BallSkin);
        WriteText(writer, PaddleSkin);

        writer.Write((ushort)Ball.Count);
        foreach (var key in Ball)
        {
            writer.Write(PackTime(key.Time));
            writer.Write(PackPosition(key.Position.x));
            writer.Write(PackPosition(key.Position.y));
            writer.Write((byte)key.Kind);
        }

        writer.Write((ushort)Scores.Count);
        foreach (var key in Scores)
        {
            writer.Write(PackTime(key.Time));
            writer.Write((ushort)Mathf.Clamp(key.Score, 0, ushort.MaxValue));
        }

        writer.Write((ushort)Paddle.Count);
        foreach (float x in Paddle) writer.Write((byte)Mathf.RoundToInt((Mathf.Clamp(x, -1f, 1f) + 1f) * 127.5f));

        writer.Flush();
        return stream.ToArray();
    }

    public static GhostRun FromBytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 12 || bytes.Length > MaxBytes) return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadByte() != Version) return null;

            var run = new GhostRun
            {
                Seed = reader.ReadInt32(),
                Score = reader.ReadUInt16(),
                Duration = UnpackTime(reader.ReadUInt16()),
                BallSkin = ReadText(reader),
                PaddleSkin = ReadText(reader)
            };

            int balls = reader.ReadUInt16();
            for (int i = 0; i < balls; i++)
            {
                float time = UnpackTime(reader.ReadUInt16());
                float x = reader.ReadInt16() / PositionScale;
                float y = reader.ReadInt16() / PositionScale;
                byte kind = reader.ReadByte();
                if (kind > (byte)KeyKind.End) return null;
                run.Ball.Add(new BallKey(time, new Vector2(x, y), (KeyKind)kind));
            }

            int scores = reader.ReadUInt16();
            for (int i = 0; i < scores; i++)
                run.Scores.Add(new ScoreKey(UnpackTime(reader.ReadUInt16()), reader.ReadUInt16()));

            int paddles = reader.ReadUInt16();
            for (int i = 0; i < paddles; i++) run.Paddle.Add(reader.ReadByte() / 127.5f - 1f);

            return stream.Position == stream.Length ? run : null;
        }
        catch (EndOfStreamException)
        {
            return null;
        }
    }

    public string ToBase64()
    {
        return Convert.ToBase64String(ToBytes());
    }

    public static GhostRun FromBase64(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        try
        {
            return FromBytes(Convert.FromBase64String(text));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static ushort PackTime(float seconds)
    {
        return (ushort)Mathf.Clamp(Mathf.RoundToInt(seconds * TimeScale), 0, ushort.MaxValue);
    }

    private static float UnpackTime(ushort value)
    {
        return value / TimeScale;
    }

    private static short PackPosition(float value)
    {
        return (short)Mathf.Clamp(Mathf.RoundToInt(value * PositionScale), short.MinValue, short.MaxValue);
    }

    private static void WriteText(BinaryWriter writer, string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text ?? string.Empty);
        int length = Mathf.Min(bytes.Length, MaxSkinLength);
        writer.Write((byte)length);
        writer.Write(bytes, 0, length);
    }

    private static string ReadText(BinaryReader reader)
    {
        int length = reader.ReadByte();
        if (length > MaxSkinLength) throw new EndOfStreamException();
        return Encoding.ASCII.GetString(reader.ReadBytes(length));
    }
}
