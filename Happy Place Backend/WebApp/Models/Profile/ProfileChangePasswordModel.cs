namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileChangePasswordModel(string AuthToken, string CurrentPassword, string NewPassword) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ResponseModel ChangePassword() {
        try {
            UserProfileManager.ChangePassword(this.AuthToken, this.CurrentPassword, this.NewPassword);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
