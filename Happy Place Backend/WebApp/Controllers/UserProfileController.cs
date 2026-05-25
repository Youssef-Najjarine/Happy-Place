using HappyWorld.HappyPlace.Web.Models.Profile;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyWorld.HappyPlace.Web.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserProfileController : ControllerBase {
    // Fields

    private const long MaxPhotoRequestBodyBytes = 52428800;

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

    [HttpPost]
    [RequestSizeLimit(MaxPhotoRequestBodyBytes)]
    public IActionResult UploadProfilePhoto([FromForm] ProfileUploadProfilePhotoModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        if (this.Request.Form.Files.Count > 1) return this.BadRequest();
        if (model.Photo != null && model.Photo.Length > MaxPhotoRequestBodyBytes) return this.StatusCode(StatusCodes.Status413PayloadTooLarge);
        var result = model.Upload();
        if (result == null) return this.BadRequest();
        return this.Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(MaxPhotoRequestBodyBytes)]
    public IActionResult UploadBackgroundPhoto([FromForm] ProfileUploadBackgroundPhotoModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        if (this.Request.Form.Files.Count > 1) return this.BadRequest();
        if (model.Photo != null && model.Photo.Length > MaxPhotoRequestBodyBytes) return this.StatusCode(StatusCodes.Status413PayloadTooLarge);
        var result = model.Upload();
        if (result == null) return this.BadRequest();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult RemoveProfilePhoto(ProfileRemoveProfilePhotoModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.Remove();
        return this.Ok(result);
    }

    [HttpPost]
    public IActionResult RemoveBackgroundPhoto(ProfileRemoveBackgroundPhotoModel model) {
        if (!model.IsAuthenticated()) return this.Unauthorized();
        var result = model.Remove();
        return this.Ok(result);
    }

    [HttpGet("/api/photo/{photoId:guid}")]
    public IActionResult GetPhoto(Guid photoId) {
        var photo = UserProfileManager.GetPhoto(photoId);
        if (photo == null) return this.NotFound();
        this.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return this.File(photo.ImageBytes, photo.ContentType);
    }
}
