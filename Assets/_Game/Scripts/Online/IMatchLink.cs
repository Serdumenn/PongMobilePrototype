using System;

public interface IMatchLink
{
    bool IsHost { get; }
    bool PeerConnected { get; }
    int PingMs { get; }

    event Action<MatchMessage> Received;
    event Action<bool> PeerChanged;

    void Send(MatchMessage message);
    void Close();
}
