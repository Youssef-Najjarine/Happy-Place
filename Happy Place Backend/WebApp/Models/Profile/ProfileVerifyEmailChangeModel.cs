namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileVerifyEmailChangeModel(string AuthToken, string EmailAddress, string VerificationCode) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Verify() {
        return UserProfileManager.VerifyEmailChange(this.AuthToken, this.EmailAddress, this.VerificationCode);
    }
}
