using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class LiveSimTests
{
    private const int Points = 5;

    private static ushort Follow(in LiveState s, bool host, int noise, bool touch)
    {
        return LiveInput.Pack(LiveInput.FromCourtX(s.BallX.ToFloat() + noise * 0.01f), touch);
    }

    private static ushort FollowExact(in LiveState s, int noiseRaw, bool touch)
    {
        long range = LiveSim.PaddleRange.Raw;
        long value = ((long)s.BallX.Raw + noiseRaw + range) * 255 / (2 * range);
        return LiveInput.Pack((byte)(value < 0 ? 0 : value > 255 ? 255 : value), touch);
    }

    [Test]
    public void FixedPoint_MathIsAccurate()
    {
        Assert.AreEqual(6f, (Fix.FromInt(2) * Fix.FromInt(3)).ToFloat(), 0.0001f);
        Assert.AreEqual(0.75f, (Fix.FromInt(3) / Fix.FromInt(4)).ToFloat(), 0.0001f);
        Assert.AreEqual(1.41421f, Fix.Sqrt(Fix.FromInt(2)).ToFloat(), 0.0005f);
        Assert.AreEqual(0f, Fix.Sqrt(Fix.Zero).ToFloat(), 0.0001f);
        Assert.AreEqual((float)Math.Sin(0.384), Fix.Sin(Fix.Ratio(384, 1000)).ToFloat(), 0.0005f);
        Assert.AreEqual((float)Math.Cos(0.384), Fix.Cos(Fix.Ratio(384, 1000)).ToFloat(), 0.0005f);
        Assert.AreEqual(2.8125f, LiveSim.HalfWidth.ToFloat(), 0.0001f);
    }

    [Test]
    public void SameInputs_GiveTheSameMatch()
    {
        var rng = new Random(11);
        for (int match = 0; match < 2000; match++)
        {
            int seed = rng.Next();
            var script = new ushort[600, 2];
            for (int t = 0; t < 600; t++)
            {
                script[t, 0] = LiveInput.Pack((byte)rng.Next(256), rng.Next(4) == 0);
                script[t, 1] = LiveInput.Pack((byte)rng.Next(256), rng.Next(4) == 0);
            }

            Assert.AreEqual(Play(seed, script), Play(seed, script), $"Match {match} diverged");
        }
    }

    private static uint Play(int seed, ushort[,] script)
    {
        var state = LiveSim.Create(seed, (seed & 1) == 0);
        uint mix = 0;
        for (int t = 0; t < script.GetLength(0); t++)
        {
            var events = new LiveEvents();
            LiveSim.Step(ref state, script[t, 0], script[t, 1], Points, ref events);
            mix = unchecked(mix * 31u + state.Checksum());
        }
        return mix;
    }

    [Test]
    public void GoldenMatch_IsBitExactOnEveryMachine()
    {
        var state = LiveSim.Create(20261005, true);
        int noise = 0;
        for (int t = 0; t < 3600; t++)
        {
            noise = (noise * 1103515245 + 12345) & 0x7FFF;
            var events = new LiveEvents();
            ushort host = FollowExact(state, (noise % 60 - 30) * 655, true);
            ushort guest = FollowExact(state, (noise % 150 - 75) * 1310, t % 3 == 0);
            LiveSim.Step(ref state, host, guest, 99, ref events);
        }

        UnityEngine.Debug.Log($"Golden live checksum {state.Checksum()} score {state.HostScore}-{state.GuestScore}");
        Assert.GreaterOrEqual(state.HostScore + state.GuestScore, 2, "The golden match includes points, serves and pauses");
        Assert.AreEqual(GoldenChecksum, state.Checksum(), "The fixed-point sim must give the same bits on Windows, Linux and phones");
    }

    private const uint GoldenChecksum = 1934392956u;

    [Test]
    public void Rally_FollowingPaddlesKeepTheBallAliveAndSpeedUp()
    {
        var state = LiveSim.Create(7, true);
        int hits = 0;
        for (int t = 0; t < 3600; t++)
        {
            var events = new LiveEvents();
            LiveSim.Step(ref state, Follow(state, true, 0, true), Follow(state, false, 0, true), Points, ref events);
            if (events.HostHit || events.GuestHit) hits++;
            Assert.AreEqual(0, events.Missed, $"Perfect followers never miss (tick {t})");
        }

        Assert.Greater(hits, 15, "About one return every two seconds");
        Assert.Greater(state.Speed.ToFloat(), 6.5f, "Every hit speeds the ball up");
        Assert.LessOrEqual(state.Speed.ToFloat(), 16.0001f);
        Assert.GreaterOrEqual(Math.Abs(state.DirY.ToFloat()), 0.399f, "The ball never goes flat");
        Assert.LessOrEqual(Math.Abs(state.BallX.ToFloat()), 2.8125f);
    }

    [Test]
    public void Serve_WaitsForTheServerThenPointsGoToTheOtherSide()
    {
        var state = LiveSim.Create(3, true);
        ushort still = LiveInput.Pack(0, false);
        var events = new LiveEvents();
        for (int t = 0; t < LiveSim.ServeMinTicks + 5; t++) LiveSim.Step(ref state, still, LiveInput.Pack(0, true), Points, ref events);
        Assert.AreEqual(LivePhase.Serve, state.Phase, "Only the server's tap serves");

        LiveSim.Step(ref state, LiveInput.Pack(0, true), still, Points, ref events);
        Assert.AreEqual(LivePhase.Play, state.Phase);
        Assert.Greater(state.DirY.ToFloat(), 0f, "The host serves up, toward the guest");

        byte missed = 0;
        for (int t = 0; t < 600 && missed == 0; t++)
        {
            var step = new LiveEvents();
            LiveSim.Step(ref state, LiveInput.Pack(255, false), LiveInput.Pack(255, false), Points, ref step);
            missed = step.Missed;
        }

        Assert.AreNotEqual(0, missed, "Paddles in the corner let the ball through");
        Assert.AreEqual(1, state.HostScore + state.GuestScore);
        Assert.AreEqual(LivePhase.Point, state.Phase);
        Assert.AreEqual(missed == 1 ? 0 : 1, state.Server, "Whoever missed serves next");
    }

    [Test]
    public void Match_EndsAtTheTarget()
    {
        var state = LiveSim.Create(5, false);
        for (int t = 0; t < 20000 && state.Phase != LivePhase.Over; t++)
        {
            var events = new LiveEvents();
            LiveSim.Step(ref state, LiveInput.Pack(0, true), LiveInput.Pack(0, true), 3, ref events);
        }

        Assert.AreEqual(LivePhase.Over, state.Phase);
        Assert.AreEqual(3, Math.Max(state.HostScore, state.GuestScore));
    }

    private sealed class Wire
    {
        private readonly List<(double at, byte[] data)> queue = new List<(double, byte[])>();
        private readonly Random random;
        private readonly double latency;
        private readonly double jitter;
        private readonly double loss;

        public Wire(int seed, double latency, double jitter, double loss)
        {
            random = new Random(seed);
            this.latency = latency;
            this.jitter = jitter;
            this.loss = loss;
        }

        public void Send(double now, byte[] data)
        {
            if (random.NextDouble() < loss) return;
            queue.Add((now + latency + random.NextDouble() * jitter, data));
        }

        public void Deliver(double now, LiveSession to)
        {
            queue.Sort((a, b) => a.at.CompareTo(b.at));
            while (queue.Count > 0 && queue[0].at <= now)
            {
                to.Receive(queue[0].data);
                queue.RemoveAt(0);
            }
        }
    }

    private static (LiveSession host, LiveSession guest, List<ushort> hostInputs, List<ushort> guestInputs) Run(double latency, double jitter, double loss, int frames, int hostSeed = 99, int guestSeed = 99)
    {
        double now = 0;
        var toGuest = new Wire(1, latency, jitter, loss);
        var toHost = new Wire(2, latency, jitter, loss);
        LiveSession host = null;
        LiveSession guest = null;
        host = new LiveSession(hostSeed, true, true, Points, data => toGuest.Send(now, data));
        guest = new LiveSession(guestSeed, true, false, Points, data => toHost.Send(now, data));

        var hostInputs = new List<ushort>();
        var guestInputs = new List<ushort>();
        var noise = new Random(5);
        for (int frame = 0; frame < frames; frame++)
        {
            now = frame / 60.0;
            toHost.Deliver(now, host);
            toGuest.Deliver(now, guest);

            int hostTick = host.Tick;
            ushort hi = Follow(host.State, true, noise.Next(-40, 40), true);
            host.Update(1f / 60f, hi);
            for (int t = hostTick; t < host.Tick; t++) hostInputs.Add(hi);

            int guestTick = guest.Tick;
            ushort gi = Follow(guest.State, false, noise.Next(-40, 40), true);
            guest.Update(1f / 60f, gi);
            for (int t = guestTick; t < guest.Tick; t++) guestInputs.Add(gi);
        }

        return (host, guest, hostInputs, guestInputs);
    }

    [Test]
    public void Rollback_BothPhonesAgreeUnderLagAndLoss()
    {
        var (host, guest, hostInputs, guestInputs) = Run(0.15, 0.03, 0.10, 3600);

        int confirmed = Math.Min(host.RemoteConfirmed, guest.RemoteConfirmed);
        Assert.Greater(confirmed, 2500, $"Both kept playing (host {host.Tick}, guest {guest.Tick})");
        Assert.Greater(host.Rollbacks + guest.Rollbacks, 0, "Late inputs caused rollbacks");
        Assert.IsFalse(host.Desynced || guest.Desynced);

        int at = confirmed - 5;
        Assert.IsTrue(host.TryGetState(at, out var hostState));
        Assert.IsTrue(guest.TryGetState(at, out var guestState));
        Assert.AreEqual(hostState.Checksum(), guestState.Checksum(), "Same confirmed state on both phones");

        var reference = LiveSim.Create(99, true);
        for (int t = 0; t < at; t++)
        {
            ushort h = t < LiveSession.InputDelay ? LiveInput.Idle : hostInputs[t - LiveSession.InputDelay];
            ushort g = t < LiveSession.InputDelay ? LiveInput.Idle : guestInputs[t - LiveSession.InputDelay];
            var events = new LiveEvents();
            LiveSim.Step(ref reference, h, g, Points, ref events);
        }
        Assert.AreEqual(reference.Checksum(), hostState.Checksum(), "Rollback gives exactly the lockstep result");
    }

    [Test]
    public void Rollback_StaysSmoothAt150ms()
    {
        var (host, guest, _, _) = Run(0.075, 0.01, 0.0, 1800);
        Assert.Greater(host.Tick, 1700, "Host barely waited");
        Assert.Greater(guest.Tick, 1700, "Guest barely waited");
        Assert.Less(Math.Abs(host.Tick - guest.Tick), 6, "Clocks stay in step");
    }

    [Test]
    public void Packets_FromAnotherMatchAreIgnored()
    {
        var old = new List<byte[]>();
        var previous = new LiveSession(4, true, false, Points, old.Add);
        for (int i = 0; i < 20; i++) previous.Update(1f / 60f, LiveInput.Pack(255, true));

        var fresh = new LiveSession(5, true, true, Points, null);
        foreach (var packet in old) fresh.Receive(packet);
        fresh.Update(1f / 60f, LiveInput.Idle);
        Assert.AreEqual(-1, fresh.RemoteConfirmed, "A rematch never mixes in the last match's inputs");
    }

    [Test]
    public void Desync_IsCaughtAndTheHostRepairsIt()
    {
        var host = default(LiveSession);
        var guest = default(LiveSession);
        int detected = -1;
        double now = 0;
        var toGuest = new Wire(3, 0.03, 0, 0);
        var toHost = new Wire(4, 0.03, 0, 0);
        host = new LiveSession(1, true, true, Points, data => toGuest.Send(now, data));
        guest = new LiveSession(1, false, false, Points, data => toHost.Send(now, data));
        guest.DesyncDetected += at => detected = at;

        for (int frame = 0; frame < 600; frame++)
        {
            now = frame / 60.0;
            toHost.Deliver(now, host);
            toGuest.Deliver(now, guest);
            host.Update(1f / 60f, Follow(host.State, true, 0, true));
            guest.Update(1f / 60f, Follow(guest.State, false, 0, true));
        }

        Assert.Greater(detected, 0, "Different starting states are noticed");
        int at = Math.Min(host.RemoteConfirmed, guest.RemoteConfirmed) - 3;
        Assert.IsTrue(host.TryGetState(at, out var a));
        Assert.IsTrue(guest.TryGetState(at, out var b));
        Assert.AreEqual(a.Checksum(), b.Checksum(), "After the host's snapshot both phones agree again");
    }
}
