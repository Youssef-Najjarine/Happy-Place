namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationSignInWithPhoneModel(string PhoneNumber, string Password) {
    // Methods
    public SignInResult SignIn() {
        return UserAccountRegistrar.SignInWithPhoneNumber(this.PhoneNumber, this.Password);
    }
}
