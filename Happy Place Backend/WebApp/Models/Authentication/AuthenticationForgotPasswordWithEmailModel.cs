namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationForgotPasswordWithEmailModel(string Email) {
    // Methods
    public ResponseModel RequestReset() {
        try {
            UserAccountRegistrar.RequestPasswordResetWithEmail(this.Email);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
