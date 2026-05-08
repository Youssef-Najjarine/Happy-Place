namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationVerifyForgotPasswordEmailModel(string Email, string VerificationCode) {
    // Methods
    public ResetPasswordTokenModel Verify() {
        string resetToken = UserAccountRegistrar.VerifyForgotPasswordEmail(this.Email, this.VerificationCode);
        if (resetToken == null)
            return null;
        return new ResetPasswordTokenModel(resetToken);
    }
}
