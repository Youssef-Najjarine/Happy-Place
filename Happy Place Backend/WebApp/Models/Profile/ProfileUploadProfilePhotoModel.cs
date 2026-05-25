using Microsoft.AspNetCore.Http;

namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileUploadProfilePhotoModel(string AuthToken, IFormFile Photo) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public MyProfileResult Upload() {
        try {
            return UserProfileManager.UploadProfilePhoto(this.AuthToken, ReadPhotoBytes());
        }
        catch (ValidationErrorsException) {
            return null;
        }
    }

    private byte[] ReadPhotoBytes() {
        if (this.Photo == null)
            return null;
        using var stream = this.Photo.OpenReadStream();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
