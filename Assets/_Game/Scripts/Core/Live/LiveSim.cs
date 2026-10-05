using System.IO;

public enum LivePhase : byte
{
    Serve,
    Play,
    Point,
    Over
}

public struct LiveState
{
    public int Tick;
    public LivePhase Phase;
    public byte Server;
    public byte HostScore;
    public byte GuestScore;
    public byte LastHitter;
    public ushort Rally;
    public int Timer;
    public uint Rng;
    public Fix BallX;
    public Fix BallY;
    public Fix DirX;
    public Fix DirY;
    public Fix Speed;
    public Fix HostX;
    public Fix GuestX;
    public Fix HostVel;
    public Fix GuestVel;

    public bool BallVisible => Phase == LivePhase.Serve || Phase == LivePhase.Play;

    public uint Checksum()
    {
        unchecked
        {
            uint hash = 2166136261u;
            void Mix(int value)
            {
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(value >> (i * 8));
                    hash *= 16777619u;
                }
            }

            Mix(Tick);
            Mix((int)Phase | (Server << 8) | (HostScore << 16) | (GuestScore << 24));
            Mix(LastHitter | (Rally << 8));
            Mix(Timer);
            Mix((int)Rng);
            Mix(BallX.Raw);
            Mix(BallY.Raw);
            Mix(DirX.Raw);
            Mix(DirY.Raw);
            Mix(Speed.Raw);
            Mix(HostX.Raw);
            Mix(GuestX.Raw);
            Mix(HostVel.Raw);
            Mix(GuestVel.Raw);
            return hash;
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Tick);
        writer.Write((byte)Phase);
        writer.Write(Server);
        writer.Write(HostScore);
        writer.Write(GuestScore);
        writer.Write(LastHitter);
        writer.Write(Rally);
        writer.Write(Timer);
        writer.Write(Rng);
        foreach (var value in new[] { BallX, BallY, DirX, DirY, Speed, HostX, GuestX, HostVel, GuestVel }) writer.Write(value.Raw);
    }

    public static LiveState Read(BinaryReader reader)
    {
        var state = new LiveState
        {
            Tick = reader.ReadInt32(),
            Phase = (LivePhase)reader.ReadByte(),
            Server = reader.ReadByte(),
            HostScore = reader.ReadByte(),
            GuestScore = reader.ReadByte(),
            LastHitter = reader.ReadByte(),
            Rally = reader.ReadUInt16(),
            Timer = reader.ReadInt32(),
            Rng = reader.ReadUInt32()
        };
        state.BallX = Fix.FromRaw(reader.ReadInt32());
        state.BallY = Fix.FromRaw(reader.ReadInt32());
        state.DirX = Fix.FromRaw(reader.ReadInt32());
        state.DirY = Fix.FromRaw(reader.ReadInt32());
        state.Speed = Fix.FromRaw(reader.ReadInt32());
        state.HostX = Fix.FromRaw(reader.ReadInt32());
        state.GuestX = Fix.FromRaw(reader.ReadInt32());
        state.HostVel = Fix.FromRaw(reader.ReadInt32());
        state.GuestVel = Fix.FromRaw(reader.ReadInt32());
        return state;
    }
}

public struct LiveEvents
{
    public bool HostHit;
    public bool GuestHit;
    public bool Wall;
    public bool Served;
    public byte Missed;
}

public static class LiveInput
{
    public const ushort Idle = 128;
    private const ushort TouchBit = 0x100;

    public static ushort Pack(byte x, bool touch)
    {
        return (ushort)(x | (touch ? TouchBit : 0));
    }

    public static byte X(ushort input)
    {
        return (byte)(input & 0xFF);
    }

    public static bool Touch(ushort input)
    {
        return (input & TouchBit) != 0;
    }

    public static byte FromCourtX(float x)
    {
        float range = LiveSim.PaddleRange.ToFloat();
        float t = range > 0f ? (x / range + 1f) * 0.5f : 0.5f;
        int value = (int)System.Math.Round(t * 255f);
        return (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
    }
}

public static class LiveSim
{
    public const int TickRate = 60;
    public const int Substeps = 4;
    public const int ServeMinTicks = 24;
    public const int AutoServeTicks = 240;
    public const int PointPauseTicks = 50;

