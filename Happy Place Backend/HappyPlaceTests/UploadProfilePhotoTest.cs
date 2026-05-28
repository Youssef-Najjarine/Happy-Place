using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using SixLabors.ImageSharp;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UploadProfilePhotoTest {
    // Constants

    private const byte ProfilePhotoType = 1;
    private const byte BackgroundPhotoType = 2;

    // Tests - Happy Path & Format Acceptance

    [Fact]
    public void ValidJpegUploadReturnsOk() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ValidPngUploadReturnsOk() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] pngBytes = TestImageGenerator.CreatePng(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", pngBytes, "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ValidWebpUploadReturnsOk() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] webpBytes = TestImageGenerator.CreateWebp(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", webpBytes, "photo.webp", "image/webp"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ResponseContainsExactlyMyProfileResultProperties() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));
        var data = response.ReadContentAsJsonDocument();
        string[] actualProperties = [.. data.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        string[] expectedProperties = ["avatarColor", "backgroundPhotoUrl", "bio", "displayName", "emailAddress", "phoneNumber", "profilePhotoUrl", "username"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void ProfilePhotoUrlIsUpdatedInResponse() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));
        string profilePhotoUrl = response.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl").GetString();

        Assert.NotNull(profilePhotoUrl);
        Assert.StartsWith("/api/photo/", profilePhotoUrl);
        Assert.True(Guid.TryParse(profilePhotoUrl["/api/photo/".Length..], out _));
    }

    // Tests - Image Processing

    [Fact]
    public void OutputDimensionsAre400By400() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType);

        Assert.Equal(400, storedPhoto.WidthPixels);
        Assert.Equal(400, storedPhoto.HeightPixels);
    }

    [Fact]
    public void OutputContentTypeIsJpeg() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] pngBytes = TestImageGenerator.CreatePng(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", pngBytes, "photo.png", "image/png")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType);

        Assert.Equal("image/jpeg", storedPhoto.ContentType);
    }

    [Fact]
    public void LargeImageIsResizedDown() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(3000, 3000);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType);

        Assert.Equal(400, storedPhoto.WidthPixels);
        Assert.Equal(400, storedPhoto.HeightPixels);
        Assert.True(storedPhoto.FileSizeBytes < jpegBytes.Length);
    }

    [Fact]
    public void ExifGpsMetadataIsStripped() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytesWithGps = TestImageGenerator.CreateJpegWithExifGpsData(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytesWithGps, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType);
        using var decodedImage = SixLabors.ImageSharp.Image.Load(storedPhoto.ImageBytes);

        Assert.True(decodedImage.Metadata.ExifProfile == null || !decodedImage.Metadata.ExifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLatitude, out _));
    }

    [Fact]
    public void PngWithTransparencyIsFlattenedToWhite() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] pngBytes = TestImageGenerator.CreatePngWithTransparency(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", pngBytes, "photo.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType);
        Assert.Equal("image/jpeg", storedPhoto.ContentType);
    }

    // Tests - File Rejection (Format)

    [Fact]
    public void GifReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] gifBytes = TestImageGenerator.CreateGif(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", gifBytes, "photo.gif", "image/gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void BmpReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] bmpBytes = TestImageGenerator.CreateBmp(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", bmpBytes, "photo.bmp", "image/bmp"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void TiffReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] tiffBytes = TestImageGenerator.CreateTiff(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", tiffBytes, "photo.tiff", "image/tiff"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void SvgReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] svgBytes = TestImageGenerator.CreateSvg();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", svgBytes, "photo.svg", "image/svg+xml"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PlainTextWithJpgExtensionReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] plainTextBytes = TestImageGenerator.CreatePlainText();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", plainTextBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void BogusBytesWithJpegMagicHeaderReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] bogusBytes = TestImageGenerator.CreateBogusBytesWithJpegMagicHeader();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", bogusBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - File Rejection (Size & Dimensions)

    [Fact]
    public void EmptyFileReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", Array.Empty<byte>(), "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void FileOver50MbReturnsPayloadTooLarge() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] oversizedBytes = TestImageGenerator.CreateOversizedDummyBytes(52_428_801);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", oversizedBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public void ImageBelow100PixelsReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] tinyJpegBytes = TestImageGenerator.CreateJpeg(99, 99);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", tinyJpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ImageAbove8000PixelsReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] tooWideJpegBytes = TestImageGenerator.CreateJpeg(8001, 100);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", tooWideJpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - File Rejection (Multipart Issues)

    [Fact]
    public void MissingFilePartReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void MultipleFilesReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes1 = TestImageGenerator.CreateJpeg(800, 800);
        byte[] jpegBytes2 = TestImageGenerator.CreateJpeg(600, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes1, "photo1.jpg", "image/jpeg"), ("Photo", jpegBytes2, "photo2.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WrongFieldNameReturnsBadRequest() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("WrongFieldName", jpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Authorization

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = "" }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = "not-a-real-token" }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", [], ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Database State & Cleanup

    [Fact]
    public void NewRowCreatedWithPhotoTypeProfile() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id);

        Assert.Equal(ProfilePhotoType, storedPhoto.PhotoType);
        Assert.True(storedPhoto.FileSizeBytes > 0);
        Assert.True(storedPhoto.UploadedAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void ReUploadReplacesExistingProfilePhoto() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] firstJpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        byte[] secondJpegBytes = TestImageGenerator.CreateJpeg(600, 600);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", firstJpegBytes, "first.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfterFirst = HappyPlaceDbContext.Create();
        var userAccount = dbContextAfterFirst.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Guid firstPhotoId = dbContextAfterFirst.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType).Id;

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", secondJpegBytes, "second.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfterSecond = HappyPlaceDbContext.Create();
        var allPhotosForUser = dbContextAfterSecond.UserProfilePhotos.Where(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType).ToList();

        Assert.Single(allPhotosForUser);
        Assert.NotEqual(firstPhotoId, allPhotosForUser[0].Id);
    }

    [Fact]
    public void ReUploadDoesNotAffectBackgroundPhoto() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] backgroundJpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        byte[] profileJpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", backgroundJpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextBeforeProfile = HappyPlaceDbContext.Create();
        var userAccount = dbContextBeforeProfile.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Guid backgroundPhotoIdBefore = dbContextBeforeProfile.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType).Id;

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", profileJpegBytes, "profile.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfter = HappyPlaceDbContext.Create();
        Guid backgroundPhotoIdAfter = dbContextAfter.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType).Id;

        Assert.Equal(backgroundPhotoIdBefore, backgroundPhotoIdAfter);
    }

    [Fact]
    public void UploadDoesNotAffectOtherUsersPhotos() {
        string email1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"user2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string token1 = SignUpAndGetToken(testingMockProvidersContainer, email1);
        byte[] user1Jpeg = TestImageGenerator.CreateJpeg(800, 800);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = token1 }, ("Photo", user1Jpeg, "u1.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        string token2 = SignUpAndGetToken(testingMockProvidersContainer, email2);
        byte[] user2Jpeg = TestImageGenerator.CreateJpeg(600, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = token2 }, ("Photo", user2Jpeg, "u2.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var user1 = dbContext.UserAccounts.Single(field => field.EmailAddress == email1);
        var user2 = dbContext.UserAccounts.Single(field => field.EmailAddress == email2);
        Assert.Single(dbContext.UserProfilePhotos.Where(field => field.UserAccountId == user1.Id && field.PhotoType == ProfilePhotoType));
        Assert.Single(dbContext.UserProfilePhotos.Where(field => field.UserAccountId == user2.Id && field.PhotoType == ProfilePhotoType));
    }

    // Tests - Cascade Behavior

    [Fact]
    public void DeletingAccountCascadesToProfilePhoto() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        using (var dbContextBefore = HappyPlaceDbContext.Create()) {
            var userId = dbContextBefore.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Id;
            Assert.True(dbContextBefore.UserProfilePhotos.Any(field => field.UserAccountId == userId));
        }

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        Assert.False(dbContextAfter.UserAccounts.Any(field => field.EmailAddress == uniqueEmail));
        Assert.False(dbContextAfter.UserProfilePhotos.Any());
    }

    // Tests - Stored Metadata

    [Fact]
    public void UploadedFileSizeBytesIsAccurate() {
        string uniqueEmail = $"upp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id);

        Assert.Equal(storedPhoto.ImageBytes.Length, storedPhoto.FileSizeBytes);
    }

    // Methods - Helpers

    private static string SignUpAndGetToken(TestingMockProvidersContainer container, string uniqueEmail) {
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }
}
