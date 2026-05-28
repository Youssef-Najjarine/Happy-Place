namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileRequestEmailChangeModel(string AuthToken, string CurrentPassword, string EmailAddress) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ResponseModel RequestChange() {
        try {
            UserProfileManager.RequestEmailChange(this.AuthToken, this.CurrentPassword, this.EmailAddress);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
