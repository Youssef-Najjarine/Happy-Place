using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class HelpOfferManager {
    // Fields

    private static readonly int RequestFreshnessSeconds = 120;

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
        if (HelpParticipant.IsGuestAtGroupLimit(helperUserAccountId.Value))
            return HelpOfferResult.RegistrationRequired();
        UpsertOffer(chatGroupId, helperUserAccountId.Value, HelpOfferStatus.Offered);
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
        return HelpOfferResult.Declined();
    }

    public static List<OpenHelpRequest> GetOpenRequestsForHelper(string authToken) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return [];
        SweepStaleProvisionalGroups();
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> respondedChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId.Value)
            .Select(field => field.ChatGroupId)];
        List<ChatGroup> openGroups = [.. dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Provisional && field.OwnerUserAccountId != helperUserAccountId.Value && !respondedChatGroupIds.Contains(field.Id))
            .OrderBy(field => field.CreatedAtUtc)];
        return [.. openGroups.Select(field => new OpenHelpRequest(field.Id.ToString(), field.Name, field.CreatedAtUtc))];
    }

    public static HelpOfferStatusResult GetConnectionStatus(string authToken) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return HelpOfferStatusResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Offered)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.LastSeenAtUtc, now));
        HelpOffer connectedOffer = dbContext.HelpOffers.SingleOrDefault(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Connected);
        if (connectedOffer != null) {
            ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == connectedOffer.ChatGroupId);
            if (chatGroup != null)
                return HelpOfferStatusResult.Connected(chatGroup.Id, chatGroup.Name);
        }
        bool hasOpenOffer = dbContext.HelpOffers.Any(field => field.HelperUserAccountId == helperUserAccountId.Value && field.Status == HelpOfferStatus.Offered);
        if (hasOpenOffer)
            return HelpOfferStatusResult.Offered();
        return HelpOfferStatusResult.None();
    }

    private static void SweepStaleProvisionalGroups() {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime freshnessCutoff = DateTime.UtcNow.AddSeconds(-RequestFreshnessSeconds);
        dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Provisional && field.LastSeenAtUtc < freshnessCutoff)
            .ExecuteDelete();
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

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try { dbContext.SaveChanges(); }
        catch (DbUpdateException) { }
    }
}
