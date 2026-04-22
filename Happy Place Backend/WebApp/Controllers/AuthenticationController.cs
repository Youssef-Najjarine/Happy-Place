using HappyWorld.HappyPlace;
using HappyWorld.HappyPlace.Web.Models.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthenticationController : ControllerBase {
    // Methods
    [HttpPost]
    public IActionResult SignUpWithEmail(AuthenticationSignUpWithEmailModel model) {
        var response = model.SignUp();
        if (response.IsSuccessful)
            return this.Ok();
        return this.BadRequest(response.ErrorMessages);
    }

    [HttpPost]
    public IActionResult VerifyEmail(AuthenticationVerifyEmailModel model) {
        var response = model.VerifyEmail();
        if (response == null)
            return this.BadRequest();
        return this.Ok(response);
    }

    [HttpPost]
    public IActionResult ResendEmailCode(AuthenticationResendEmailCodeModel model) {
        model.Resend();
        return this.Ok();
    }

    [HttpPost]
    public IActionResult SignInWithEmail(AuthenticationSignInWithEmailModel model) {
        SignInResult signInResult = model.SignIn();
        if (signInResult == null)
            return this.BadRequest();
        return this.Ok(signInResult);
    }

    [HttpPost]
    public IActionResult SignUpWithPhone(AuthenticationSignUpWithPhoneModel model) {
        var response = model.SignUp();
        if (response.IsSuccessful)
            return this.Ok();
        return this.BadRequest(response.ErrorMessages);
    }

    [HttpPost]
    public IActionResult VerifyPhone(AuthenticationVerifyPhoneModel model) {
        var response = model.VerifyPhone();
        if (response == null)
            return this.BadRequest();
        return this.Ok(response);
    }

    [HttpPost]
    public IActionResult ResendPhoneCode(AuthenticationResendPhoneCodeModel model) {
        model.Resend();
        return this.Ok();
    }

    [HttpPost]
    public IActionResult SignInWithPhone(AuthenticationSignInWithPhoneModel model) {
        SignInResult signInResult = model.SignIn();
        if (signInResult == null)
            return this.BadRequest();
        return this.Ok(signInResult);
    }

    [HttpPost]
    public IActionResult ValidateToken(AuthenticationValidateTokenModel model) {
        UserProfileResult userProfile = model.Validate();
        if (userProfile == null)
            return this.Unauthorized();
        return this.Ok(userProfile);
    }
}
