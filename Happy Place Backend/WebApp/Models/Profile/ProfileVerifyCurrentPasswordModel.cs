using Microsoft.AspNetCore.Identity;

namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileVerifyCurrentPasswordModel(string AuthToken, string Password) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public PasswordVerificationResult Verify() {
        return UserProfileManager.VerifyCurrentPassword(this.AuthToken, this.Password);
    }
}
