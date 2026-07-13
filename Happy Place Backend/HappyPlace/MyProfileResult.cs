using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record MyProfileResult(string DisplayName, string Username, string AvatarColor, string ProfilePhotoUrl, string BackgroundPhotoUrl, string Bio, string EmailAddress, string PhoneNumber, int FriendCount) {
    // Methods

    public static MyProfileResult FromUserAccount(UserAccount userAccount) {
        return new MyProfileResult(userAccount.DisplayName, userAccount.Username, UserAccountRegistrar.GetAvatarColor(userAccount.Id), userAccount.ProfilePhotoUrl, userAccount.BackgroundPhotoUrl, userAccount.Bio, userAccount.EmailAddress, userAccount.PhoneNumber, FriendshipManager.CountFriends(userAccount.Id));
    }
}
