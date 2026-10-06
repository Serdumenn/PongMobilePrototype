using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class FakeFriendsBackend : IFriendsBackend
{
    public readonly List<FriendRecord> FriendList = new List<FriendRecord>();
    public readonly List<string> IncomingList = new List<string>();
    public readonly List<string> OutgoingList = new List<string>();
    public readonly List<string> Calls = new List<string>();
    public readonly List<(string To, FriendInvite Invite)> SentInvites = new List<(string, FriendInvite)>();
    public readonly List<FriendStatus> Presence = new List<FriendStatus>();
    public readonly HashSet<string> KnownTags = new HashSet<string>();

    public event Action Changed;
    public event Action<string, FriendInvite> InviteReceived;

    public IReadOnlyList<FriendRecord> Friends => FriendList;
    public IReadOnlyList<string> Incoming => IncomingList;
    public IReadOnlyList<string> Outgoing => OutgoingList;

    public Task<string> StartAsync(string baseName)
    {
        Calls.Add("start");
        return Task.FromResult(baseName + "#1234");
    }

    public Task<FriendsResult> AddByTagAsync(string tag)
    {
        Calls.Add("tag " + tag);
        if (!KnownTags.Contains(tag)) return Task.FromResult(FriendsResult.NotFound);
        OutgoingList.Add("id-" + tag);
        return Done();
    }

    public Task<FriendsResult> AddAsync(string playerId)
    {
        Calls.Add("add " + playerId);
        if (IncomingList.Remove(playerId)) FriendList.Add(new FriendRecord(playerId, FriendStatus.Online, default));
        else OutgoingList.Add(playerId);
        return Done();
    }

    public Task<FriendsResult> DeclineAsync(string playerId)
    {
        Calls.Add("decline " + playerId);
        IncomingList.Remove(playerId);
        return Done();
    }

    public Task<FriendsResult> CancelAsync(string playerId)
    {
        Calls.Add("cancel " + playerId);
        OutgoingList.Remove(playerId);
        return Done();
    }

    public Task<FriendsResult> RemoveAsync(string playerId)
    {
        Calls.Add("remove " + playerId);
        FriendList.RemoveAll(f => f.Id == playerId);
        return Done();
    }

    public Task<FriendsResult> BlockAsync(string playerId)
    {
        Calls.Add("block " + playerId);
        FriendList.RemoveAll(f => f.Id == playerId);
        IncomingList.Remove(playerId);
        return Done();
    }

    public Task<FriendsResult> InviteAsync(string playerId, FriendInvite invite)
    {
        Calls.Add("invite " + playerId);
        SentInvites.Add((playerId, invite));
        return Task.FromResult(FriendsResult.Ok);
    }

    public Task SetPresenceAsync(FriendStatus status)
    {
        Presence.Add(status);
        return Task.CompletedTask;
    }

    public Task RefreshAsync()
    {
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Calls.Add("clear");
        FriendList.Clear();
        IncomingList.Clear();
        OutgoingList.Clear();
        return Done();
    }

    public void Raise()
    {
        Changed?.Invoke();
    }

    public void Invite(string fromId, string code, string mode)
    {
        InviteReceived?.Invoke(fromId, FriendInvite.For(code, mode));
    }

    private Task<FriendsResult> Done()
    {
        Changed?.Invoke();
        return Task.FromResult(FriendsResult.Ok);
    }
}
