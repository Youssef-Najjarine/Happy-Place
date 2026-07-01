using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class DeviceTokenManager {
    // Methods

    public static bool RegisterDevice(string authToken, string token, string platform) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return false;
        if (string.IsNullOrWhiteSpace(token))
            return true;
        UpsertDeviceToken(userAccountId.Value, token, platform);
        return true;
    }

    public static bool UnregisterDevice(string authToken, string token) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return false;
        if (string.IsNullOrWhiteSpace(token))
            return true;
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.DeviceTokens.Where(field => field.UserAccountId == userAccountId.Value && field.Token == token).ExecuteDelete();
        return true;
    }

    private static void UpsertDeviceToken(Guid userAccountId, string token, string platform) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        DeviceToken existingToken = dbContext.DeviceTokens.SingleOrDefault(field => field.Token == token);
        if (existingToken == null) {
            dbContext.DeviceTokens.Add(new() { Id = Guid.NewGuid(), UserAccountId = userAccountId, Token = token, Platform = platform, CreatedAtUtc = now, LastSeenAtUtc = now });
            TrySaveChanges(dbContext);
            return;
        }
        existingToken.UserAccountId = userAccountId;
        existingToken.Platform = platform;
        existingToken.LastSeenAtUtc = now;
        TrySaveChanges(dbContext);
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
        }
    }
}
