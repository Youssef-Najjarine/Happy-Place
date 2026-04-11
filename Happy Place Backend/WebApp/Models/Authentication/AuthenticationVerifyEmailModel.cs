using Microsoft.AspNetCore.Identity;
using System.Runtime.InteropServices;

namespace HappyWorld.HappyPlace.Web.Models.Authentication
{
    public record AuthenticationVerifyEmailModel(string Email, string VerificationCode)
    {
        public LoginSuccessModel VerifyEmail()
        {
            bool isVerified = UserAccountRegistrar.VerifyEmailAddress(this.Email, this.VerificationCode);
            if (!isVerified)
            {
                return null;
            }
            // create userAuthenticator class that logs user in and generates auth token
            UserAuthenticationToken authToken = UserAuthenticationToken.GenerateForUser(this.Email);
            return new LoginSuccessModel(authToken.ToAuthTokenString());
        }
    }
}
