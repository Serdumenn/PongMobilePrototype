using System;
using System.Collections.Generic;
using NUnit.Framework;

public sealed class FakeMatchLink : IMatchLink
{
    public readonly List<MatchMessage> Sent = new List<MatchMessage>();
    public readonly List<byte[]> SentLive = new List<byte[]>();

    public FakeMatchLink(bool host)
    {
        IsHost = host;
    }

    public bool IsHost { get; }
    public bool PeerConnected { get; private set; } = true;
    public int PingMs => 42;
    public bool Closed { get; private set; }

    public event Action<MatchMessage> Received;
    public event Action<bool> PeerChanged;
    public event Action<byte[]> LiveReceived;

    public void Send(MatchMessage message)
    {
        Assert.IsTrue(MatchMessage.TryParse(message.ToBytes(), out var wire), "Message must survive the wire");
        Sent.Add(wire);
    }

    public void SendLive(byte[] data)
    {
        SentLive.Add(data);
    }

    public void DeliverLive(byte[] data)
    {
        LiveReceived?.Invoke(data);
    }

    public void Close()
    {
        Closed = true;
    }

    public void Deliver(MatchMessage message)
    {
        Assert.IsTrue(MatchMessage.TryParse(message.ToBytes(), out var wire));
        Received?.Invoke(wire);
    }

    public void SetPeer(bool connected)
    {
        PeerConnected = connected;
        PeerChanged?.Invoke(connected);
    }

    public MatchMessage? Last(MatchMessageType type)
    {
        for (int i = Sent.Count - 1; i >= 0; i--)
            if (Sent[i].Type == type) return Sent[i];
        return null;
    }

    public int Count(MatchMessageType type)
    {
        int count = 0;
        foreach (var message in Sent) if (message.Type == type) count++;
        return count;
    }
}
