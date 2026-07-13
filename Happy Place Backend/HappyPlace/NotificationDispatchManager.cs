using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HappyWorld.HappyPlace;

public static class NotificationDispatchManager {
    // Fields

    private static readonly int DeviceRetentionDays = 30;
    private static readonly int QuietWindowMs = 500;
    private static readonly int MaxWaitMs = 2000;
    private static readonly int MinIntervalMs = 1000;
    private static readonly int ClaimTtlSeconds = 30;

    // Properties

    public static bool BackgroundSweepEnabled { get; set; } = true;

    // Methods - Event Hooks (Waiting / helper broadcast)

    public static void MarkWaitingDirtyForAllHelpers() {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.Waiting));
        }
        catch (Exception) {
        }
    }

    public static void ActivateWaitingChannel(Guid helperUserAccountId) {
        try {
            EnsureWaitingChannel(helperUserAccountId);
            using var dbContext = HappyPlaceDbContext.Create();
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.Waiting && field.RecipientUserAccountId == helperUserAccountId));
        }
        catch (Exception) {
        }
    }

    public static void DeactivateWaitingChannel(Guid helperUserAccountId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.Waiting && field.RecipientUserAccountId == helperUserAccountId);
        }
        catch (Exception) {
        }
    }

    // Methods - Event Hooks (Offers / seeker)

    public static void MarkOffersDirty(Guid chatGroupId) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
            if (chatGroup == null || chatGroup.OwnerUserAccountId == null)
                return;
            EnsureOffersChannel(chatGroupId, chatGroup.OwnerUserAccountId.Value);
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == chatGroupId));
        }
        catch (Exception) {
        }
    }

    public static void RemoveOffersChannel(Guid chatGroupId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == chatGroupId);
        }
        catch (Exception) {
        }
    }

    // Methods - Event Hooks (Device registration)

    public static void ResetChannelsForRecipient(Guid recipientUserAccountId) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            DateTime now = DateTime.UtcNow;
            DateTime quietDue = now.AddMilliseconds(QuietWindowMs);
            dbContext.NotificationChannels
                .Where(field => field.RecipientUserAccountId == recipientUserAccountId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(field => field.LastSentCount, 0)
                    .SetProperty(field => field.LastEventAtUtc, now)
                    .SetProperty(field => field.FirstDirtyAtUtc, field => field.FirstDirtyAtUtc == null ? now : field.FirstDirtyAtUtc)
                    .SetProperty(field => field.DueAtUtc, quietDue));
        }
        catch (Exception) {
        }
    }

    // Methods - Event Hooks (Join Requests / group owner)

    public static void MarkJoinRequestsDirty(Guid chatGroupId) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
            if (chatGroup == null || chatGroup.OwnerUserAccountId == null)
                return;
            EnsureJoinRequestsChannel(chatGroupId, chatGroup.OwnerUserAccountId.Value);
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == chatGroupId));
        }
        catch (Exception) {
        }
    }

    public static void RemoveJoinRequestsChannel(Guid chatGroupId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == chatGroupId);
        }
        catch (Exception) {
        }
    }

    public static void SyncJoinRequestsOwner(Guid chatGroupId) {
        try {
            Guid? currentOwnerUserAccountId;
            bool ownerMatchesChannel;
            bool hasPendingRequests;
            using (var dbContext = HappyPlaceDbContext.Create()) {
                ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
                currentOwnerUserAccountId = chatGroup != null && chatGroup.Status == ChatGroupStatus.Active ? chatGroup.OwnerUserAccountId : null;
                NotificationChannel existingChannel = dbContext.NotificationChannels.SingleOrDefault(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == chatGroupId);
                ownerMatchesChannel = existingChannel == null || existingChannel.RecipientUserAccountId == currentOwnerUserAccountId;
                hasPendingRequests = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending);
            }
            if (!ownerMatchesChannel)
                TeardownChannels(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == chatGroupId);
            if (currentOwnerUserAccountId != null && hasPendingRequests)
                MarkJoinRequestsDirty(chatGroupId);
        }
        catch (Exception) {
        }
    }

    // Methods - Event Hooks (Messages / group members)

    public static void MarkMessagesDirty(Guid chatGroupId, Guid senderUserAccountId) {
        try {
            EnsureMessagesChannels(chatGroupId, senderUserAccountId);
            using var dbContext = HappyPlaceDbContext.Create();
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == chatGroupId && field.RecipientUserAccountId != senderUserAccountId));
        }
        catch (Exception) {
        }
    }

    public static void MarkMessagesReadDirty(Guid chatGroupId, Guid recipientUserAccountId) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == chatGroupId && field.RecipientUserAccountId == recipientUserAccountId));
        }
        catch (Exception) {
        }
    }

    public static void RemoveMessagesChannel(Guid chatGroupId, Guid recipientUserAccountId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == chatGroupId && field.RecipientUserAccountId == recipientUserAccountId);
        }
        catch (Exception) {
        }
    }

    public static void RemoveMessagesChannels(Guid chatGroupId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == chatGroupId);
        }
        catch (Exception) {
        }
    }

    // Methods - Event Hooks (Friend Requests / recipient)

    public static void MarkFriendRequestsDirty(Guid recipientUserAccountId) {
        try {
            EnsureFriendRequestsChannel(recipientUserAccountId);
            using var dbContext = HappyPlaceDbContext.Create();
            MarkDirty(dbContext.NotificationChannels.Where(field => field.Kind == NotificationChannelKind.FriendRequests && field.RecipientUserAccountId == recipientUserAccountId));
        }
        catch (Exception) {
        }
    }

    public static void RemoveFriendRequestsChannel(Guid recipientUserAccountId) {
        try {
            TeardownChannels(field => field.Kind == NotificationChannelKind.FriendRequests && field.RecipientUserAccountId == recipientUserAccountId);
        }
        catch (Exception) {
        }
    }

    // Methods - Event Pushes

    public static void SendJoinApprovedPush(Guid recipientUserAccountId, Guid chatGroupId, string chatGroupName) {
        try {
            SendToRecipientDevices(recipientUserAccountId, deviceToken => new PushMessage {
                Token = deviceToken,
                Title = "You're in!",
                Body = $"You were accepted into {chatGroupName}.",
                Data = new() { ["type"] = "joinApproved", ["chatGroupId"] = chatGroupId.ToString(), ["alerting"] = "true" },
                CollapseId = $"join-approved-{chatGroupId}",
                Alerting = true
            });
        }
        catch (Exception) {
        }
    }

    public static void SendFriendRequestAcceptedPush(Guid recipientUserAccountId, Guid accepterUserAccountId, string accepterDisplayName, string accepterUsername) {
        try {
            SendToRecipientDevices(recipientUserAccountId, deviceToken => new PushMessage {
                Token = deviceToken,
                Title = "New friend!",
                Body = $"{accepterDisplayName} accepted your friend request.",
                Data = new() { ["type"] = "friendAccepted", ["username"] = accepterUsername ?? "", ["alerting"] = "true" },
                CollapseId = $"friend-accepted-{accepterUserAccountId}",
                Alerting = true
            });
        }
        catch (Exception) {
        }
    }

    // Methods - Retention

    public static void RunRetentionSweep() {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            DateTime retentionCutoffUtc = DateTime.UtcNow.AddDays(-DeviceRetentionDays);
            dbContext.DeviceTokens
                .Where(field => field.LastSeenAtUtc < retentionCutoffUtc)
                .ExecuteDelete();
            dbContext.NotificationChannels
                .Where(field => field.Kind == NotificationChannelKind.Waiting
                    && !dbContext.DeviceTokens.Any(device => device.UserAccountId == field.RecipientUserAccountId)
                    && (field.LastEventAtUtc == null || field.LastEventAtUtc < retentionCutoffUtc))
                .ExecuteDelete();
        }
        catch (Exception) {
        }
    }

    // Methods - Sweep

    public static void Sweep() {
        Guid claimToken = Guid.NewGuid();
        List<NotificationChannel> claimedChannels = ClaimDueChannels(claimToken);
        foreach (NotificationChannel channel in claimedChannels) {
            try {
                ProcessChannel(channel);
            }
            catch (Exception) {
            }
        }
    }

    private static List<NotificationChannel> ClaimDueChannels(Guid claimToken) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        DateTime maxWaitCutoff = now.AddMilliseconds(-MaxWaitMs);
        DateTime minIntervalCutoff = now.AddMilliseconds(-MinIntervalMs);
        DateTime claimExpiry = now.AddSeconds(ClaimTtlSeconds);
        int claimedCount = dbContext.NotificationChannels
            .Where(field => field.DueAtUtc != null
                && (field.DueAtUtc <= now || (field.FirstDirtyAtUtc != null && field.FirstDirtyAtUtc <= maxWaitCutoff))
                && (field.LastSentAtUtc == null || field.LastSentAtUtc <= minIntervalCutoff)
                && (field.ClaimToken == null || field.ClaimExpiresAtUtc == null || field.ClaimExpiresAtUtc <= now))
            .ExecuteUpdate(setters => setters
                .SetProperty(field => field.ClaimToken, claimToken)
                .SetProperty(field => field.ClaimExpiresAtUtc, claimExpiry));
        if (claimedCount == 0)
            return [];
        return [.. dbContext.NotificationChannels.Where(field => field.ClaimToken == claimToken)];
    }

    private static void ProcessChannel(NotificationChannel channel) {
        int count = ComputeCount(channel);
        bool wasLive = channel.IsLive;
        bool sent = false;
        if (count <= 0) {
            if (wasLive)
                sent = SendDismissal(channel) > 0;
        }
        else if (count != channel.LastSentCount || !wasLive) {
            bool alerting = count > channel.LastSentCount;
            sent = SendCountUpdate(channel, count, alerting) > 0;
        }
        FinalizeChannel(channel, count, sent);
    }

    private static void FinalizeChannel(NotificationChannel channel, int count, bool sent) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        bool isLive = count > 0;
        if (sent) {
            dbContext.NotificationChannels.Where(field => field.Id == channel.Id).ExecuteUpdate(setters => setters
                .SetProperty(field => field.LastSentCount, count)
                .SetProperty(field => field.IsLive, isLive)
                .SetProperty(field => field.LastSentAtUtc, now)
                .SetProperty(field => field.ClaimToken, (Guid?)null)
                .SetProperty(field => field.ClaimExpiresAtUtc, (DateTime?)null));
        }
        else {
            dbContext.NotificationChannels.Where(field => field.Id == channel.Id).ExecuteUpdate(setters => setters
                .SetProperty(field => field.ClaimToken, (Guid?)null)
                .SetProperty(field => field.ClaimExpiresAtUtc, (DateTime?)null));
        }
        DateTime? claimedLastEvent = channel.LastEventAtUtc;
        dbContext.NotificationChannels
            .Where(field => field.Id == channel.Id && field.LastEventAtUtc == claimedLastEvent)
            .ExecuteUpdate(setters => setters
                .SetProperty(field => field.FirstDirtyAtUtc, (DateTime?)null)
                .SetProperty(field => field.DueAtUtc, (DateTime?)null));
    }

    // Methods - Counts

    private static int ComputeCount(NotificationChannel channel) {
        using var dbContext = HappyPlaceDbContext.Create();
        if (channel.Kind == NotificationChannelKind.Waiting)
            return CountWaitingForHelper(dbContext, channel.RecipientUserAccountId);
        if (channel.Kind == NotificationChannelKind.JoinRequests)
            return CountPendingJoinRequests(dbContext, channel.ScopeChatGroupId);
        if (channel.Kind == NotificationChannelKind.Messages)
            return CountUnreadMessages(dbContext, channel.ScopeChatGroupId, channel.RecipientUserAccountId);
        if (channel.Kind == NotificationChannelKind.FriendRequests)
            return CountPendingFriendRequests(dbContext, channel.RecipientUserAccountId);
        return CountOffersForGroup(dbContext, channel.ScopeChatGroupId);
    }

    private static int CountPendingJoinRequests(HappyPlaceDbContext dbContext, Guid? chatGroupId) {
        if (chatGroupId == null)
            return 0;
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == chatGroupId.Value && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static int CountPendingFriendRequests(HappyPlaceDbContext dbContext, Guid recipientUserAccountId) {
        return dbContext.Friendships.Count(field => field.AddresseeUserAccountId == recipientUserAccountId && field.Status == FriendshipStatus.Pending);
    }

    private static int CountWaitingForHelper(HappyPlaceDbContext dbContext, Guid helperUserAccountId) {
        List<Guid> declinedChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Declined)
            .Select(field => field.ChatGroupId)];
        return dbContext.ChatGroups.Count(field => field.Status == ChatGroupStatus.Provisional
            && field.OwnerUserAccountId != helperUserAccountId
            && !declinedChatGroupIds.Contains(field.Id));
    }

    private static int CountOffersForGroup(HappyPlaceDbContext dbContext, Guid? chatGroupId) {
        if (chatGroupId == null)
            return 0;
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId.Value && field.Status == HelpOfferStatus.Offered);
    }

    private static int CountUnreadMessages(HappyPlaceDbContext dbContext, Guid? chatGroupId, Guid recipientUserAccountId) {
        if (chatGroupId == null)
            return 0;
        ChatGroupMember member = dbContext.ChatGroupMembers.SingleOrDefault(field => field.ChatGroupId == chatGroupId.Value && field.UserAccountId == recipientUserAccountId && field.Status == ChatGroupMemberStatus.Active);
        if (member == null)
            return 0;
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == chatGroupId.Value && !field.IsDeleted && field.SenderUserAccountId != recipientUserAccountId && field.Sequence > member.LastReadSequence);
    }

    // Methods - Sending

    private static int SendCountUpdate(NotificationChannel channel, int count, bool alerting) {
        string collapseId = BuildCollapseId(channel);
        NotificationContent content = BuildCountContent(channel, count);
        Dictionary<string, string> data = new(content.Data) {
            ["alerting"] = alerting ? "true" : "false"
        };
        return SendToRecipientDevices(channel.RecipientUserAccountId, deviceToken => new PushMessage {
            Token = deviceToken,
            Title = content.Title,
            Body = content.Body,
            Data = new Dictionary<string, string>(data),
            CollapseId = collapseId,
            Alerting = alerting
        });
    }

    private static int SendDismissal(NotificationChannel channel) {
        string collapseId = BuildCollapseId(channel);
        return SendToRecipientDevices(channel.RecipientUserAccountId, deviceToken => new PushMessage {
            Token = deviceToken,
            Data = new() { ["type"] = "dismiss", ["collapseId"] = collapseId },
            CollapseId = collapseId,
            IsDismiss = true
        });
    }

    private static int SendToRecipientDevices(Guid recipientUserAccountId, Func<string, PushMessage> buildMessage) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<DeviceToken> deviceTokens = [.. dbContext.DeviceTokens.Where(field => field.UserAccountId == recipientUserAccountId)];
        int deliveredCount = 0;
        foreach (DeviceToken deviceToken in deviceTokens) {
            try {
                PushSender.Create().Send(buildMessage(deviceToken.Token));
                deliveredCount++;
            }
            catch (PushTokenInvalidException) {
                dbContext.DeviceTokens.Where(field => field.Id == deviceToken.Id).ExecuteDelete();
            }
            catch (Exception) {
            }
        }
        return deliveredCount;
    }

    private static string BuildCollapseId(NotificationChannel channel) {
        if (channel.Kind == NotificationChannelKind.Waiting)
            return "help-waiting";
        if (channel.Kind == NotificationChannelKind.JoinRequests)
            return $"join-requests-{channel.ScopeChatGroupId}";
        if (channel.Kind == NotificationChannelKind.Messages)
            return $"chat-messages-{channel.ScopeChatGroupId}";
        if (channel.Kind == NotificationChannelKind.FriendRequests)
            return "friend-requests";
        return $"help-offers-{channel.ScopeChatGroupId}";
    }

    private static NotificationContent BuildCountContent(NotificationChannel channel, int count) {
        if (channel.Kind == NotificationChannelKind.Waiting) {
            string waitingBody = count == 1 ? "1 person needs help right now." : $"{count} people need help right now.";
            return new NotificationContent("People need help", waitingBody, new() {
                ["type"] = "helpWaiting",
                ["count"] = count.ToString()
            });
        }
        if (channel.Kind == NotificationChannelKind.JoinRequests) {
            string chatGroupName = LoadChatGroupName(channel.ScopeChatGroupId);
            string joinBody = count == 1 ? $"1 person wants to join {chatGroupName}." : $"{count} people want to join {chatGroupName}.";
            return new NotificationContent("Join requests", joinBody, new() {
                ["type"] = "joinRequests",
                ["count"] = count.ToString(),
                ["chatGroupId"] = channel.ScopeChatGroupId == null ? "" : channel.ScopeChatGroupId.Value.ToString()
            });
        }
        if (channel.Kind == NotificationChannelKind.Messages) {
            string chatGroupName = LoadChatGroupName(channel.ScopeChatGroupId);
            string messagesBody = count == 1 ? "1 new message." : $"{count} new messages.";
            return new NotificationContent(chatGroupName, messagesBody, new() {
                ["type"] = "chatMessages",
                ["count"] = count.ToString(),
                ["chatGroupId"] = channel.ScopeChatGroupId == null ? "" : channel.ScopeChatGroupId.Value.ToString()
            });
        }
        if (channel.Kind == NotificationChannelKind.FriendRequests) {
            string friendRequestsBody = count == 1 ? "1 person sent you a friend request." : $"{count} people sent you friend requests.";
            return new NotificationContent("Friend requests", friendRequestsBody, new() {
                ["type"] = "friendRequests",
                ["count"] = count.ToString()
            });
        }
        string offersBody = count == 1 ? "1 person wants to help with your request." : $"{count} people want to help with your request.";
        return new NotificationContent("Help is on the way", offersBody, new() {
            ["type"] = "helpOffers",
            ["count"] = count.ToString(),
            ["chatGroupId"] = channel.ScopeChatGroupId == null ? "" : channel.ScopeChatGroupId.Value.ToString()
        });
    }

    private static string LoadChatGroupName(Guid? chatGroupId) {
        if (chatGroupId == null)
            return "your group";
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId.Value);
        if (chatGroup == null || string.IsNullOrWhiteSpace(chatGroup.Name))
            return "your group";
        return chatGroup.Name;
    }

    // Methods - Channel Lifecycle

    private static void EnsureWaitingChannel(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        bool exists = dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.Waiting && field.RecipientUserAccountId == helperUserAccountId);
        if (exists)
            return;
        dbContext.NotificationChannels.Add(new() {
            Id = Guid.NewGuid(),
            RecipientUserAccountId = helperUserAccountId,
            Kind = NotificationChannelKind.Waiting,
            ScopeChatGroupId = null,
            LastSentCount = 0,
            IsLive = false
        });
        TrySaveChanges(dbContext);
    }

    private static void EnsureOffersChannel(Guid chatGroupId, Guid seekerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        bool exists = dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == chatGroupId);
        if (exists)
            return;
        dbContext.NotificationChannels.Add(new() {
            Id = Guid.NewGuid(),
            RecipientUserAccountId = seekerUserAccountId,
            Kind = NotificationChannelKind.Offers,
            ScopeChatGroupId = chatGroupId,
            LastSentCount = 0,
            IsLive = false
        });
        TrySaveChanges(dbContext);
    }

    private static void EnsureJoinRequestsChannel(Guid chatGroupId, Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        bool exists = dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == chatGroupId);
        if (exists)
            return;
        dbContext.NotificationChannels.Add(new() {
            Id = Guid.NewGuid(),
            RecipientUserAccountId = ownerUserAccountId,
            Kind = NotificationChannelKind.JoinRequests,
            ScopeChatGroupId = chatGroupId,
            LastSentCount = 0,
            IsLive = false
        });
        TrySaveChanges(dbContext);
    }

    private static void EnsureFriendRequestsChannel(Guid recipientUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        bool exists = dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.FriendRequests && field.RecipientUserAccountId == recipientUserAccountId);
        if (exists)
            return;
        dbContext.NotificationChannels.Add(new() {
            Id = Guid.NewGuid(),
            RecipientUserAccountId = recipientUserAccountId,
            Kind = NotificationChannelKind.FriendRequests,
            ScopeChatGroupId = null,
            LastSentCount = 0,
            IsLive = false
        });
        TrySaveChanges(dbContext);
    }

    private static void EnsureMessagesChannels(Guid chatGroupId, Guid senderUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> memberIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active && field.UserAccountId != senderUserAccountId)
            .Select(field => field.UserAccountId)];
        if (memberIds.Count == 0)
            return;
        List<Guid> existingRecipientIds = [.. dbContext.NotificationChannels
            .Where(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == chatGroupId)
            .Select(field => field.RecipientUserAccountId)];
        foreach (Guid memberId in memberIds)
            if (!existingRecipientIds.Contains(memberId))
                dbContext.NotificationChannels.Add(new() {
                    Id = Guid.NewGuid(),
                    RecipientUserAccountId = memberId,
                    Kind = NotificationChannelKind.Messages,
                    ScopeChatGroupId = chatGroupId,
                    LastSentCount = 0,
                    IsLive = false
                });
        TrySaveChanges(dbContext);
    }

    private static void TeardownChannels(Expression<Func<NotificationChannel, bool>> predicate) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<NotificationChannel> channels = [.. dbContext.NotificationChannels.Where(predicate)];
        foreach (NotificationChannel channel in channels) {
            if (channel.IsLive)
                SendDismissal(channel);
        }
        dbContext.NotificationChannels.Where(predicate).ExecuteDelete();
    }

    private static void MarkDirty(IQueryable<NotificationChannel> channels) {
        DateTime now = DateTime.UtcNow;
        DateTime quietDue = now.AddMilliseconds(QuietWindowMs);
        channels.ExecuteUpdate(setters => setters
            .SetProperty(field => field.LastEventAtUtc, now)
            .SetProperty(field => field.FirstDirtyAtUtc, field => field.FirstDirtyAtUtc == null ? now : field.FirstDirtyAtUtc)
            .SetProperty(field => field.DueAtUtc, quietDue));
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
        }
    }

    private sealed record NotificationContent(string Title, string Body, Dictionary<string, string> Data);
}
