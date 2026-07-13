using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record PublicProfileResult(string DisplayName, string Username, string AvatarColor, string ProfilePhotoUrl, string BackgroundPhotoUrl, string Bio, string FriendshipStatus, int FriendCount) {
    // Methods

    public static PublicProfileResult FromUserAccount(UserAccount userAccount, string friendshipStatus) {
        return new PublicProfileResult(userAccount.DisplayName, userAccount.Username, UserAccountRegistrar.GetAvatarColor(userAccount.Id), userAccount.ProfilePhotoUrl, userAccount.BackgroundPhotoUrl, userAccount.Bio, friendshipStatus, FriendshipManager.CountFriends(userAccount.Id));
    }
}
