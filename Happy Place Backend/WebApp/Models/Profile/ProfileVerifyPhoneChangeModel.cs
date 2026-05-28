namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileVerifyPhoneChangeModel(string AuthToken, string PhoneNumber, string VerificationCode) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Verify() {
        return UserProfileManager.VerifyPhoneChange(this.AuthToken, this.PhoneNumber, this.VerificationCode);
    }
}
