using System;

public interface IMatchLink
{
    bool IsHost { get; }
    bool PeerConnected { get; }
    int PingMs { get; }

    event Action<MatchMessage> Received;
    event Action<bool> PeerChanged;
    event Action<byte[]> LiveReceived;

    void Send(MatchMessage message);
    void SendLive(byte[] data);
    void Close();
}
