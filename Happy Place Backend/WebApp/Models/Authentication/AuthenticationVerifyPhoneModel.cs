namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationVerifyPhoneModel(string PhoneNumber, string VerificationCode) {
    // Methods
    public LoginSuccessModel VerifyPhone() {
        bool isVerified = UserAccountRegistrar.VerifyPhoneNumber(this.PhoneNumber, this.VerificationCode);
        if (!isVerified) {
            return null;
        }
        UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(this.PhoneNumber);
        return new LoginSuccessModel(authToken.ToAuthTokenString());
    }
}