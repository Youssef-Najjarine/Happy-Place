using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record ChatGroupDirectContact(string DisplayName, string Username, string ProfilePhotoUrl, string AvatarColor, string Initial) {
    // Methods

    public static ChatGroupDirectContact FromUserAccount(UserAccount userAccount) {
        return new ChatGroupDirectContact(userAccount.DisplayName, userAccount.Username, userAccount.ProfilePhotoUrl, UserAccountRegistrar.GetAvatarColor(userAccount.Id), BuildInitial(userAccount.DisplayName));
    }

    private static string BuildInitial(string displayName) {
        string trimmedDisplayName = (displayName ?? "").Trim();
        if (trimmedDisplayName.Length == 0)
            return "?";
        return trimmedDisplayName[..1].ToUpperInvariant();
    }
}
