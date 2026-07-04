using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HappyWorld.HappyPlace;

public static class NotificationDispatchManager {
    // Fields

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
            if (chatGroup == null)
                return;
            EnsureOffersChannel(chatGroupId, chatGroup.OwnerUserAccountId);
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
            if (wasLive) {
                SendDismissal(channel);
                sent = true;
            }
        }
        else if (count != channel.LastSentCount || !wasLive) {
            bool alerting = count > channel.LastSentCount;
            SendCountUpdate(channel, count, alerting);
            sent = true;
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
        return CountOffersForGroup(dbContext, channel.ScopeChatGroupId);
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

    // Methods - Sending

    private static void SendCountUpdate(NotificationChannel channel, int count, bool alerting) {
        string collapseId = BuildCollapseId(channel);
        NotificationContent content = BuildCountContent(channel, count);
        Dictionary<string, string> data = new(content.Data) {
            ["alerting"] = alerting ? "true" : "false"
        };
        SendToRecipientDevices(channel.RecipientUserAccountId, deviceToken => new PushMessage {
            Token = deviceToken,
            Title = content.Title,
            Body = content.Body,
            Data = new Dictionary<string, string>(data),
            CollapseId = collapseId,
            Alerting = alerting
        });
    }

    private static void SendDismissal(NotificationChannel channel) {
        string collapseId = BuildCollapseId(channel);
        SendToRecipientDevices(channel.RecipientUserAccountId, deviceToken => new PushMessage {
            Token = deviceToken,
            Data = new() { ["type"] = "dismiss", ["collapseId"] = collapseId },
            CollapseId = collapseId,
            IsDismiss = true
        });
    }

    private static void SendToRecipientDevices(Guid recipientUserAccountId, Func<string, PushMessage> buildMessage) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<DeviceToken> deviceTokens = [.. dbContext.DeviceTokens.Where(field => field.UserAccountId == recipientUserAccountId)];
        foreach (DeviceToken deviceToken in deviceTokens) {
            try {
                PushSender.Create().Send(buildMessage(deviceToken.Token));
            }
            catch (PushTokenInvalidException) {
                dbContext.DeviceTokens.Where(field => field.Id == deviceToken.Id).ExecuteDelete();
            }
            catch (Exception) {
            }
        }
    }

    private static string BuildCollapseId(NotificationChannel channel) {
        if (channel.Kind == NotificationChannelKind.Waiting)
            return "help-waiting";
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
        string offersBody = count == 1 ? "1 person wants to help with your request." : $"{count} people want to help with your request.";
        return new NotificationContent("Help is on the way", offersBody, new() {
            ["type"] = "helpOffers",
            ["count"] = count.ToString(),
            ["chatGroupId"] = channel.ScopeChatGroupId == null ? "" : channel.ScopeChatGroupId.Value.ToString()
        });
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
