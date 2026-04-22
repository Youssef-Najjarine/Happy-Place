using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationValidateTokenModel(string AuthToken) {
    // Methods
    public UserProfileResult Validate() {
        if (string.IsNullOrWhiteSpace(this.AuthToken))
            return null;

        UserAuthenticationToken token;
        try {
            token = UserAuthenticationToken.ValidateToken(this.AuthToken);
        }
        catch {
            return null;
        }

        if (token == null)
            return null;

        if (!Guid.TryParse(token.Identifier, out Guid userId))
            return null;

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.Id == userId);
        if (userAccount == null)
            return null;

        return UserProfileResult.FromUserAccount(userAccount);
    }
}
