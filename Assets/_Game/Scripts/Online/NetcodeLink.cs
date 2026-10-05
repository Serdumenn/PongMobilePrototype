using System;
using Unity.Collections;
using Unity.Netcode;

public sealed class NetcodeLink : IMatchLink
{
    private const string Channel = "pingi.match";

    private readonly NetworkManager network;
    private readonly Func<int> ping;
    private bool closed;

    public NetcodeLink(NetworkManager network, Func<int> ping)
    {
        this.network = network;
        this.ping = ping;
        network.CustomMessagingManager.RegisterNamedMessageHandler(Channel, OnMessage);
        network.OnClientConnectedCallback += OnPeerEvent;
        network.OnClientDisconnectCallback += OnPeerEvent;
    }

    public bool IsHost => network != null && network.IsServer;

    public bool PeerConnected
    {
        get
        {
            if (closed || network == null || !network.IsListening) return false;
            return network.IsServer ? network.ConnectedClientsIds.Count > 1 : network.IsConnectedClient;
        }
    }

    public int PingMs => ping != null ? ping() : -1;

    public event Action<MatchMessage> Received;
    public event Action<bool> PeerChanged;

    public static bool CanOpen(NetworkManager network)
    {
        return network != null && network.IsListening && network.CustomMessagingManager != null;
    }

    public void Send(MatchMessage message)
    {
        if (closed || !CanOpen(network)) return;

        byte[] data = message.ToBytes();
        using var writer = new FastBufferWriter(data.Length + 8, Allocator.Temp);
        writer.WriteValueSafe(data);

        if (!network.IsServer)
        {
            network.CustomMessagingManager.SendNamedMessage(Channel, NetworkManager.ServerClientId, writer);
            return;
        }

        foreach (ulong id in network.ConnectedClientsIds)
            if (id != NetworkManager.ServerClientId) network.CustomMessagingManager.SendNamedMessage(Channel, id, writer);
    }

    public void Close()
    {
        if (closed) return;
        closed = true;

        if (network == null) return;
        network.CustomMessagingManager?.UnregisterNamedMessageHandler(Channel);
        network.OnClientConnectedCallback -= OnPeerEvent;
        network.OnClientDisconnectCallback -= OnPeerEvent;
    }

    private void OnMessage(ulong sender, FastBufferReader reader)
    {
        if (closed) return;

        reader.ReadValueSafe(out byte[] data);
        if (MatchMessage.TryParse(data, out var message)) Received?.Invoke(message);
    }

    private void OnPeerEvent(ulong clientId)
    {
        if (!closed) PeerChanged?.Invoke(PeerConnected);
    }
}
