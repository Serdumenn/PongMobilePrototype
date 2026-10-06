using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Models;
using Unity.Services.Friends.Notifications;
using Unity.Services.Friends.Options;
using UnityEngine;

public sealed class UgsFriendsBackend : IFriendsBackend
{
    private readonly List<FriendRecord> friends = new List<FriendRecord>();
    private readonly List<string> incoming = new List<string>();
    private readonly List<string> outgoing = new List<string>();

    private IFriendsService service;

    public event Action Changed;
    public event Action<string, FriendInvite> InviteReceived;

    public IReadOnlyList<FriendRecord> Friends => friends;
    public IReadOnlyList<string> Incoming => incoming;
    public IReadOnlyList<string> Outgoing => outgoing;

    public async Task<string> StartAsync(string baseName)
    {
        var auth = AuthenticationService.Instance;
        string name = await auth.GetPlayerNameAsync(false);
        if (!FriendTag.HasBase(name, baseName)) name = await auth.UpdatePlayerNameAsync(baseName);

        if (service == null)
        {
            var friendsService = FriendsService.Instance;
            await friendsService.InitializeAsync(new InitializeOptions().WithEvents(true).WithMemberPresence(true).WithMemberProfile(false));
            friendsService.RelationshipAdded += _ => Sync();
            friendsService.RelationshipDeleted += _ => Sync();
            friendsService.PresenceUpdated += _ => Sync();
            friendsService.MessageReceived += OnMessage;
            service = friendsService;
        }
        else await service.ForceRelationshipsRefreshAsync();

        Sync();
        return name;
    }

    public Task<FriendsResult> AddByTagAsync(string tag) => Run(() => service.AddFriendByNameAsync(tag));
    public Task<FriendsResult> AddAsync(string playerId) => Run(() => service.AddFriendAsync(playerId));
    public Task<FriendsResult> DeclineAsync(string playerId) => Run(() => service.DeleteIncomingFriendRequestAsync(playerId));
    public Task<FriendsResult> CancelAsync(string playerId) => Run(() => service.DeleteOutgoingFriendRequestAsync(playerId));
    public Task<FriendsResult> RemoveAsync(string playerId) => Run(() => service.DeleteFriendAsync(playerId));
    public Task<FriendsResult> BlockAsync(string playerId) => Run(() => service.AddBlockAsync(playerId));
    public Task<FriendsResult> InviteAsync(string playerId, FriendInvite invite) => Run(() => service.MessageAsync(playerId, invite));

    public async Task SetPresenceAsync(FriendStatus status)
    {
        if (service == null) return;
        var availability = status switch
        {
            FriendStatus.Online => Availability.Online,
            FriendStatus.Playing => Availability.Busy,
            _ => Availability.Offline
        };

        try
        {
            await service.SetPresenceAvailabilityAsync(availability);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Friends presence update failed: {e.Message}");
        }
    }

    public async Task RefreshAsync()
    {
        if (service == null) return;
        try
        {
            await service.ForceRelationshipsRefreshAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Friends refresh failed: {e.Message}");
        }
        Sync();
    }

    public async Task ClearAsync()
    {
        if (service == null) return;
        var ids = new List<string>();
        foreach (var relationship in service.Relationships) ids.Add(relationship.Id);
        foreach (var id in ids) await service.DeleteRelationshipAsync(id);
        Sync();
    }

    private async Task<FriendsResult> Run(Func<Task> action)
    {
        if (service == null) return FriendsResult.Offline;
        try
        {
            await action();
            Sync();
            return FriendsResult.Ok;
        }
        catch (Exception e)
        {
            var result = Classify(e);
            if (result == FriendsResult.Failed) Debug.LogWarning($"Friends request failed: {e.Message}");
            return result;
        }
    }

    private void Sync()
    {
        if (service == null) return;

        friends.Clear();
        incoming.Clear();
        outgoing.Clear();
        foreach (var relationship in service.Friends)
        {
            var member = relationship?.Member;
            if (member == null) continue;
            friends.Add(new FriendRecord(member.Id, StatusOf(member.Presence), member.Presence != null ? member.Presence.LastSeen : default));
        }
        foreach (var relationship in service.IncomingFriendRequests)
            if (relationship?.Member != null) incoming.Add(relationship.Member.Id);
        foreach (var relationship in service.OutgoingFriendRequests)
            if (relationship?.Member != null) outgoing.Add(relationship.Member.Id);

        Changed?.Invoke();
    }

    private void OnMessage(IMessageReceivedEvent message)
    {
        FriendInvite invite;
        try
        {
            invite = message.GetAs<FriendInvite>();
        }
        catch (Exception)
        {
            return;
        }

        if (invite != null && invite.IsValid) InviteReceived?.Invoke(message.UserId, invite);
    }

    private static FriendStatus StatusOf(Presence presence)
    {
        if (presence == null) return FriendStatus.Offline;
        return presence.Availability switch
        {
            Availability.Online => FriendStatus.Online,
            Availability.Busy => FriendStatus.Playing,
            _ => FriendStatus.Offline
        };
    }

    private static FriendsResult Classify(Exception e)
    {
        if (e is FriendsServiceException friends)
        {
            switch (friends.ErrorCode)
            {
                case FriendsErrorCode.UserTargetingSelf:
                    return FriendsResult.Self;
                case FriendsErrorCode.FriendshipAlreadyExists:
                case FriendsErrorCode.RelationshipAlreadyExists:
                    return FriendsResult.AlreadyFriends;
                case FriendsErrorCode.ActionUnauthorizedWhenBlocked:
                    return FriendsResult.Blocked;
                case FriendsErrorCode.FriendLimitReached:
                case FriendsErrorCode.FriendRequestLimitReached:
                case FriendsErrorCode.TargetsFriendLimitReached:
                case FriendsErrorCode.BlockLimitReached:
                    return FriendsResult.Full;
                case FriendsErrorCode.NetworkError:
                case FriendsErrorCode.NotificationConnectionError:
                    return FriendsResult.Offline;
                case FriendsErrorCode.RelationshipNotFound:
                case FriendsErrorCode.InvalidCreateTarget:
                    return FriendsResult.NotFound;
            }
            if (friends.StatusCode == HttpStatusCode.NotFound) return FriendsResult.NotFound;
        }

        if (e is RequestFailedException failed && failed.ErrorCode == CommonErrorCodes.TransportError) return FriendsResult.Offline;
        return FriendsResult.Failed;
    }
}
