namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationResendPhoneCodeModel(string PhoneNumber) {
    // Methods
    public void Resend() {
        UserAccountRegistrar.ResendPhoneVerificationCode(this.PhoneNumber);
    }
}
