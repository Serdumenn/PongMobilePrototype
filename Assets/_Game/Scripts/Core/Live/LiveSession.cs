using System;
using System.Collections.Generic;
using System.IO;

public sealed class LiveSession
{
    public const int InputDelay = 2;
    public const int MaxRollback = 15;
    public const int ChecksumEvery = 30;

    private const int History = 128;
    private const int MaxInputsPerPacket = 32;
    private const int MaxCatchUp = 4;
    private const byte InputPacket = 1;
    private const byte SnapshotPacket = 2;

    public readonly struct Confirmed
    {
        public readonly int Tick;
        public readonly byte Missed;
        public readonly bool Over;

        public Confirmed(int tick, byte missed, bool over)
        {
            Tick = tick;
            Missed = missed;
            Over = over;
        }
    }

    private readonly LiveState[] states = new LiveState[History];
    private readonly ushort[] localInputs = new ushort[History];
    private readonly ushort[] remoteInputs = new ushort[History];
    private readonly int[] remoteKnown = new int[History];
    private readonly ushort[] usedRemote = new ushort[History];
    private readonly Dictionary<int, uint> remoteChecksums = new Dictionary<int, uint>();
    private readonly Dictionary<int, uint> localChecksums = new Dictionary<int, uint>();
    private readonly Queue<byte[]> inbox = new Queue<byte[]>();
    private readonly Action<byte[]> send;
    private readonly int pointsToWin;
    private readonly int matchId;

    private int tick;
    private int remoteConfirmed = -1;
    private int remoteAck = -1;
    private int remoteAhead;
    private int confirmedCursor = -1;
    private int lastChecksumTick = -1;
    private int rollbackFrom = int.MaxValue;
    private float accumulator;
    private int skipCredit;
    private bool snapshotPending;

    public LiveSession(int seed, bool hostServes, bool isHost, int pointsToWin, Action<byte[]> send)
    {
        IsHost = isHost;
        matchId = seed;
        this.pointsToWin = Math.Max(1, pointsToWin);
        this.send = send;

        for (int i = 0; i < History; i++)
        {
            remoteKnown[i] = -1;
            localInputs[i] = LiveInput.Idle;
        }
        states[0] = LiveSim.Create(seed, hostServes);
    }

    public bool IsHost { get; }
    public int Tick => tick;
    public int RemoteConfirmed => remoteConfirmed;
    public LiveState State => states[tick % History];
    public int Rollbacks { get; private set; }
    public int RolledBackTicks { get; private set; }
    public int Stalls { get; private set; }
    public bool Desynced { get; private set; }
    public int DesyncTick { get; private set; } = -1;
    public bool Corrected { get; private set; }
    public LiveState BeforeCorrection { get; private set; }

    public event Action<LiveEvents> Predicted;
    public event Action<Confirmed> ConfirmedEvent;
    public event Action<int> DesyncDetected;

    public bool TryGetState(int atTick, out LiveState state)
    {
        state = default;
        if (atTick < 0 || atTick > tick || tick - atTick >= History) return false;
        state = states[atTick % History];
        return true;
    }

    public void Receive(byte[] packet)
    {
        if (packet != null && packet.Length > 0) inbox.Enqueue(packet);
    }

    public void Update(float deltaSeconds, ushort localInput)
    {
        Corrected = false;
        ProcessInbox();

        accumulator += Math.Max(0f, deltaSeconds);
        float step = 1f / LiveSim.TickRate;
        int due = 0;
        while (accumulator >= step && due < MaxCatchUp)
        {
            accumulator -= step;
            due++;
        }
        if (accumulator > step * MaxCatchUp) accumulator = step * MaxCatchUp;

        int advantage = (tick - remoteConfirmed - 1) - remoteAhead;
        if (advantage >= 2 && due > 0)
        {
            skipCredit += advantage;
            if (skipCredit >= 12)
            {
                skipCredit = 0;
                due--;
            }
        }

        for (int i = 0; i < due; i++)
            if (!AdvanceTick(localInput)) break;

        EmitConfirmed();
        SendInputs();
    }

    public bool AdvanceTick(ushort localInput)
    {
        if (tick - remoteConfirmed > MaxRollback)
        {
            Stalls++;
            return false;
        }

        int scheduled = tick + InputDelay;
        localInputs[scheduled % History] = localInput;
        Simulate(tick, true);
        tick++;
        return true;
    }

    public void Pump()
    {
        Corrected = false;
        ProcessInbox();
        EmitConfirmed();
        SendInputs();
    }

    private ushort LocalInputAt(int t)
    {
        return t < InputDelay ? LiveInput.Idle : localInputs[t % History];
    }

    private ushort RemoteInputAt(int t)
    {
        if (remoteKnown[t % History] == t) return remoteInputs[t % History];
        if (remoteConfirmed >= 0) return remoteInputs[remoteConfirmed % History];
        return LiveInput.Idle;
    }

    private void Simulate(int t, bool fresh)
    {
        ushort local = LocalInputAt(t);
        ushort remote = RemoteInputAt(t);
        usedRemote[t % History] = remote;

        var state = states[t % History];
        var events = new LiveEvents();
        LiveSim.Step(ref state, IsHost ? local : remote, IsHost ? remote : local, pointsToWin, ref events);
        states[(t + 1) % History] = state;

        if (fresh) Predicted?.Invoke(events);
    }

