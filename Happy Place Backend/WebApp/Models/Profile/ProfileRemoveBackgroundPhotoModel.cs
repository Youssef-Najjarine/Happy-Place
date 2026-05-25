namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileRemoveBackgroundPhotoModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Remove() {
        return UserProfileManager.RemoveBackgroundPhoto(this.AuthToken);
    }
}
