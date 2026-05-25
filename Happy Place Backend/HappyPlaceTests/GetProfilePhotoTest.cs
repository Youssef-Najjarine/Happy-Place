using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using SixLabors.ImageSharp;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GetProfilePhotoTest {
    // Tests - Happy Path

    [Fact]
    public void ValidPhotoIdReturnsOkWithBytes() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);
        byte[] responseBytes = ReadResponseBytes(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(responseBytes.Length > 0);
    }

    [Fact]
    public void ResponseContentMatchesStoredBytes() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);
        Guid photoId = ExtractPhotoIdFromUrl(photoUrl);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);
        byte[] responseBytes = ReadResponseBytes(response);
        using var dbContext = HappyPlaceDbContext.Create();
        byte[] storedBytes = dbContext.UserProfilePhotos.Single(field => field.Id == photoId).ImageBytes;

        Assert.Equal(storedBytes, responseBytes);
    }

    [Fact]
    public void ContentTypeIsJpeg() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);

        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void ContentLengthMatchesByteCount() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);
        byte[] responseBytes = ReadResponseBytes(response);

        Assert.Equal(responseBytes.Length, response.Content.Headers.ContentLength);
    }

    [Fact]
    public void CacheControlHeaderIsSet() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);

        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.Public);
        Assert.True(response.Headers.CacheControl.MaxAge?.TotalSeconds >= 86400);
    }

    [Fact]
    public void ResponseBytesDecodeAsValidJpeg() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);
        byte[] responseBytes = ReadResponseBytes(response);
        using var decodedImage = SixLabors.ImageSharp.Image.Load(responseBytes);

        Assert.Equal(400, decodedImage.Width);
        Assert.Equal(400, decodedImage.Height);
    }

    // Tests - Not Found

    [Fact]
    public void NonExistentGuidReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string nonExistentPhotoUrl = $"/api/photo/{Guid.NewGuid()}";

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(nonExistentPhotoUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void MalformedGuidReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get("/api/photo/not-a-real-guid");

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public void EmptyGuidReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get($"/api/photo/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Tests - Access

    [Fact]
    public void PublicAccessNoAuthRequired() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Photo Types

    [Fact]
    public void ProfilePhotoCanBeRetrieved() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string profilePhotoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(profilePhotoUrl);
        byte[] responseBytes = ReadResponseBytes(response);
        using var decodedImage = SixLabors.ImageSharp.Image.Load(responseBytes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(400, decodedImage.Width);
        Assert.Equal(400, decodedImage.Height);
    }

    [Fact]
    public void BackgroundPhotoCanBeRetrieved() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        HttpResponseMessage uploadResponse = testingMockProvidersContainer.WebClient.UploadMultipart("api/profile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg"));
        string backgroundPhotoUrl = uploadResponse.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(backgroundPhotoUrl);
        byte[] responseBytes = ReadResponseBytes(response);
        using var decodedImage = SixLabors.ImageSharp.Image.Load(responseBytes);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1200, decodedImage.Width);
        Assert.Equal(400, decodedImage.Height);
    }

    // Tests - Cascade Behavior

    [Fact]
    public void AfterRemoveEndpointPhotoNotFound() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        testingMockProvidersContainer.WebClient.PostJson("api/profile/removeProfilePhoto", new { AuthToken = authToken }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void AfterAccountDeletedPhotoNotFound() {
        string uniqueEmail = $"gph{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        string photoUrl = UploadProfilePhotoAndGetUrl(testingMockProvidersContainer, authToken);

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(photoUrl);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Methods - Helpers

    private static string SignUpAndGetToken(TestingMockProvidersContainer container, string uniqueEmail) {
        container.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    private static string UploadProfilePhotoAndGetUrl(TestingMockProvidersContainer container, string authToken) {
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        HttpResponseMessage uploadResponse = container.WebClient.UploadMultipart("api/profile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg"));
        uploadResponse.EnsureSuccessStatusCode();
        return uploadResponse.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl").GetString();
    }

    private static Guid ExtractPhotoIdFromUrl(string photoUrl) {
        return Guid.Parse(photoUrl["/api/photo/".Length..]);
    }

    private static byte[] ReadResponseBytes(HttpResponseMessage response) {
        using var contentStream = response.Content.ReadAsStream();
        using var memoryStream = new MemoryStream();
        contentStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
