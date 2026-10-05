using System;
using System.IO;

public enum MatchMessageType : byte
{
    Start = 1,
    Handoff = 2,
    Miss = 3,
    Score = 4,
    Reaction = 5,
    Rematch = 6,
    RushStart = 7,
    RushScore = 8,
    Attack = 9,
    Shield = 10,
    RushFinal = 11
}

public struct MatchMessage
{
    public const byte Version = 1;

    public MatchMessageType Type;
    public ushort Seq;

    public int Seed;
    public bool HostServes;

    public float X;
    public float DirX;
    public float DirY;
    public float Speed;
    public ushort Rally;
    public string Look;

    public byte HostScore;
    public byte GuestScore;
    public byte Lives;
    public ushort Passes;
    public bool Ended;
    public bool HostWon;

    public byte Reaction;

    public byte Slot;
    public byte Target;
    public byte Kind;
    public int Value;
    public string Roster;

    public static MatchMessage Start(int seed, bool hostServes)
    {
        return new MatchMessage { Type = MatchMessageType.Start, Seed = seed, HostServes = hostServes };
    }

    public static MatchMessage Handoff(ushort seq, float x, float dirX, float dirY, float speed, ushort rally, string look)
    {
        return new MatchMessage { Type = MatchMessageType.Handoff, Seq = seq, X = x, DirX = dirX, DirY = dirY, Speed = speed, Rally = rally, Look = look };
    }

    public static MatchMessage Miss(ushort seq)
    {
        return new MatchMessage { Type = MatchMessageType.Miss, Seq = seq };
    }

    public static MatchMessage ReactionOf(byte reaction)
    {
        return new MatchMessage { Type = MatchMessageType.Reaction, Reaction = reaction };
    }

    public static MatchMessage RematchRequest()
    {
        return new MatchMessage { Type = MatchMessageType.Rematch };
    }

    public static MatchMessage RematchFrom(byte slot)
    {
        return new MatchMessage { Type = MatchMessageType.Rematch, Slot = slot };
    }

    public static MatchMessage RushStart(int seed, string roster)
    {
        return new MatchMessage { Type = MatchMessageType.RushStart, Seed = seed, Roster = roster };
    }

    public static MatchMessage RushScore(byte slot, int score, byte secondsLeft)
    {
        return new MatchMessage { Type = MatchMessageType.RushScore, Slot = slot, Value = score, Kind = secondsLeft };
    }

    public static MatchMessage AttackOn(ushort seq, byte from, byte to, byte kind)
    {
        return new MatchMessage { Type = MatchMessageType.Attack, Seq = seq, Slot = from, Target = to, Kind = kind };
    }

    public static MatchMessage ShieldFrom(ushort seq, byte slot, byte attacker, byte kind)
    {
        return new MatchMessage { Type = MatchMessageType.Shield, Seq = seq, Slot = slot, Target = attacker, Kind = kind };
    }

    public static MatchMessage RushFinal(byte slot, int score)
    {
        return new MatchMessage { Type = MatchMessageType.RushFinal, Slot = slot, Value = score };
    }

    public static MatchMessage ReactionFrom(byte slot, byte reaction)
    {
        return new MatchMessage { Type = MatchMessageType.Reaction, Slot = slot, Reaction = reaction };
    }

    public static MatchMessage Score(OnlineMatchRules rules)
    {
        return new MatchMessage
        {
            Type = MatchMessageType.Score,
            HostScore = (byte)Math.Min(rules.HostScore, byte.MaxValue),
            GuestScore = (byte)Math.Min(rules.GuestScore, byte.MaxValue),
            Lives = (byte)Math.Max(0, rules.Lives),
            Passes = (ushort)Math.Min(rules.Passes, ushort.MaxValue),
            HostServes = rules.HostServes,
            Ended = rules.Ended,
            HostWon = rules.HostWon
        };
    }

    public byte[] ToBytes()
    {
        using var stream = new MemoryStream(48);
        using var writer = new BinaryWriter(stream);
        writer.Write(Version);
        writer.Write((byte)Type);
        writer.Write(Seq);

        switch (Type)
        {
            case MatchMessageType.Start:
                writer.Write(Seed);
                writer.Write(HostServes);
                break;
            case MatchMessageType.Handoff:
                writer.Write(X);
                writer.Write(DirX);
                writer.Write(DirY);
                writer.Write(Speed);
                writer.Write(Rally);
                writer.Write(Look ?? string.Empty);
                break;
            case MatchMessageType.Score:
                writer.Write(HostScore);
                writer.Write(GuestScore);
                writer.Write(Lives);
                writer.Write(Passes);
                writer.Write(HostServes);
                writer.Write(Ended);
                writer.Write(HostWon);
                break;
            case MatchMessageType.Reaction:
                writer.Write(Reaction);
                writer.Write(Slot);
                break;
            case MatchMessageType.Rematch:
                writer.Write(Slot);
                break;
            case MatchMessageType.RushStart:
                writer.Write(Seed);
                writer.Write(Roster ?? string.Empty);
                break;
            case MatchMessageType.RushScore:
            case MatchMessageType.RushFinal:
                writer.Write(Slot);
                writer.Write(Value);
                writer.Write(Kind);
                break;
            case MatchMessageType.Attack:
            case MatchMessageType.Shield:
                writer.Write(Slot);
                writer.Write(Target);
                writer.Write(Kind);
                break;
        }

        writer.Flush();
        return stream.ToArray();
    }

    public static bool TryParse(byte[] data, out MatchMessage message)
    {
        message = default;
        if (data == null || data.Length < 4) return false;

        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != Version) return false;

            message.Type = (MatchMessageType)reader.ReadByte();
            message.Seq = reader.ReadUInt16();

            switch (message.Type)
            {
                case MatchMessageType.Start:
                    message.Seed = reader.ReadInt32();
                    message.HostServes = reader.ReadBoolean();
                    break;
                case MatchMessageType.Handoff:
                    message.X = reader.ReadSingle();
                    message.DirX = reader.ReadSingle();
                    message.DirY = reader.ReadSingle();
                    message.Speed = reader.ReadSingle();
                    message.Rally = reader.ReadUInt16();
                    message.Look = reader.ReadString();
                    break;
                case MatchMessageType.Score:
                    message.HostScore = reader.ReadByte();
                    message.GuestScore = reader.ReadByte();
                    message.Lives = reader.ReadByte();
                    message.Passes = reader.ReadUInt16();
                    message.HostServes = reader.ReadBoolean();
                    message.Ended = reader.ReadBoolean();
                    message.HostWon = reader.ReadBoolean();
                    break;
                case MatchMessageType.Reaction:
                    message.Reaction = reader.ReadByte();
                    message.Slot = reader.ReadByte();
                    break;
                case MatchMessageType.Rematch:
                    message.Slot = reader.ReadByte();
                    break;
                case MatchMessageType.RushStart:
                    message.Seed = reader.ReadInt32();
                    message.Roster = reader.ReadString();
                    break;
                case MatchMessageType.RushScore:
                case MatchMessageType.RushFinal:
                    message.Slot = reader.ReadByte();
                    message.Value = reader.ReadInt32();
                    message.Kind = reader.ReadByte();
                    break;
                case MatchMessageType.Attack:
                case MatchMessageType.Shield:
                    message.Slot = reader.ReadByte();
                    message.Target = reader.ReadByte();
                    message.Kind = reader.ReadByte();
                    break;
                case MatchMessageType.Miss:
                    break;
                default:
                    return false;
            }

            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }
}
