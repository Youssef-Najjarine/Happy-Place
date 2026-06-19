namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationCreateGuestModel {
    // Methods
    public SignInResult CreateGuest() {
        return UserAccountRegistrar.CreateGuestAccount();
    }
}
