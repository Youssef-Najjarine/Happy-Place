using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public class UserAccountResolver {
    // Methods

    public static UserAccount Resolve(string authToken) {
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
