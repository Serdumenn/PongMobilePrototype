using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;

public sealed class OnlineLobby : MonoBehaviour
{
    public const string ProtocolVersion = "2";

    public enum Failure
    {
        None,
        Offline,
        NotFound,
        Full,
        VersionMismatch,
        ConnectionLost,
        HostLeft,
        Unknown
    }

    private const string ModeKey = "mode";
    private const string ProtocolKey = "proto";
    private const string NameKey = "name";
    private const string LookKey = "look";
    private const string ReadyKey = "ready";

    [SerializeField] private OnlineService online;
    [SerializeField] private List<GameModeDefinition> modes = new List<GameModeDefinition>();
    [SerializeField] private float reconnectWindow = 10f;
    [SerializeField] private float quickMatchTimeout = 6f;

    private readonly List<LobbyPlayer> players = new List<LobbyPlayer>();
    private ISession session;
    private string hostId;
    private bool busy;
    private float pausedAt = -1f;
    private float reconnectDeadline;
    private Task pendingSave = Task.CompletedTask;

    public bool InLobby => session != null;
    public bool IsHost => session != null && session.IsHost;
    public bool Busy => busy;
    public bool Reconnecting { get; private set; }
    public string Code => session?.Code;
    public string ModeId => Property(ModeKey);
    public GameModeDefinition Mode => FindMode(ModeId);
    public IReadOnlyList<GameModeDefinition> ModeList => modes;
    public bool IsPrivate => session != null && session.IsPrivate;
    public float ReconnectWindow => reconnectWindow;
    public float ReconnectRemaining => Reconnecting ? Mathf.Max(0f, reconnectDeadline - Time.realtimeSinceStartup) : 0f;
    public int MaxPlayers => session != null ? session.MaxPlayers : 0;
    public IReadOnlyList<LobbyPlayer> Players => players;
    public int MinPlayers => Mode != null ? Mathf.Max(2, Mode.MinPlayers) : MaxPlayers;
    public bool HasEnoughPlayers => session != null && players.Count >= MinPlayers;
    public bool AllReady => LobbyPlayer.AllReady(players, MinPlayers);

    public event Action Changed;
    public event Action<Failure> Closed;

    public Task<Failure> CreateAsync(string modeId, int maxPlayers, string look)
    {
        return Enter(async () => await MultiplayerService.Instance.CreateSessionAsync(Options(modeId, maxPlayers, look, true)));
    }

    public Task<Failure> JoinAsync(string code, string look)
    {
        string normalized = SessionCode.Normalize(code);
        if (!SessionCode.IsValid(normalized)) return Task.FromResult(Failure.NotFound);

        var options = new JoinSessionOptions { PlayerProperties = PlayerProperties(look, false) };
        return Enter(() => MultiplayerService.Instance.JoinSessionByCodeAsync(normalized, options));
    }

    public Task<Failure> QuickMatchAsync(string modeId, int maxPlayers, string look)
    {
        var quick = new QuickJoinOptions
        {
            CreateSession = true,
            Timeout = TimeSpan.FromSeconds(quickMatchTimeout),
            Filters = new List<FilterOption>
            {
                new FilterOption(FilterField.StringIndex1, modeId, FilterOperation.Equal),
                new FilterOption(FilterField.StringIndex2, ProtocolVersion, FilterOperation.Equal)
            }
        };
        return Enter(() => MultiplayerService.Instance.MatchmakeSessionAsync(quick, Options(modeId, maxPlayers, look, false)));
    }

    public async Task LeaveAsync()
    {
        var leaving = session;
        Detach();
        if (leaving == null) return;

        try
        {
            await leaving.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Leaving the lobby failed: {e.Message}");
        }
    }

    public Task SetReadyAsync(bool ready)
    {
        return SavePlayer(ReadyKey, ready ? "1" : "0");
    }

    public Task SetLookAsync(string look)
    {
        return SavePlayer(LookKey, look);
    }

    public bool PeerOnline
    {
        get
        {
            var network = NetworkManager.Singleton;
            if (session == null || !NetcodeLink.CanOpen(network)) return false;
            return network.IsServer ? network.ConnectedClientsIds.Count > 1 : network.IsConnectedClient;
        }
    }

    public bool EveryoneOnline
    {
        get
        {
            var network = NetworkManager.Singleton;
            if (session == null || !NetcodeLink.CanOpen(network)) return false;
            return network.IsServer ? network.ConnectedClientsIds.Count >= players.Count : network.IsConnectedClient;
        }
    }

    public string MyPlayerId => session?.CurrentPlayer?.Id;

