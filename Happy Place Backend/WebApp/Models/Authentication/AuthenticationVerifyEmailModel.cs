using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationVerifyEmailModel(string Email, string VerificationCode) {
    // Methods
    public SignInSuccessModel VerifyEmail() {
        UserAccount userAccount = UserAccountRegistrar.VerifyEmailAddress(this.Email, this.VerificationCode);
        if (userAccount == null)
            return null;
        UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(userAccount.Id.ToString());
        return new SignInSuccessModel(authToken.ToAuthTokenString());
    }
}