    public static readonly Fix HalfWidth = Fix.Ratio(45, 16);
    public static readonly Fix HalfHeight = Fix.FromInt(5);
    public static readonly Fix PaddleY = Fix.FromInt(4);
    public static readonly Fix PaddleHalf = Fix.Ratio(56, 100);
    public static readonly Fix CapRadius = Fix.Ratio(17, 100);
    public static readonly Fix BallRadius = Fix.Ratio(2025, 10000);
    public static readonly Fix PaddleRange = HalfWidth - PaddleHalf;
    public static readonly Fix MoveStep = Fix.Ratio(12, TickRate);
    public static readonly Fix LaunchSpeed = Fix.FromInt(6);
    public static readonly Fix MaxSpeed = Fix.FromInt(16);
    public static readonly Fix SpeedGain = Fix.Ratio(1015, 1000);
    public static readonly Fix MinVertical = Fix.Ratio(2, 5);
    public static readonly Fix Jitter = Fix.Ratio(5236, 100000);
    public static readonly Fix MaxTilt = Fix.Ratio(38397, 100000);
    public static readonly Fix SpeedForMaxTilt = Fix.FromInt(10);
    public static readonly Fix StepTime = Fix.Ratio(1, TickRate * Substeps);
    public static readonly Fix Separation = Fix.Ratio(1, 100);

    private static readonly Fix SegHalf = PaddleHalf - CapRadius;
    private static readonly Fix Reach = CapRadius + BallRadius;
    private static readonly Fix ServeLow = Fix.Ratio(3, 5);
    private static readonly Fix Tiny = Fix.FromRaw(16);

    public static LiveState Create(int seed, bool hostServes)
    {
        uint rng = unchecked((uint)seed);
        if (rng == 0u) rng = 0x9E3779B9u;

        return new LiveState
        {
            Phase = LivePhase.Serve,
            Server = hostServes ? (byte)0 : (byte)1,
            LastHitter = hostServes ? (byte)0 : (byte)1,
            Rng = rng,
            Speed = LaunchSpeed,
            DirY = Fix.One
        };
    }

    public static Fix TargetX(ushort input)
    {
        int b = LiveInput.X(input);
        return Fix.FromRaw((int)((long)PaddleRange.Raw * (2 * b - 255) / 255));
    }

    public static void Step(ref LiveState s, ushort hostInput, ushort guestInput, int pointsToWin, ref LiveEvents events)
    {
        s.Tick++;
        MovePaddle(ref s.HostX, ref s.HostVel, TargetX(hostInput));
        MovePaddle(ref s.GuestX, ref s.GuestVel, TargetX(guestInput));

        switch (s.Phase)
        {
            case LivePhase.Serve:
                s.Timer++;
                s.BallX = Fix.Zero;
                s.BallY = Fix.Zero;
                bool tap = LiveInput.Touch(s.Server == 0 ? hostInput : guestInput);
                if ((s.Timer >= ServeMinTicks && tap) || s.Timer >= AutoServeTicks) Launch(ref s, ref events);
                break;

            case LivePhase.Play:
                for (int i = 0; i < Substeps && s.Phase == LivePhase.Play; i++) Advance(ref s, pointsToWin, ref events);
                break;

            case LivePhase.Point:
                s.Timer--;
                if (s.Timer <= 0)
                {
                    s.Phase = LivePhase.Serve;
                    s.Timer = 0;
                    s.BallX = Fix.Zero;
                    s.BallY = Fix.Zero;
                }
                break;
        }
    }

    private static void MovePaddle(ref Fix x, ref Fix velocity, Fix target)
    {
        Fix delta = Fix.Clamp(target - x, -MoveStep, MoveStep);
        Fix next = Fix.Clamp(x + delta, -PaddleRange, PaddleRange);
        velocity = (next - x) * TickRate;
        x = next;
    }

    private static void Launch(ref LiveState s, ref LiveEvents events)
    {
        Fix x = (NextRandom(ref s.Rng) & 1u) == 0u ? -Fix.One : Fix.One;
        Fix y = RandomRange(ref s.Rng, ServeLow, Fix.One);
        if (s.Server == 1) y = -y;

        SafeDirection(ref x, ref y);
        s.DirX = x;
        s.DirY = y;
        s.Speed = LaunchSpeed;
        s.Rally = 0;
        s.LastHitter = s.Server;
        s.Phase = LivePhase.Play;
        s.Timer = 0;
        events.Served = true;
    }