    public LobbyPlayer? Opponent
    {
        get
        {
            foreach (var player in players)
                if (!player.IsYou) return player;
            return null;
        }
    }

    public IMatchLink CreateLink()
    {
        return PeerOnline ? new NetcodeLink(NetworkManager.Singleton, PingMs) : null;
    }

    public GameModeDefinition FindMode(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var mode in modes)
            if (mode != null && mode.Id == id) return mode;
        return null;
    }

    public int PingMs()
    {
        var network = NetworkManager.Singleton;
        if (session == null || network == null || !network.IsListening) return -1;

        var transport = network.NetworkConfig.NetworkTransport;
        if (!network.IsHost) return (int)transport.GetCurrentRtt(NetworkManager.ServerClientId);

        foreach (ulong id in network.ConnectedClientsIds)
            if (id != network.LocalClientId) return (int)transport.GetCurrentRtt(id);
        return -1;
    }

    private async Task<Failure> Enter(Func<Task<ISession>> open)
    {
        if (busy) return Failure.Unknown;
        busy = true;
        try
        {
            if (session != null) await LeaveAsync();

            if (online == null) return Failure.Unknown;
            await online.ConnectAsync();
            if (!online.IsReady) return OnlineService.HasInternet ? Failure.Unknown : Failure.Offline;

            EnsureNetwork();
            var opened = await open();
            if (Property(opened, ProtocolKey) != ProtocolVersion)
            {
                await opened.LeaveAsync();
                return Failure.VersionMismatch;
            }

            Attach(opened);
            return Failure.None;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Lobby request failed: {e.Message}");
            return Classify(e);
        }
        finally
        {
            busy = false;
        }
    }

    private static void EnsureNetwork()
    {
        if (NetworkManager.Singleton != null) return;

        var host = new GameObject("Network");
        var transport = host.AddComponent<UnityTransport>();
        var network = host.AddComponent<NetworkManager>();
        if (network.NetworkConfig == null) network.NetworkConfig = new NetworkConfig();
        network.NetworkConfig.NetworkTransport = transport;
        network.NetworkConfig.EnableSceneManagement = false;
    }

    private SessionOptions Options(string modeId, int maxPlayers, string look, bool isPrivate)
    {
        return new SessionOptions
        {
            MaxPlayers = maxPlayers,
            IsPrivate = isPrivate,
            SessionProperties = new Dictionary<string, SessionProperty>
            {
                { ModeKey, new SessionProperty(modeId, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                { ProtocolKey, new SessionProperty(ProtocolVersion, VisibilityPropertyOptions.Public, PropertyIndex.String2) }
            },
            PlayerProperties = PlayerProperties(look, false)
        }.WithRelayNetwork();
    }

    private Dictionary<string, PlayerProperty> PlayerProperties(string look, bool ready)
    {
        return new Dictionary<string, PlayerProperty>
        {
            { NameKey, new PlayerProperty(online != null ? online.PlayerName : string.Empty, VisibilityPropertyOptions.Member) },
            { LookKey, new PlayerProperty(look ?? string.Empty, VisibilityPropertyOptions.Member) },
            { ReadyKey, new PlayerProperty(ready ? "1" : "0", VisibilityPropertyOptions.Member) }
        };
    }

    private Task SavePlayer(string key, string value)
    {
        pendingSave = SaveAfter(pendingSave, key, value);
        return pendingSave;
    }

    private async Task SaveAfter(Task previous, string key, string value)
    {
        try
        {
            await previous;
        }
        catch (Exception)
        {
        }

        var target = session;
        if (target == null) return;

        target.CurrentPlayer.SetProperty(key, new PlayerProperty(value, VisibilityPropertyOptions.Member));
        Refresh();
        try
        {
            await target.SaveCurrentPlayerDataAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Saving lobby player data failed: {e.Message}");
        }
    }

    private void Attach(ISession opened)
    {
        session = opened;
        hostId = session.Host;
        session.Changed += Refresh;
        session.PlayerJoined += OnPlayerEvent;
        session.PlayerHasLeft += OnPlayerEvent;
        session.PlayerPropertiesChanged += Refresh;
        session.SessionPropertiesChanged += Refresh;
        session.RemovedFromSession += OnRemoved;
        session.Deleted += OnRemoved;
        session.StateChanged += OnStateChanged;
        session.SessionHostChanged += OnHostChanged;
        Refresh();
    }

    private void Detach()
    {
        if (session != null)
        {
            session.Changed -= Refresh;
            session.PlayerJoined -= OnPlayerEvent;
            session.PlayerHasLeft -= OnPlayerEvent;
            session.PlayerPropertiesChanged -= Refresh;
            session.SessionPropertiesChanged -= Refresh;
            session.RemovedFromSession -= OnRemoved;
            session.Deleted -= OnRemoved;
            session.StateChanged -= OnStateChanged;
            session.SessionHostChanged -= OnHostChanged;
        }

        session = null;
        Reconnecting = false;
        players.Clear();
        Changed?.Invoke();
    }

    private void Refresh()
    {
        players.Clear();
        if (session != null)
        {
            string me = session.CurrentPlayer?.Id;
            foreach (var player in session.Players)
            {
                string name = Property(player, NameKey);
                players.Add(new LobbyPlayer(
                    player.Id,
                    string.IsNullOrEmpty(name) ? PlayerNames.ForId(player.Id) : name,
                    Property(player, LookKey),
                    Property(player, ReadyKey) == "1",
                    player.Id == session.Host,
                    player.Id == me));
            }
            LobbyPlayer.Order(players);
        }
        Changed?.Invoke();
    }

    private void OnPlayerEvent(string playerId)
    {
        Refresh();
    }

    private void OnRemoved()
    {
        Debug.Log("Lobby: removed from session");
        Close(Failure.HostLeft);
    }

    private void OnHostChanged(string newHost)
    {
        bool hostLeft = !string.IsNullOrEmpty(hostId) && newHost != hostId;
        Debug.Log($"Lobby: host changed to {newHost}, left={hostLeft}");
        hostId = newHost;
        if (hostLeft) Close(Failure.HostLeft);
    }

    private void OnStateChanged(SessionState state)
    {
        Debug.Log($"Lobby: session state {state}");
        if (state == SessionState.Disconnected) TryReconnect();
        else if (state == SessionState.Deleted) Close(Failure.HostLeft);
    }

    private async void TryReconnect()
    {
        if (session == null || Reconnecting) return;

        string id = session.Id;
        reconnectDeadline = Time.realtimeSinceStartup + reconnectWindow;
        Reconnecting = true;
        Changed?.Invoke();

        while (Time.realtimeSinceStartup < reconnectDeadline && Reconnecting)
        {
            try
            {
                var back = await MultiplayerService.Instance.ReconnectToSessionAsync(id);
                if (!Reconnecting) return;
                Detach();
                Attach(back);
                return;
            }
            catch (Exception)
            {
                await Task.Delay(1000);
            }
        }

        if (Reconnecting) Close(Failure.ConnectionLost);
    }

    private void Close(Failure reason)
    {
        if (session == null) return;
        Debug.Log($"Lobby closed: {reason}");
        Detach();
        Closed?.Invoke(reason);
    }

    private void OnApplicationPause(bool paused)
    {
        if (session == null) return;

        if (paused)
        {
            pausedAt = Time.realtimeSinceStartup;
            return;
        }

        if (pausedAt >= 0f && Time.realtimeSinceStartup - pausedAt > reconnectWindow)
        {
            pausedAt = -1f;
            _ = LeaveAsync();
            Closed?.Invoke(Failure.ConnectionLost);
            return;
        }

        pausedAt = -1f;
        _ = session.RefreshAsync();
    }

    private void OnDestroy()
    {
        if (session != null) _ = LeaveAsync();
    }

    private string Property(string key)
    {
        return session != null ? Property(session, key) : null;
    }

    private static string Property(ISession target, string key)
    {
        if (target?.Properties == null) return null;
        return target.Properties.TryGetValue(key, out var property) ? property.Value : null;
    }

    private static string Property(IReadOnlyPlayer player, string key)
    {
        if (player?.Properties == null) return null;
        return player.Properties.TryGetValue(key, out var property) ? property.Value : null;
    }

    public static Failure Classify(Exception e)
    {
        if (e is LobbyServiceException lobby)
        {
            if (lobby.Reason == LobbyExceptionReason.LobbyNotFound || lobby.Reason == LobbyExceptionReason.InvalidJoinCode) return Failure.NotFound;
            if (lobby.Reason == LobbyExceptionReason.LobbyFull) return Failure.Full;
        }

        if (e is SessionException session && session.Error == SessionError.SessionNotFound) return Failure.NotFound;
        if (Mentions(e, "invalid character") || Mentions(e, "lobby code")) return Failure.NotFound;
        if (Mentions(e, "full")) return Failure.Full;
        return OnlineService.HasInternet ? Failure.Unknown : Failure.Offline;
    }

    private static bool Mentions(Exception e, string text)
    {
        return e.Message != null && e.Message.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
