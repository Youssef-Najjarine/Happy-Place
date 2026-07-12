using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class HelpOfferManager {
    // Fields

    private static readonly int OfferedRequestTtlDays = 7;

    // Methods

    public static HelpOfferResult CreateOffer(string authToken, Guid chatGroupId) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return HelpOfferResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Provisional)
            return HelpOfferResult.RequestClosed();
        if (chatGroup.OwnerUserAccountId == helperUserAccountId.Value)
            return HelpOfferResult.RequestClosed();
        UpsertOffer(chatGroupId, helperUserAccountId.Value, HelpOfferStatus.Offered);
        NotificationDispatchManager.MarkOffersDirty(chatGroupId);
        return HelpOfferResult.Offered();
    }

    public static HelpOfferResult DeclineOffer(string authToken, Guid chatGroupId) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return HelpOfferResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Provisional)
            return HelpOfferResult.RequestClosed();
        if (chatGroup.OwnerUserAccountId == helperUserAccountId.Value)
            return HelpOfferResult.RequestClosed();
        UpsertOffer(chatGroupId, helperUserAccountId.Value, HelpOfferStatus.Declined);
        NotificationDispatchManager.MarkOffersDirty(chatGroupId);
        return HelpOfferResult.Declined();
    }

    public static HelpOfferResult WithdrawOffer(string authToken, Guid chatGroupId) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return HelpOfferResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Offered)
            .ExecuteDelete();
        NotificationDispatchManager.MarkOffersDirty(chatGroupId);
        return HelpOfferResult.Withdrawn();
    }

    public static HelpJoinResult JoinGroup(string authToken, Guid chatGroupId) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return HelpJoinResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return HelpJoinResult.Unavailable();
        if (!chatGroup.IsPublic) {
            bool invited = dbContext.HelpOffers.Any(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == userAccountId.Value && field.Status == HelpOfferStatus.Connected);
            if (!invited)
                return HelpJoinResult.Unavailable();
        }
        bool alreadyMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value);
        if (alreadyMember) {
            ClaimOwnershipIfUnowned(dbContext, chatGroupId, userAccountId.Value);
            return HelpJoinResult.Joined(chatGroup.Id, chatGroup.Name);
        }
        dbContext.ChatGroupMembers.Add(new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = userAccountId.Value, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        TrySaveChanges(dbContext);
        bool groupStillActive = dbContext.ChatGroups.Any(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active);
        if (!groupStillActive) {
            dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value).ExecuteDelete();
            return HelpJoinResult.Unavailable();
        }
        ClaimOwnershipIfUnowned(dbContext, chatGroupId, userAccountId.Value);
        return HelpJoinResult.Joined(chatGroup.Id, chatGroup.Name);
    }

    public static HelpOfferResult DeclineInvite(string authToken, Guid chatGroupId) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return HelpOfferResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return HelpOfferResult.RequestClosed();
        if (chatGroup.OwnerUserAccountId == helperUserAccountId.Value)
            return HelpOfferResult.RequestClosed();
        bool alreadyMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == helperUserAccountId.Value);
        if (alreadyMember)
            return HelpOfferResult.RequestClosed();
        HelpOffer offer = dbContext.HelpOffers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId.Value);
        if (offer == null)
            return HelpOfferResult.RequestClosed();
        if (offer.Status == HelpOfferStatus.Declined)
            return HelpOfferResult.Declined();
        if (offer.Status != HelpOfferStatus.Connected)
            return HelpOfferResult.RequestClosed();
        offer.Status = HelpOfferStatus.Declined;
        TrySaveChanges(dbContext);
        return HelpOfferResult.Declined();
    }

    public static List<OpenHelpRequest> GetOpenRequestsForHelper(string authToken) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return [];
        SweepExpiredProvisionalGroups();
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> declinedChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Declined)
            .Select(field => field.ChatGroupId)];
        List<Guid> offeredChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Offered)
            .Select(field => field.ChatGroupId)];
        List<ChatGroup> openGroups = [.. dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Provisional && field.OwnerUserAccountId != helperUserAccountId.Value && !declinedChatGroupIds.Contains(field.Id))
            .OrderBy(field => field.CreatedAtUtc)];
        return [.. openGroups.Select(field => new OpenHelpRequest(field.Id.ToString(), field.Name, field.CreatedAtUtc, offeredChatGroupIds.Contains(field.Id) ? "offered" : "none"))];
    }

    public static List<StartedGroup> GetStartedGroupsForHelper(string authToken) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return [];
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> connectedChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Connected)
            .Select(field => field.ChatGroupId)];
        if (connectedChatGroupIds.Count == 0)
            return [];
        List<Guid> memberChatGroupIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == helperUserAccountId.Value)
            .Select(field => field.ChatGroupId)];
        List<ChatGroup> startedGroups = [.. dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Active && connectedChatGroupIds.Contains(field.Id) && !memberChatGroupIds.Contains(field.Id))
            .OrderBy(field => field.CreatedAtUtc)];
        return [.. startedGroups.Select(field => new StartedGroup(field.Id.ToString(), field.Name))];
    }

    private static void SweepExpiredProvisionalGroups() {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime offerExpiryCutoff = DateTime.UtcNow.AddDays(-OfferedRequestTtlDays);
        List<Guid> offeredChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.Status == HelpOfferStatus.Offered && field.CreatedAtUtc < offerExpiryCutoff)
            .Select(field => field.ChatGroupId)
            .Distinct()];
        if (offeredChatGroupIds.Count == 0)
            return;
        List<Guid> expiredChatGroupIds = [.. dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Provisional && offeredChatGroupIds.Contains(field.Id))
            .Select(field => field.Id)];
        if (expiredChatGroupIds.Count == 0)
            return;
        foreach (Guid expiredChatGroupId in expiredChatGroupIds)
            NotificationDispatchManager.RemoveOffersChannel(expiredChatGroupId);
        dbContext.ChatGroups.Where(field => expiredChatGroupIds.Contains(field.Id)).ExecuteDelete();
        NotificationDispatchManager.MarkWaitingDirtyForAllHelpers();
    }

    private static void UpsertOffer(Guid chatGroupId, Guid helperUserAccountId, HelpOfferStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        HelpOffer existingOffer = dbContext.HelpOffers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId);
        if (existingOffer == null) {
            dbContext.HelpOffers.Add(new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, HelperUserAccountId = helperUserAccountId, Status = status, CreatedAtUtc = now, LastSeenAtUtc = now });
            TrySaveChanges(dbContext);
            return;
        }
        if (existingOffer.Status != HelpOfferStatus.Offered && existingOffer.Status != HelpOfferStatus.Declined)
            return;
        existingOffer.Status = status;
        existingOffer.LastSeenAtUtc = now;
        TrySaveChanges(dbContext);
    }

    private static void ClaimOwnershipIfUnowned(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid userAccountId) {
        int claimed = dbContext.ChatGroups
            .Where(field => field.Id == chatGroupId && field.OwnerUserAccountId == null)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.OwnerUserAccountId, (Guid?)userAccountId));
        if (claimed != 1)
            return;
        dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.MemberRole, ChatGroupMemberRole.Owner));
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try { dbContext.SaveChanges(); }
        catch (DbUpdateException) { }
    }
}
