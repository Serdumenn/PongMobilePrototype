using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class OnlineFriends : MonoBehaviour
{
    public enum Status
    {
        Off,
        Starting,
        Ready,
        Failed
    }

    public enum Relation
    {
        None,
        Self,
        Friend,
        Sent,
        Received
    }

    public readonly struct Friend
    {
        public readonly string Id;
        public readonly string Name;
        public readonly FriendStatus Status;
        public readonly DateTime LastSeen;

        public Friend(string id, FriendStatus status, DateTime lastSeen)
        {
            Id = id;
            Name = PlayerNames.ForId(id);
            Status = status;
            LastSeen = lastSeen;
        }
    }

    public sealed class Invite
    {
        public string FromId;
        public string FromName;
        public string Code;
        public string ModeId;
        public float ReceivedAt;
    }

    [SerializeField] private OnlineService online;
    [SerializeField] private float InviteLifetime = 120f;
    [SerializeField] private float InviteCooldown = 20f;

    public static Func<IFriendsBackend> BackendFactory = () => new UgsFriendsBackend();

    private readonly List<Friend> friends = new List<Friend>();
    private readonly List<Friend> incoming = new List<Friend>();
    private readonly List<Friend> outgoing = new List<Friend>();
    private readonly HashSet<string> knownIncoming = new HashSet<string>();
    private readonly List<Invite> invites = new List<Invite>();
    private readonly Dictionary<string, float> invitedAt = new Dictionary<string, float>();

    private IFriendsBackend backend;
    private bool playing;
    private bool paused;
    private FriendStatus? sentPresence;

    public Status State { get; private set; } = Status.Off;
    public bool IsReady => State == Status.Ready;
    public string MyTag { get; private set; }
    public IReadOnlyList<Friend> Friends => friends;
    public IReadOnlyList<Friend> Incoming => incoming;
    public IReadOnlyList<Friend> Outgoing => outgoing;

    public int OnlineCount
    {
        get
        {
            int count = 0;
            foreach (var friend in friends)
                if (friend.Status != FriendStatus.Offline) count++;
            return count;
        }
    }

    public event Action Changed;
    public event Action<string> RequestReceived;
    public event Action<Invite> InviteReceived;

    private async void Start()
    {
        if (online == null) online = FindFirstObjectByType<OnlineService>();
        if (online == null) return;

        online.StateChanged += OnOnlineState;
        if (OnlineService.Disabled) return;

        await System.Threading.Tasks.Task.Yield();
        if (online.IsReady) _ = StartAsync(BackendFactory());
    }

    private void OnDestroy()
    {
        if (online != null) online.StateChanged -= OnOnlineState;
        Attach(null);
    }

    private void OnApplicationPause(bool pause)
    {
        paused = pause;
        PushPresence();
        if (!pause && IsReady) _ = backend.RefreshAsync();
    }

    public async Task StartAsync(IFriendsBackend with)
    {
        if (with == null || State == Status.Starting || State == Status.Ready) return;

        Attach(with);
        SetState(Status.Starting);
        try
        {
            MyTag = await backend.StartAsync(FriendTag.BaseName(online != null ? online.PlayerId : null));
            knownIncoming.Clear();
            foreach (var id in backend.Incoming) knownIncoming.Add(id);
            sentPresence = null;
            SetState(Status.Ready);
            Rebuild();
            PushPresence();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Friends could not start: {e.Message}");
            SetState(Status.Failed);
        }
    }

    public Relation RelationTo(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return Relation.None;
        if (online != null && playerId == online.PlayerId) return Relation.Self;
        if (Contains(friends, playerId)) return Relation.Friend;
        if (Contains(outgoing, playerId)) return Relation.Sent;
        if (Contains(incoming, playerId)) return Relation.Received;
        return Relation.None;
    }

    public bool RecentlyInvited(string playerId)
    {
        return playerId != null && invitedAt.TryGetValue(playerId, out float at) && Time.realtimeSinceStartup - at < InviteCooldown;
    }

    public Task<FriendsResult> AddByTagAsync(string tag)
    {
        if (!IsReady) return Task.FromResult(FriendsResult.Offline);
        if (string.IsNullOrEmpty(tag)) return Task.FromResult(FriendsResult.NotFound);
        if (string.Equals(tag, MyTag, StringComparison.OrdinalIgnoreCase)) return Task.FromResult(FriendsResult.Self);
        return backend.AddByTagAsync(tag);
    }

    public Task<FriendsResult> AddAsync(string playerId)
    {
        if (!IsReady) return Task.FromResult(FriendsResult.Offline);
        switch (RelationTo(playerId))
        {
            case Relation.Self:
                return Task.FromResult(FriendsResult.Self);
            case Relation.Friend:
            case Relation.Sent:
                return Task.FromResult(FriendsResult.AlreadyFriends);
        }
        return backend.AddAsync(playerId);
    }

    public Task<FriendsResult> AcceptAsync(string playerId) => IsReady ? backend.AddAsync(playerId) : Task.FromResult(FriendsResult.Offline);
    public Task<FriendsResult> DeclineAsync(string playerId) => IsReady ? backend.DeclineAsync(playerId) : Task.FromResult(FriendsResult.Offline);
    public Task<FriendsResult> CancelAsync(string playerId) => IsReady ? backend.CancelAsync(playerId) : Task.FromResult(FriendsResult.Offline);
    public Task<FriendsResult> RemoveAsync(string playerId) => IsReady ? backend.RemoveAsync(playerId) : Task.FromResult(FriendsResult.Offline);
    public Task<FriendsResult> BlockAsync(string playerId) => IsReady ? backend.BlockAsync(playerId) : Task.FromResult(FriendsResult.Offline);

    public async Task<FriendsResult> InviteAsync(string playerId, string code, string modeId)
    {
        if (!IsReady) return FriendsResult.Offline;
        if (RelationTo(playerId) != Relation.Friend) return FriendsResult.NotFound;
        if (RecentlyInvited(playerId)) return FriendsResult.Ok;

        invitedAt[playerId] = Time.realtimeSinceStartup;
        var result = await backend.InviteAsync(playerId, FriendInvite.For(code, modeId));
        if (result != FriendsResult.Ok) invitedAt.Remove(playerId);
        else if (isActiveAndEnabled) StartCoroutine(EndCooldown());
        Changed?.Invoke();
        return result;
    }

    private System.Collections.IEnumerator EndCooldown()
    {
        yield return new WaitForSecondsRealtime(InviteCooldown + 0.1f);
        Changed?.Invoke();
    }

    public bool TryTakeInvite(out Invite invite)
    {
        float now = Time.realtimeSinceStartup;
        invites.RemoveAll(i => now - i.ReceivedAt > InviteLifetime || RelationTo(i.FromId) != Relation.Friend);
        if (invites.Count == 0)
        {
            invite = null;
            return false;
        }

        invite = invites[0];
        invites.RemoveAt(0);
        return true;
    }

    public bool HasInvite => invites.Count > 0;

    public void SetPlaying(bool value)
    {
        if (playing == value) return;
        playing = value;
        PushPresence();
    }

    public async Task ClearAsync()
    {
        if (!IsReady) return;
        try
        {
            await backend.ClearAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Clearing friends failed: {e.Message}");
        }
    }

    public Task RefreshAsync() => IsReady ? backend.RefreshAsync() : Task.CompletedTask;

    private void OnOnlineState(OnlineService.Status status)
    {
        if (status == OnlineService.Status.Ready)
        {
            if (State == Status.Off || State == Status.Failed) _ = StartAsync(backend ?? BackendFactory());
            return;
        }

        if ((status == OnlineService.Status.Offline || status == OnlineService.Status.Connecting) && State != Status.Off)
        {
            friends.Clear();
            incoming.Clear();
            outgoing.Clear();
            invites.Clear();
            MyTag = null;
            SetState(Status.Off);
        }
    }

    private void Attach(IFriendsBackend with)
    {
        if (backend == with) return;
        if (backend != null)
        {
            backend.Changed -= OnBackendChanged;
            backend.InviteReceived -= OnInvite;
        }
        backend = with;
        if (backend != null)
        {
            backend.Changed += OnBackendChanged;
            backend.InviteReceived += OnInvite;
        }
    }

    private void OnBackendChanged()
    {
        if (State != Status.Ready) return;
        Rebuild();
    }

    private void Rebuild()
    {
        friends.Clear();
        foreach (var record in backend.Friends) friends.Add(new Friend(record.Id, record.Status, record.LastSeen));
        friends.Sort(Compare);

        incoming.Clear();
        foreach (var id in backend.Incoming)
        {
            incoming.Add(new Friend(id, FriendStatus.Offline, default));
            if (knownIncoming.Add(id)) RequestReceived?.Invoke(PlayerNames.ForId(id));
        }
        knownIncoming.RemoveWhere(id => !Contains(incoming, id));

        outgoing.Clear();
        foreach (var id in backend.Outgoing) outgoing.Add(new Friend(id, FriendStatus.Offline, default));

        Changed?.Invoke();
    }

    private void OnInvite(string fromId, FriendInvite invite)
    {
        if (invite == null || !invite.IsValid || RelationTo(fromId) != Relation.Friend) return;

        invites.RemoveAll(i => i.FromId == fromId);
        var entry = new Invite
        {
            FromId = fromId,
            FromName = PlayerNames.ForId(fromId),
            Code = invite.code,
            ModeId = invite.mode,
            ReceivedAt = Time.realtimeSinceStartup
        };
        invites.Add(entry);
        InviteReceived?.Invoke(entry);
    }

    private FriendStatus CurrentPresence()
    {
        if (paused) return FriendStatus.Offline;
        return playing ? FriendStatus.Playing : FriendStatus.Online;
    }

    private void PushPresence()
    {
        if (!IsReady) return;
        var presence = CurrentPresence();
        if (sentPresence == presence) return;
        sentPresence = presence;
        _ = backend.SetPresenceAsync(presence);
    }

    private void SetState(Status state)
    {
        if (State == state) return;
        State = state;
        Changed?.Invoke();
    }

    private static bool Contains(List<Friend> list, string id)
    {
        foreach (var friend in list)
            if (friend.Id == id) return true;
        return false;
    }

    public static int Compare(Friend a, Friend b)
    {
        int rank = Rank(a.Status).CompareTo(Rank(b.Status));
        if (rank != 0) return rank;
        if (a.Status == FriendStatus.Offline && a.LastSeen != b.LastSeen) return b.LastSeen.CompareTo(a.LastSeen);
        return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
    }

    private static int Rank(FriendStatus status)
    {
        return status switch
        {
            FriendStatus.Online => 0,
            FriendStatus.Playing => 1,
            _ => 2
        };
    }
}
