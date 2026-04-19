namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationResendEmailCodeModel(string Email) {
    // Methods
    public void Resend() {
        UserAccountRegistrar.ResendEmailVerificationCode(this.Email);
    }
}
