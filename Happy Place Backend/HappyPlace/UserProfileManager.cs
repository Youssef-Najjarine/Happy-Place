using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public class UserProfileManager {
    // Methods

    public static MyProfileResult GetMyProfile(string authToken) {
        var userAccount = GetAuthenticatedUserAccount(authToken);
        if (userAccount == null)
            return null;
        return MyProfileResult.FromUserAccount(userAccount);
    }

    public static bool IsAuthenticated(string authToken) {
        return GetAuthenticatedUserAccount(authToken) != null;
    }

    public static PublicProfileResult GetPublicProfile(string username) {
        if (string.IsNullOrWhiteSpace(username))
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.Username == username);
        if (userAccount == null)
            return null;
        return PublicProfileResult.FromUserAccount(userAccount);
    }

    private static UserAccount GetAuthenticatedUserAccount(string authToken) {
        if (string.IsNullOrWhiteSpace(authToken))
            return null;
        UserAuthenticationToken token;
        try {
            token = UserAuthenticationToken.ValidateToken(authToken);
        }
        catch {
            return null;
        }
        if (token == null)
            return null;
        if (!Guid.TryParse(token.Identifier, out Guid userId))
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.SingleOrDefault(field => field.Id == userId);
    }
}
