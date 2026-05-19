namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileGetPublicUserProfileModel(string AuthToken, string Username) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public PublicProfileResult GetProfile() {
        return UserProfileManager.GetPublicProfile(this.Username);
    }
}