    private static void Advance(ref LiveState s, int pointsToWin, ref LiveEvents events)
    {
        Fix travel = s.Speed * StepTime;
        s.BallX += s.DirX * travel;
        s.BallY += s.DirY * travel;

        Fix limit = HalfWidth - BallRadius;
        if (s.BallX > limit || s.BallX < -limit)
        {
            bool right = s.BallX > limit;
            Fix edge = right ? limit : -limit;
            s.BallX = edge - (s.BallX - edge);
            s.DirX = right ? -Fix.Abs(s.DirX) : Fix.Abs(s.DirX);

            Fix angle = RandomRange(ref s.Rng, -Jitter, Jitter);
            Fix x = s.DirX;
            Fix y = s.DirY;
            Rotate(ref x, ref y, angle);
            SafeDirection(ref x, ref y);
            s.DirX = x;
            s.DirY = y;
            events.Wall = true;
        }

        if (s.DirY < Fix.Zero) Collide(ref s, 0, ref events);
        else Collide(ref s, 1, ref events);

        Fix goal = HalfHeight + BallRadius;
        if (s.BallY < -goal) Score(ref s, false, pointsToWin, ref events);
        else if (s.BallY > goal) Score(ref s, true, pointsToWin, ref events);
    }

    private static void Collide(ref LiveState s, byte side, ref LiveEvents events)
    {
        Fix inward = side == 0 ? Fix.One : -Fix.One;
        Fix py = side == 0 ? -PaddleY : PaddleY;
        Fix px = side == 0 ? s.HostX : s.GuestX;
        Fix pv = side == 0 ? s.HostVel : s.GuestVel;

        Fix dy = s.BallY - py;
        if (dy * inward < -(CapRadius / 2)) return;

        Fix cx = Fix.Clamp(s.BallX, px - SegHalf, px + SegHalf);
        Fix dx = s.BallX - cx;
        Fix distSq = dx * dx + dy * dy;
        if (distSq >= Reach * Reach) return;

        Fix dist = Fix.Sqrt(distSq);
        Fix nx = Fix.Zero;
        Fix ny = inward;
        if (dist > Tiny)
        {
            nx = dx / dist;
            ny = dy / dist;
        }

        s.BallX = cx + nx * (Reach + Separation);
        s.BallY = py + ny * (Reach + Separation);

        Fix tiltSign = side == 0 ? Fix.One : -Fix.One;
        Fix tilt = -Fix.Clamp(pv / SpeedForMaxTilt, -Fix.One, Fix.One) * MaxTilt * tiltSign;
        Rotate(ref nx, ref ny, tilt);

        Fix x = s.DirX;
        Fix y = s.DirY;
        Fix dot = x * nx + y * ny;
        x -= nx * dot * 2;
        y -= ny * dot * 2;
        if (y * inward < Fix.Zero) y = -y;
        SafeDirection(ref x, ref y);

        s.DirX = x;
        s.DirY = y;
        s.Speed = Fix.Min(s.Speed * SpeedGain, MaxSpeed);
        s.Rally++;
        s.LastHitter = side;
        if (side == 0) events.HostHit = true;
        else events.GuestHit = true;
    }

    private static void Score(ref LiveState s, bool hostScored, int pointsToWin, ref LiveEvents events)
    {
        if (hostScored) s.HostScore++;
        else s.GuestScore++;

        events.Missed = hostScored ? (byte)2 : (byte)1;
        s.Server = hostScored ? (byte)1 : (byte)0;
        s.Rally = 0;

        if (s.HostScore >= pointsToWin || s.GuestScore >= pointsToWin)
        {
            s.Phase = LivePhase.Over;
            return;
        }

        s.Phase = LivePhase.Point;
        s.Timer = PointPauseTicks;
    }

    private static void SafeDirection(ref Fix x, ref Fix y)
    {
        Normalize(ref x, ref y);
        if (Fix.Abs(y) >= MinVertical) return;

        y = y < Fix.Zero ? -MinVertical : MinVertical;
        Fix side = Fix.Sqrt(Fix.One - y * y);
        x = x < Fix.Zero ? -side : side;
    }

    private static void Normalize(ref Fix x, ref Fix y)
    {
        Fix length = Fix.Sqrt(x * x + y * y);
        if (length <= Tiny)
        {
            x = Fix.Zero;
            y = Fix.One;
            return;
        }

        x /= length;
        y /= length;
    }

    private static void Rotate(ref Fix x, ref Fix y, Fix angle)
    {
        Fix c = Fix.Cos(angle);
        Fix sn = Fix.Sin(angle);
        Fix rx = x * c - y * sn;
        Fix ry = x * sn + y * c;
        x = rx;
        y = ry;
    }

    public static uint NextRandom(ref uint state)
    {
        unchecked
        {
            state += 0x6D2B79F5u;
            uint z = state;
            z = (z ^ (z >> 15)) * (z | 1u);
            z ^= z + (z ^ (z >> 7)) * (z | 61u);
            return z ^ (z >> 14);
        }
    }

    private static Fix RandomRange(ref uint state, Fix min, Fix max)
    {
        uint r = NextRandom(ref state) & 0xFFFFu;
        return min + Fix.FromRaw((int)(((long)(max - min).Raw * r) >> 16));
    }
}
