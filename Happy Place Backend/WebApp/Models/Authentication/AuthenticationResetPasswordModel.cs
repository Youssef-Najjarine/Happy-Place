namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationResetPasswordModel(string ResetToken, string NewPassword) {
    // Methods
    public ResponseModel Reset() {
        try {
            UserAccountRegistrar.ResetPassword(this.ResetToken, this.NewPassword);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
