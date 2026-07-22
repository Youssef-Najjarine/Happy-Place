using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class HelpAvailabilityManager {
    // Methods

    public static HelpAvailabilityStatusResult SetAvailability(string authToken, bool isAvailable) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return null;
        if (isAvailable && OwnsOpenRequest(helperUserAccountId.Value))
            return new HelpAvailabilityStatusResult("seeking", false);
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        HelpAvailability availability = dbContext.HelpAvailabilities.SingleOrDefault(field => field.HelperUserAccountId == helperUserAccountId.Value);
        if (availability == null) {
            dbContext.HelpAvailabilities.Add(new() { Id = Guid.NewGuid(), HelperUserAccountId = helperUserAccountId.Value, IsAvailable = isAvailable, LastSeenAtUtc = now });
            TrySaveChanges(dbContext);
        }
        else {
            availability.IsAvailable = isAvailable;
            availability.LastSeenAtUtc = now;
            TrySaveChanges(dbContext);
        }
        if (isAvailable) {
            NotificationDispatchManager.ActivateWaitingChannel(helperUserAccountId.Value);
        }
        else {
            WithdrawOutstandingOffers(helperUserAccountId.Value);
            NotificationDispatchManager.DeactivateWaitingChannel(helperUserAccountId.Value);
        }
        return new HelpAvailabilityStatusResult("ok", isAvailable);
    }

    public static void SetUnavailable(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        HelpAvailability availability = dbContext.HelpAvailabilities.SingleOrDefault(field => field.HelperUserAccountId == helperUserAccountId);
        if (availability != null && availability.IsAvailable) {
            availability.IsAvailable = false;
            availability.LastSeenAtUtc = DateTime.UtcNow;
            TrySaveChanges(dbContext);
        }
        WithdrawOutstandingOffers(helperUserAccountId);
        NotificationDispatchManager.DeactivateWaitingChannel(helperUserAccountId);
    }

    public static HelpAvailabilityStatusResult GetAvailability(string authToken) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        HelpAvailability availability = dbContext.HelpAvailabilities.SingleOrDefault(field => field.HelperUserAccountId == helperUserAccountId.Value);
        return new HelpAvailabilityStatusResult("ok", availability != null && availability.IsAvailable);
    }

    private static bool OwnsOpenRequest(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.OwnerUserAccountId == userAccountId && field.Status == ChatGroupStatus.Provisional);
    }

    private static void WithdrawOutstandingOffers(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> affectedChatGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Offered)
            .Select(field => field.ChatGroupId)
            .Distinct()];
        if (affectedChatGroupIds.Count == 0)
            return;
        dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Offered)
            .ExecuteDelete();
        foreach (Guid chatGroupId in affectedChatGroupIds)
            NotificationDispatchManager.MarkOffersDirty(chatGroupId);
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try { dbContext.SaveChanges(); }
        catch (DbUpdateException) { }
    }
}
