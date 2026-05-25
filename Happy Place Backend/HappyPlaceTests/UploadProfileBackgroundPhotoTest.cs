using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UploadProfileBackgroundPhotoTest {
    // Constants

    private const byte ProfilePhotoType = 1;
    private const byte BackgroundPhotoType = 2;

    // Tests - Happy Path & Format Acceptance

    [Fact]
    public void ValidJpegUploadReturnsOk() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ValidPngUploadReturnsOk() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] pngBytes = TestImageGenerator.CreatePng(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", pngBytes, "bg.png", "image/png"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ValidWebpUploadReturnsOk() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] webpBytes = TestImageGenerator.CreateWebp(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", webpBytes, "bg.webp", "image/webp"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Image Processing

    [Fact]
    public void OutputDimensionsAre1200By400() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(3000, 1000);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType);

        Assert.Equal(1200, storedPhoto.WidthPixels);
        Assert.Equal(400, storedPhoto.HeightPixels);
    }

    // Tests - Database State

    [Fact]
    public void NewRowCreatedWithPhotoTypeBackground() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var storedPhoto = dbContext.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id);

        Assert.Equal(BackgroundPhotoType, storedPhoto.PhotoType);
    }

    [Fact]
    public void BackgroundPhotoUrlIsUpdatedInResponse() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg"));
        string backgroundPhotoUrl = response.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl").GetString();

        Assert.NotNull(backgroundPhotoUrl);
        Assert.StartsWith("/api/photo/", backgroundPhotoUrl);
        Assert.True(Guid.TryParse(backgroundPhotoUrl["/api/photo/".Length..], out _));
    }

    // Tests - Cleanup

    [Fact]
    public void ReUploadReplacesExistingBackgroundPhoto() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] firstJpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        byte[] secondJpegBytes = TestImageGenerator.CreateJpeg(2000, 800);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", firstJpegBytes, "first.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfterFirst = HappyPlaceDbContext.Create();
        var userAccount = dbContextAfterFirst.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Guid firstPhotoId = dbContextAfterFirst.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType).Id;

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", secondJpegBytes, "second.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfterSecond = HappyPlaceDbContext.Create();
        var allBackgroundPhotos = dbContextAfterSecond.UserProfilePhotos.Where(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType).ToList();

        Assert.Single(allBackgroundPhotos);
        Assert.NotEqual(firstPhotoId, allBackgroundPhotos[0].Id);
    }

    [Fact]
    public void ReUploadDoesNotAffectProfilePhoto() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] profileJpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        byte[] backgroundJpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", profileJpegBytes, "profile.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextBeforeBackground = HappyPlaceDbContext.Create();
        var userAccount = dbContextBeforeBackground.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Guid profilePhotoIdBefore = dbContextBeforeBackground.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType).Id;

        testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", backgroundJpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        using var dbContextAfter = HappyPlaceDbContext.Create();
        Guid profilePhotoIdAfter = dbContextAfter.UserProfilePhotos.Single(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType).Id;

        Assert.Equal(profilePhotoIdBefore, profilePhotoIdAfter);
    }

    // Tests - Authorization

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = "" }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = "not-a-real-token" }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - File Rejection

    [Fact]
    public void GifReturnsBadRequest() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] gifBytes = TestImageGenerator.CreateGif(1500, 600);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", gifBytes, "bg.gif", "image/gif"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyFileReturnsBadRequest() {
        string uniqueEmail = $"upb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", Array.Empty<byte>(), "bg.jpg", "image/jpeg"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Methods - Helpers

    private static string SignUpAndGetToken(TestingMockProvidersContainer container, string uniqueEmail) {
        container.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }
}
