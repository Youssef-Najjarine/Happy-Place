using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public class UserProfileSummaryResult {
    // Properties

    public string DisplayName { get; private set; }
    public string Username { get; private set; }
    public string AvatarColor { get; private set; }
    public string ProfilePhotoUrl { get; private set; }
    public bool IsAnonymous { get; private set; }

    // Methods

    public static UserProfileSummaryResult FromUserAccount(UserAccount userAccount) {
        return new() {
            DisplayName = userAccount.DisplayName,
            Username = userAccount.Username,
            AvatarColor = UserAccountRegistrar.GetAvatarColor(userAccount.Id),
            ProfilePhotoUrl = userAccount.ProfilePhotoUrl,
            IsAnonymous = userAccount.IsAnonymous
        };
    }
}
