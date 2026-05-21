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

    [HttpPost]
    public IActionResult UpdateProfile(ProfileUpdateProfileModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.Update();
        if (result == null) return this.BadRequest();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult ChangePassword(ProfileChangePasswordModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var response = model.ChangePassword();
        if (response.IsSuccessful) return this.Ok();
        return this.BadRequest(response.ErrorMessages);
    }

    [HttpPost]
    public IActionResult VerifyCurrentPassword(ProfileVerifyCurrentPasswordModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.Verify();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult CheckUsernameAvailability(ProfileCheckUsernameAvailabilityModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.Check();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult DeleteAccount(ProfileDeleteAccountModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var response = model.Delete();
        if (response.IsSuccessful) return this.Ok();
        return this.BadRequest(response.ErrorMessages);
    }
}
