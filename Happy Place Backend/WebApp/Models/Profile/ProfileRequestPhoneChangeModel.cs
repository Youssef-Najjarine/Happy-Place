namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileRequestPhoneChangeModel(string AuthToken, string CurrentPassword, string PhoneNumber) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ResponseModel RequestChange() {
        try {
            UserProfileManager.RequestPhoneChange(this.AuthToken, this.CurrentPassword, this.PhoneNumber);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
