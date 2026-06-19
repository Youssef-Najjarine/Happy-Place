using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public static class HelpParticipant {
    // Fields

    private static readonly int MaxGuestGroupMemberships = 2;

    // Methods

    public static Guid? ResolveUserAccountId(string authToken) {
        if (string.IsNullOrWhiteSpace(authToken))
            return null;
        UserAuthenticationToken token;
        try { token = UserAuthenticationToken.ValidateToken(authToken); }
        catch { return null; }
        if (token == null)
            return null;
        if (!Guid.TryParse(token.Identifier, out Guid userId))
            return null;
        return userId;
    }

    public static bool IsGuestAtGroupLimit(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount userAccount = dbContext.UserAccounts.SingleOrDefault(field => field.Id == userAccountId);
        if (userAccount == null || !userAccount.IsAnonymous)
            return false;
        int groupMembershipCount = dbContext.ChatGroupMembers.Count(field => field.UserAccountId == userAccountId);
        return groupMembershipCount >= MaxGuestGroupMemberships;
    }
}
