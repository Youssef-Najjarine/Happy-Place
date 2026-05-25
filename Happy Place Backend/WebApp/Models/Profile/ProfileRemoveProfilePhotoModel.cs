namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileRemoveProfilePhotoModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Remove() {
        return UserProfileManager.RemoveProfilePhoto(this.AuthToken);
    }
}
