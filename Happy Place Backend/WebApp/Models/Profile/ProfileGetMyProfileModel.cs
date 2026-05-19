namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileGetMyProfileModel(string AuthToken) {
    // Methods

    public MyProfileResult Validate() {
        return UserProfileManager.GetMyProfile(this.AuthToken);
    }
}
