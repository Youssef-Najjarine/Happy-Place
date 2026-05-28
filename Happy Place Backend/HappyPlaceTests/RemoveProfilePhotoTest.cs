using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RemoveProfilePhotoTest {
    // Constants

    private const byte ProfilePhotoType = 1;
    private const byte BackgroundPhotoType = 2;

    // Tests - Happy Path

    [Fact]
    public void ExistingPhotoRemovedReturnsOk() {
        string uniqueEmail = $"rmp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RemovedWhenNonExistentReturnsOk() {
        string uniqueEmail = $"rmp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RowIsDeletedFromDatabase() {
        string uniqueEmail = $"rmp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = authToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Assert.False(dbContext.UserProfilePhotos.Any(field => field.UserAccountId == userAccount.Id && field.PhotoType == ProfilePhotoType));
    }

    [Fact]
    public void ProfilePhotoUrlIsNulledInResponse() {
        string uniqueEmail = $"rmp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] jpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", jpegBytes, "photo.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = authToken });
        JsonElement profilePhotoUrlElement = response.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl");

        Assert.Equal(JsonValueKind.Null, profilePhotoUrlElement.ValueKind);
    }

    [Fact]
    public void RemoveDoesNotAffectBackgroundPhoto() {
        string uniqueEmail = $"rmp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = SignUpAndGetToken(testingMockProvidersContainer, uniqueEmail);
        byte[] profileJpegBytes = TestImageGenerator.CreateJpeg(800, 800);
        byte[] backgroundJpegBytes = TestImageGenerator.CreateJpeg(1500, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", profileJpegBytes, "profile.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadBackgroundPhoto", new Dictionary<string, string> { ["AuthToken"] = authToken }, ("Photo", backgroundJpegBytes, "bg.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = authToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Assert.True(dbContext.UserProfilePhotos.Any(field => field.UserAccountId == userAccount.Id && field.PhotoType == BackgroundPhotoType));
    }

    [Fact]
    public void RemoveDoesNotAffectOtherUsersPhotos() {
        string email1 = $"u1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"u2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string token1 = SignUpAndGetToken(testingMockProvidersContainer, email1);
        string token2 = SignUpAndGetToken(testingMockProvidersContainer, email2);
        byte[] jpeg1 = TestImageGenerator.CreateJpeg(800, 800);
        byte[] jpeg2 = TestImageGenerator.CreateJpeg(600, 600);
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = token1 }, ("Photo", jpeg1, "u1.jpg", "image/jpeg")).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.UploadMultipart("api/userProfile/uploadProfilePhoto", new Dictionary<string, string> { ["AuthToken"] = token2 }, ("Photo", jpeg2, "u2.jpg", "image/jpeg")).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = token1 }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var user2 = dbContext.UserAccounts.Single(field => field.EmailAddress == email2);
        Assert.True(dbContext.UserProfilePhotos.Any(field => field.UserAccountId == user2.Id && field.PhotoType == ProfilePhotoType));
    }

    // Tests - Authorization

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { AuthToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/removeProfilePhoto", new { });

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
