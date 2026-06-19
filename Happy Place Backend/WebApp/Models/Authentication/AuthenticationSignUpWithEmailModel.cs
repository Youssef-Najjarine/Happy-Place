namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationSignUpWithEmailModel(string AuthToken, string Email, string Name, string Password) {
    public ResponseModel SignUp() {
        try {
            UserAccountRegistrar.RegisterWithEmailAddress(this.AuthToken, this.Email, this.Name, this.Password);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex) {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}
