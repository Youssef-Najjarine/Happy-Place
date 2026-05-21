namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileDeleteAccountModel(string AuthToken, string Password) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ResponseModel Delete() {
        try {
            UserProfileManager.DeleteAccount(this.AuthToken, this.Password);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
