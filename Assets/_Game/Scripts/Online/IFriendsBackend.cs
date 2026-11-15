using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum FriendStatus
{
    Offline,
    Online,
    Playing
}

public enum FriendsResult
{
    Ok,
    NotFound,
    Self,
    AlreadyFriends,
    Blocked,
    Full,
    Offline,
    Failed
}

public readonly struct FriendRecord
{
    public readonly string Id;
    public readonly FriendStatus Status;
    public readonly DateTime LastSeen;
    public readonly string Frame;

    public FriendRecord(string id, FriendStatus status, DateTime lastSeen, string frame = null)
    {
        Id = id;
        Status = status;
        LastSeen = lastSeen;
        Frame = frame;
    }
}

public interface IFriendsBackend
{
    event Action Changed;
    event Action<string, FriendInvite> InviteReceived;

    IReadOnlyList<FriendRecord> Friends { get; }
    IReadOnlyList<string> Incoming { get; }
    IReadOnlyList<string> Outgoing { get; }

    Task<string> StartAsync(string baseName);
    Task<FriendsResult> AddByTagAsync(string tag);
    Task<FriendsResult> AddAsync(string playerId);
    Task<FriendsResult> DeclineAsync(string playerId);
    Task<FriendsResult> CancelAsync(string playerId);
    Task<FriendsResult> RemoveAsync(string playerId);
    Task<FriendsResult> BlockAsync(string playerId);
    Task<FriendsResult> InviteAsync(string playerId, FriendInvite invite);
    Task SetPresenceAsync(FriendStatus status, string frame);
    Task RefreshAsync();
    Task ClearAsync();
}
