using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class HelpRequestManager {
    // Fields

    private static readonly int MaxTopicLength = 100;
    private static readonly int OfferFreshnessSeconds = 90;
    private static readonly string GeneratedChatGroupName = "Support Chat";

    // Methods

    public static HelpRequestResult CreateRequest(string authToken, string topic) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return HelpRequestResult.None();
        string normalizedTopic = NormalizeTopic(topic);
        HelpRequestResult existingRequest = FindExistingProvisionalRequest(userAccountId.Value);
        if (existingRequest != null)
            return existingRequest;
        if (HelpParticipant.IsGuestAtGroupLimit(userAccountId.Value))
            return HelpRequestResult.RegistrationRequired();
        return CreateProvisionalGroup(userAccountId.Value, normalizedTopic);
    }

    public static HelpConnectResult Connect(string authToken, Guid chatGroupId) {
        Guid? seekerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (seekerUserAccountId == null)
            return HelpConnectResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.OwnerUserAccountId != seekerUserAccountId.Value)
            return HelpConnectResult.None();
        if (chatGroup.Status == ChatGroupStatus.Active)
            return HelpConnectResult.Connected(chatGroup.Id, chatGroup.Name);
        return ConnectLongestWaitingOffer(chatGroup.Id, chatGroup.Name);
    }

    public static HelpRequestStatusResult GetRequestStatus(string authToken, Guid chatGroupId) {
        Guid? seekerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (seekerUserAccountId == null)
            return HelpRequestStatusResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.OwnerUserAccountId != seekerUserAccountId.Value)
            return HelpRequestStatusResult.None();
        if (chatGroup.Status == ChatGroupStatus.Active)
            return HelpRequestStatusResult.Connected(chatGroup.Id, chatGroup.Name);
        chatGroup.LastSeenAtUtc = DateTime.UtcNow;
        dbContext.SaveChanges();
        DateTime freshnessCutoff = DateTime.UtcNow.AddSeconds(-OfferFreshnessSeconds);
        int readyHelperCount = dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered && field.LastSeenAtUtc >= freshnessCutoff);
        return HelpRequestStatusResult.Waiting(chatGroup.Id, chatGroup.Name, readyHelperCount);
    }

    public static HelpCancelResult CancelRequest(string authToken, Guid chatGroupId) {
        Guid? seekerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (seekerUserAccountId == null)
            return HelpCancelResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.OwnerUserAccountId != seekerUserAccountId.Value)
            return HelpCancelResult.None();
        if (chatGroup.Status != ChatGroupStatus.Provisional)
            return HelpCancelResult.None();
        dbContext.ChatGroups.Where(field => field.Id == chatGroupId).ExecuteDelete();
        return HelpCancelResult.Cancelled();
    }

    private static HelpConnectResult ConnectLongestWaitingOffer(Guid chatGroupId, string chatGroupName) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime freshnessCutoff = DateTime.UtcNow.AddSeconds(-OfferFreshnessSeconds);
        List<HelpOffer> offeredOffers = [.. dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered && field.LastSeenAtUtc >= freshnessCutoff)
            .OrderBy(field => field.CreatedAtUtc)];
        foreach (HelpOffer offer in offeredOffers) {
            if (TryClaimAndActivate(chatGroupId, offer.Id, offer.HelperUserAccountId))
                return HelpConnectResult.Connected(chatGroupId, chatGroupName);
        }
        if (IsGroupActive(chatGroupId))
            return HelpConnectResult.Connected(chatGroupId, chatGroupName);
        return HelpConnectResult.NoOffers(chatGroupId, chatGroupName);
    }

    private static bool TryClaimAndActivate(Guid chatGroupId, Guid offerId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        try {
            using var transaction = dbContext.Database.BeginTransaction();
            int groupActivatedCount = dbContext.ChatGroups
                .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Provisional)
                .ExecuteUpdate(setters => setters
                    .SetProperty(field => field.Status, ChatGroupStatus.Active));
            if (groupActivatedCount != 1) {
                transaction.Rollback();
                return false;
            }
            int claimedCount = dbContext.HelpOffers
                .Where(field => field.Id == offerId && field.Status == HelpOfferStatus.Offered)
                .ExecuteUpdate(setters => setters
                    .SetProperty(field => field.Status, HelpOfferStatus.Connected)
                    .SetProperty(field => field.LastSeenAtUtc, now));
            if (claimedCount != 1) {
                transaction.Rollback();
                return false;
            }
            dbContext.HelpOffers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered)
                .ExecuteUpdate(setters => setters
                    .SetProperty(field => field.Status, HelpOfferStatus.Released)
                    .SetProperty(field => field.LastSeenAtUtc, now));
            dbContext.HelpOffers
                .Where(field => field.HelperUserAccountId == helperUserAccountId && field.ChatGroupId != chatGroupId && field.Status == HelpOfferStatus.Offered)
                .ExecuteUpdate(setters => setters
                    .SetProperty(field => field.Status, HelpOfferStatus.Released)
                    .SetProperty(field => field.LastSeenAtUtc, now));
            dbContext.ChatGroupMembers.Add(new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = helperUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
            dbContext.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    private static bool IsGroupActive(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        return chatGroup != null && chatGroup.Status == ChatGroupStatus.Active;
    }

    private static HelpRequestResult FindExistingProvisionalRequest(Guid seekerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup provisionalGroup = dbContext.ChatGroups
            .Where(field => field.OwnerUserAccountId == seekerUserAccountId && field.Status == ChatGroupStatus.Provisional)
            .OrderByDescending(field => field.CreatedAtUtc)
            .FirstOrDefault();
        if (provisionalGroup == null)
            return null;
        provisionalGroup.LastSeenAtUtc = DateTime.UtcNow;
        dbContext.SaveChanges();
        return HelpRequestResult.Waiting(provisionalGroup.Id, provisionalGroup.Name);
    }

    private static HelpRequestResult CreateProvisionalGroup(Guid seekerUserAccountId, string topic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        string chatGroupName = BuildChatGroupName(topic);
        dbContext.ChatGroups.Add(new() { Id = chatGroupId, Name = chatGroupName, OwnerUserAccountId = seekerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Provisional, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = seekerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        try {
            dbContext.SaveChanges();
            return HelpRequestResult.Waiting(chatGroupId, chatGroupName);
        }
        catch (DbUpdateException) {
            HelpRequestResult existingRequest = FindExistingProvisionalRequest(seekerUserAccountId);
            if (existingRequest != null)
                return existingRequest;
            throw;
        }
    }

    private static string NormalizeTopic(string topic) {
        string trimmedTopic = (topic ?? "").Trim();
        if (trimmedTopic.Length == 0)
            return null;
        if (trimmedTopic.Length > MaxTopicLength)
            return trimmedTopic[..MaxTopicLength];
        return trimmedTopic;
    }

    private static string BuildChatGroupName(string topic) {
        if (string.IsNullOrWhiteSpace(topic))
            return GeneratedChatGroupName;
        return topic;
    }
}
