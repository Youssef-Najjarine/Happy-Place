using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GetPublicUserProfileTest {
    // Tests - Happy Path

    [Fact]
    public void ReturnsOkForExistingUser() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ReturnsExpectedPublicFields() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);
        string expectedBio = "Here to listen and support.";
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var targetUser = dbContext.UserAccounts.Single(field => field.Username == targetUsername);
            targetUser.Bio = expectedBio;
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = response.ReadContentAsJsonDocument();

        Assert.Equal("Target Person", profileData.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(targetUsername, profileData.RootElement.GetProperty("username").GetString());
        Assert.Equal(expectedBio, profileData.RootElement.GetProperty("bio").GetString());
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", profileData.RootElement.GetProperty("avatarColor").GetString());
    }

    // Tests - Not Found

    [Fact]
    public void NonexistentUsernameReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = "nonexistentuser999999" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void EmptyUsernameReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = "" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void MissingUsernameFieldReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void DeletedTargetUserReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var targetUser = dbContext.UserAccounts.Single(field => field.Username == targetUsername);
            dbContext.UserAccounts.Remove(targetUser);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Tests - Response Security

    [Fact]
    public void PublicProfileContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = response.ReadContentAsJsonDocument();
        List<string> actualProperties = [.. profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["avatarColor", "backgroundPhotoUrl", "bio", "displayName", "friendCount", "friendshipStatus", "profilePhotoUrl", "username"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void PublicProfileDoesNotContainUserId() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = response.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("userId", out _));
        Assert.False(profileData.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public void PublicProfileDoesNotContainEmailOrPhone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = response.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("emailAddress", out _));
        Assert.False(profileData.RootElement.TryGetProperty("phoneNumber", out _));
        Assert.False(profileData.RootElement.TryGetProperty("hashedPassword", out _));
    }

    [Fact]
    public void ViewingOwnProfileViaGetPublicUserProfileReturnsPublicOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownToken = CreateVerifiedUser(testingMockProvidersContainer, "Self Viewer");
        string ownUsername = LookupUsername(ownToken);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = ownToken, Username = ownUsername });
        var profileData = response.ReadContentAsJsonDocument();
        List<string> actualProperties = [.. profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["avatarColor", "backgroundPhotoUrl", "bio", "displayName", "friendCount", "friendshipStatus", "profilePhotoUrl", "username"];

        Assert.Equal(expectedProperties, actualProperties);
        Assert.False(profileData.RootElement.TryGetProperty("emailAddress", out _));
        Assert.False(profileData.RootElement.TryGetProperty("phoneNumber", out _));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = "", Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = "not-a-real-token-at-all", Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeletedRequesterTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterToken = CreateVerifiedUser(testingMockProvidersContainer, "Requester Person");
        string targetToken = CreateVerifiedUser(testingMockProvidersContainer, "Target Person");
        string targetUsername = LookupUsername(targetToken);
        string requesterUsername = LookupUsername(requesterToken);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var requesterUser = dbContext.UserAccounts.Single(field => field.Username == requesterUsername);
            dbContext.UserAccounts.Remove(requesterUser);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Helpers

    private static string CreateVerifiedUser(TestingMockProvidersContainer testingMockProvidersContainer, string displayName) {
        string uniqueEmail = $"public{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = displayName, Email = uniqueEmail, Password = "Seven74!" };
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    private static string LookupUsername(string authToken) {
        Guid userId = Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userId).Username;
    }
}
