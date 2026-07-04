using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class HelpAvailabilityManager {
    // Methods

    public static bool SetAvailability(string authToken, bool isAvailable) {
        Guid? helperUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (helperUserAccountId == null)
            return false;
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
        return true;
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
