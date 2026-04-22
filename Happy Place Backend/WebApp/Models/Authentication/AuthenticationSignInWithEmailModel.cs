namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationSignInWithEmailModel(string Email, string Password) {
    // Methods
    public SignInResult SignIn() {
        return UserAccountRegistrar.SignInWithEmailAddress(this.Email, this.Password);
    }
}
