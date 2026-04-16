using HappyWorld.HappyPlace.Web.Models.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthenticationController : ControllerBase {
    // Fields
    private readonly ILogger<AuthenticationController> _logger;

    // Constructors
    public AuthenticationController(ILogger<AuthenticationController> logger) {
        _logger = logger;
    }

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
}