    private void ProcessInbox()
    {
        while (inbox.Count > 0)
        {
            var packet = inbox.Dequeue();
            try
            {
                if (packet[0] == InputPacket) ReadInputs(packet);
                else if (packet[0] == SnapshotPacket) ReadSnapshot(packet);
            }
            catch (Exception e) when (e is EndOfStreamException || e is IOException || e is ArgumentException)
            {
            }
        }

        if (rollbackFrom == int.MaxValue) return;

        int from = rollbackFrom;
        rollbackFrom = int.MaxValue;
        if (from >= tick) return;

        BeforeCorrection = State;
        Corrected = true;
        Rollbacks++;
        RolledBackTicks += tick - from;
        for (int t = from; t < tick; t++) Simulate(t, false);
    }

    private void ReadInputs(byte[] packet)
    {
        using var reader = new BinaryReader(new MemoryStream(packet));
        reader.ReadByte();
        if (reader.ReadInt32() != matchId) return;
        reader.ReadInt32();
        int ack = reader.ReadInt32();
        int first = reader.ReadInt32();
        int count = reader.ReadByte();
        var inputs = new ushort[count];
        for (int i = 0; i < count; i++) inputs[i] = reader.ReadUInt16();
        int ahead = reader.ReadSByte();
        int checksumTick = reader.ReadInt32();
        uint checksum = reader.ReadUInt32();

        remoteAck = Math.Max(remoteAck, ack);
        remoteAhead = ahead;

        for (int i = 0; i < count; i++)
        {
            int t = first + i;
            if (t < 0 || t <= remoteConfirmed - History + 1) continue;
            if (t > tick + History / 2) continue;
            if (remoteKnown[t % History] == t) continue;

            remoteKnown[t % History] = t;
            remoteInputs[t % History] = inputs[i];
            if (t < tick && usedRemote[t % History] != inputs[i]) rollbackFrom = Math.Min(rollbackFrom, t);
        }

        int before = remoteConfirmed;
        while (remoteKnown[(remoteConfirmed + 1) % History] == remoteConfirmed + 1) remoteConfirmed++;

        if (remoteConfirmed > before)
        {
            for (int t = before + 1; t <= remoteConfirmed && t < tick; t++)
                if (usedRemote[t % History] != remoteInputs[t % History]) rollbackFrom = Math.Min(rollbackFrom, t);
        }

        if (checksumTick >= 0) remoteChecksums[checksumTick] = checksum;
        Compare(checksumTick);
    }

    private void ReadSnapshot(byte[] packet)
    {
        if (IsHost) return;

        using var reader = new BinaryReader(new MemoryStream(packet));
        reader.ReadByte();
        if (reader.ReadInt32() != matchId) return;
        int at = reader.ReadInt32();
        var snapshot = LiveState.Read(reader);
        if (at > tick || tick - at >= History - 1) return;

        states[at % History] = snapshot;
        rollbackFrom = Math.Min(rollbackFrom, at);
        Desynced = false;
        remoteChecksums.Clear();
        localChecksums.Clear();
    }

    private void EmitConfirmed()
    {
        int limit = Math.Min(remoteConfirmed, tick - 1);
        for (int t = confirmedCursor + 1; t <= limit; t++)
        {
            var before = states[t % History];
            var after = states[(t + 1) % History];
            confirmedCursor = t;

            byte missed = 0;
            if (after.GuestScore > before.GuestScore) missed = 1;
            else if (after.HostScore > before.HostScore) missed = 2;
            bool over = after.Phase == LivePhase.Over && before.Phase != LivePhase.Over;
            if (missed != 0 || over) ConfirmedEvent?.Invoke(new Confirmed(t + 1, missed, over));

            if ((t + 1) % ChecksumEvery == 0)
            {
                localChecksums[t + 1] = after.Checksum();
                lastChecksumTick = t + 1;
                Compare(t + 1);
            }
        }
    }

    private void Compare(int at)
    {
        if (at < 0 || !localChecksums.TryGetValue(at, out uint mine) || !remoteChecksums.TryGetValue(at, out uint theirs)) return;

        localChecksums.Remove(at);
        remoteChecksums.Remove(at);
        if (mine == theirs || Desynced) return;

        Desynced = true;
        DesyncTick = at;
        DesyncDetected?.Invoke(at);
        if (IsHost) snapshotPending = true;
    }

    private void SendInputs()
    {
        if (send == null) return;

        int last = tick - 1 + InputDelay;
        int first = Math.Max(Math.Max(0, remoteAck + 1), last - MaxInputsPerPacket + 1);
        int count = Math.Max(0, last - first + 1);

        using var stream = new MemoryStream(24 + count * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write(InputPacket);
        writer.Write(matchId);
        writer.Write(tick);
        writer.Write(remoteConfirmed);
        writer.Write(first);
        writer.Write((byte)count);
        for (int i = 0; i < count; i++) writer.Write(LocalInputAt(first + i));
        writer.Write((sbyte)Math.Max(-100, Math.Min(100, tick - remoteConfirmed - 1)));
        writer.Write(lastChecksumTick);
        writer.Write(lastChecksumTick >= 0 && localChecksums.TryGetValue(lastChecksumTick, out uint sum) ? sum : LastChecksum());
        writer.Flush();
        send(stream.ToArray());

        if (snapshotPending && IsHost) SendSnapshot();
    }

    private uint LastChecksum()
    {
        if (lastChecksumTick < 0 || tick - lastChecksumTick >= History) return 0u;
        return states[lastChecksumTick % History].Checksum();
    }

    private void SendSnapshot()
    {
        int at = Math.Min(remoteConfirmed, tick - 1) + 1;
        if (at <= 0 || tick - at >= History - 1) return;

        using var stream = new MemoryStream(96);
        using var writer = new BinaryWriter(stream);
        writer.Write(SnapshotPacket);
        writer.Write(matchId);
        writer.Write(at);
        states[at % History].Write(writer);
        writer.Flush();
        send(stream.ToArray());

        snapshotPending = false;
        Desynced = false;
    }
}
