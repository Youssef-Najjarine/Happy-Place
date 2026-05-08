namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationVerifyForgotPasswordPhoneModel(string PhoneNumber, string VerificationCode) {
    // Methods
    public ResetPasswordTokenModel Verify() {
        string resetToken = UserAccountRegistrar.VerifyForgotPasswordPhone(this.PhoneNumber, this.VerificationCode);
        if (resetToken == null)
            return null;
        return new ResetPasswordTokenModel(resetToken);
    }
}
