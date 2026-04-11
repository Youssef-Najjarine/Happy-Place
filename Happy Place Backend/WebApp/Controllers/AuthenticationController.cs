using HappyWorld.HappyPlace.Web.Models.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthenticationController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }
        [HttpGet]
        [HttpPost]
        public IActionResult SignUpWithEmail(AuthenticationSignUpWithEmailModel model)
        {
            var response = model.SignUp();
            if (response.IsSuccessful)
            {
                return this.Ok();
            }

            return this.BadRequest(response.ErrorMessages);
        }

        [HttpPost]
        public IActionResult VerifyEmail(AuthenticationVerifyEmailModel model)
        {
            var response = model.VerifyEmail();
            if (response == null) 
                return this.BadRequest();
            return this.Ok(response);
        }
    }
}
