using HappyWorld.HappyPlace.Web.Models.Profile;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ProfileController : ControllerBase {
    // Methods

    [HttpPost]
    public IActionResult GetMyProfile(ProfileGetMyProfileModel model) {
        var result = model.Validate();
        if (result == null) return this.Unauthorized();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult GetPublicUserProfile(ProfileGetPublicUserProfileModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.GetProfile();
        if (result == null) return this.NotFound();
        return this.Ok(result);
    }
}
