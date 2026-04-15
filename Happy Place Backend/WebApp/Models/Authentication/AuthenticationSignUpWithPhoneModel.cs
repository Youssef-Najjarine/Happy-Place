namespace HappyWorld.HappyPlace.Web.Models.Authentication;

public record AuthenticationSignUpWithPhoneModel(string PhoneNumber, string Name, string Password)
{
    // Methods
    public ResponseModel SignUp()
    {
        try
        {
            UserAccountRegistrar.RegisterWithPhoneNumber(this.PhoneNumber, this.Name, this.Password);
            return ResponseModel.AsSuccess();
        }
        catch (ValidationErrorsException ex)
        {
            return ResponseModel.WithErrors(ex.ValidationErrors);
        }
    }
}