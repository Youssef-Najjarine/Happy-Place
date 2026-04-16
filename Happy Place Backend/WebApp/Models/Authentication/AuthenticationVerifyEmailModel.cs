namespace HappyWorld.HappyPlace.Web.Models.Authentication {
    public record AuthenticationVerifyEmailModel(string Email, string VerificationCode) {
        public LoginSuccessModel VerifyEmail() {
            bool isVerified = UserAccountRegistrar.VerifyEmailAddress(this.Email, this.VerificationCode);
            if (!isVerified) {
                return null;
            }

            UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(this.Email);
            return new LoginSuccessModel(authToken.ToAuthTokenString());
        }
    }
}
