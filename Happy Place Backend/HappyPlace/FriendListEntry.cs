using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record FriendListEntry(string DisplayName, string Username, string AvatarColor, string ProfilePhotoUrl, string FriendshipStatus) {
    // Methods

    public static FriendListEntry FromUserAccount(UserAccount userAccount, string friendshipStatus) {
        return new FriendListEntry(userAccount.DisplayName, userAccount.Username, UserAccountRegistrar.GetAvatarColor(userAccount.Id), userAccount.ProfilePhotoUrl, friendshipStatus);
    }
}
