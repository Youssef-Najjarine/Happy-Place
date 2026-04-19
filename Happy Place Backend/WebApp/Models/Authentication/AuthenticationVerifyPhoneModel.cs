using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationVerifyPhoneModel(string PhoneNumber, string VerificationCode) {
    // Methods
    public LoginSuccessModel VerifyPhone() {
        UserAccount userAccount = UserAccountRegistrar.VerifyPhoneNumber(this.PhoneNumber, this.VerificationCode);
        if (userAccount == null)
            return null;
        UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(userAccount.Id.ToString());
        return new LoginSuccessModel(authToken.ToAuthTokenString());
    }
}
