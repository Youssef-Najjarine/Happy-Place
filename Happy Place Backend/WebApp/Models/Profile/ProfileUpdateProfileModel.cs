namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileUpdateProfileModel(string AuthToken, string Username, string DisplayName, string Bio) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Update() {
        try {
            return UserProfileManager.UpdateProfile(this.AuthToken, this.Username, this.DisplayName, this.Bio);
        }
        catch (ValidationErrorsException) {
            return null;
        }
    }
}
