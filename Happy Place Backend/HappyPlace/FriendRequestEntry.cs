using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record FriendRequestEntry(string DisplayName, string Username, string AvatarColor, string ProfilePhotoUrl, DateTime RequestedAtUtc) {
    // Methods

    public static FriendRequestEntry FromUserAccount(UserAccount userAccount, DateTime requestedAtUtc) {
        return new FriendRequestEntry(userAccount.DisplayName, userAccount.Username, UserAccountRegistrar.GetAvatarColor(userAccount.Id), userAccount.ProfilePhotoUrl, requestedAtUtc);
    }
}
