using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RemoveProfileBackgroundPhotoTest {
    // Constants

    private const byte ProfilePhotoType = 1;
    private const byte BackgroundPhotoType = 2;

    // Tests - Happy Path

    [Fact]
    public void ExistingBackgroundRemovedReturnsOk() {
        string uniqueEmail = $"rmb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RemovedWhenNonExistentReturnsOk() {
        string uniqueEmail = $"rmb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RowIsDeletedFromDatabase() {
        string uniqueEmail = $"rmb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = authToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Assert.False(dbContext.UserProfilePhotos.Any(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType));
    }

    [Fact]
    public void BackgroundPhotoUrlIsNulledInResponse() {
        string uniqueEmail = $"rmb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = authToken });
        JsonElement backgroundPhotoUrlElement = response.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl");

        Assert.Equal(JsonValueKind.Null, backgroundPhotoUrlElement.ValueKind);
    }

    [Fact]
    public void RemoveDoesNotAffectProfilePhoto() {
        string uniqueEmail = $"rmb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] profileJpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        byte[] backgroundJpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", profileJpegBytes, "profile.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", backgroundJpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = authToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Assert.True(dbContext.UserProfilePhotos.Any(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType));
    }

    // Tests - Authorization

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeBackgroundPhoto", new { AuthToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
