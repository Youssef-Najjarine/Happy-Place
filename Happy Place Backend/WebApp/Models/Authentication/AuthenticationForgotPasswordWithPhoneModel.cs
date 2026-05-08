namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationForgotPasswordWithPhoneModel(string PhoneNumber) {
    // Methods
    public ResponseModel RequestReset() {
        try {
            UserAccountRegistrar.RequestPasswordResetWithPhone(this.PhoneNumber);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
