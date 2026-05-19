using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GetPublicUserProfileTest {
    // Tests - Happy Path

    [Fact]
    public void AuthenticatedUserCanViewAnotherUsersPublicProfile() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public void PublicProfileReturnsDisplayName() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        string targetName = "Alice Thompson";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = targetName, Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        string returnedName = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("displayName").GetString();

        Assert.Equal(targetName, returnedName);
    }

    [Fact]
    public void PublicProfileReturnsUsername() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        string returnedUsername = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString();

        Assert.Equal(targetUsername, returnedUsername);
    }

    [Fact]
    public void PublicProfileReturnsAvatarColorMatchingDirectCalculation() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var targetUser = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail);
        string expectedColor = UserAccountRegistrar.GetAvatarColor(targetUser.Id);

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUser.Username });
        string returnedColor = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        Assert.Equal(expectedColor, returnedColor);
    }

    [Fact]
    public void PublicProfileReturnsBioWhenSet() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        string expectedBio = "I believe in the power of kindness and connection.";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var targetUser = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail);
        targetUser.Bio = expectedBio;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUser.Username });
        string returnedBio = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("bio").GetString();

        Assert.Equal(expectedBio, returnedBio);
    }

    [Fact]
    public void PublicProfileReturnsProfilePhotoUrlWhenSet() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        string expectedPhotoUrl = "https://happyplace.blob.core.windows.net/photos/target-avatar.jpg";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var targetUser = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail);
        targetUser.ProfilePhotoUrl = expectedPhotoUrl;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUser.Username });
        string returnedPhotoUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl").GetString();

        Assert.Equal(expectedPhotoUrl, returnedPhotoUrl);
    }

    [Fact]
    public void PublicProfileReturnsBackgroundPhotoUrlWhenSet() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        string expectedBgUrl = "https://happyplace.blob.core.windows.net/backgrounds/target-bg.jpg";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var targetUser = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail);
        targetUser.BackgroundPhotoUrl = expectedBgUrl;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUser.Username });
        string returnedBgUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl").GetString();

        Assert.Equal(expectedBgUrl, returnedBgUrl);
    }

    // Tests - Response Security

    [Fact]
    public void PublicProfileDoesNotContainEmailAddress() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("emailAddress", out _));
        Assert.False(profileData.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public void PublicProfileDoesNotContainPhoneNumber() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("phoneNumber", out _));
        Assert.False(profileData.RootElement.TryGetProperty("phone", out _));
    }

    [Fact]
    public void PublicProfileDoesNotContainHashedPassword() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("hashedPassword", out _));
        Assert.False(profileData.RootElement.TryGetProperty("password", out _));
    }

    [Fact]
    public void PublicProfileDoesNotContainUserId() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("userId", out _));
        Assert.False(profileData.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public void PublicProfileContainsExactlyExpectedProperties() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        List<string> actualProperties = profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToList();
        List<string> expectedProperties = new List<string> { "avatarColor", "backgroundPhotoUrl", "bio", "displayName", "profilePhotoUrl", "username" }.OrderBy(name => name).ToList();

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Not Found

    [Fact]
    public void DeletedTargetUserReturnsNotFound() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"deltgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var targetUser = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail);
        string targetUsername = targetUser.Username;
        dbContext.UserAccounts.Remove(targetUser);
        dbContext.SaveChanges();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void NonExistentUsernameReturnsNotFound() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = "nonexistentuser999999" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void EmptyUsernameReturnsNotFound() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = "" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyUsernameReturnsNotFound() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = "   " });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void MissingUsernameFieldReturnsNotFound() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Tests - Edge Cases

    [Fact]
    public void ViewingOwnProfileViaGetPublicUserProfileReturnsPublicOnly() {
        string uniqueEmail = $"self{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Self Viewer", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string ownUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = authToken, Username = ownUsername });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        List<string> actualProperties = profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToList();
        List<string> expectedProperties = new List<string> { "avatarColor", "backgroundPhotoUrl", "bio", "displayName", "profilePhotoUrl", "username" }.OrderBy(name => name).ToList();

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal(expectedProperties, actualProperties);
        Assert.False(profileData.RootElement.TryGetProperty("emailAddress", out _));
        Assert.False(profileData.RootElement.TryGetProperty("phoneNumber", out _));
    }

    [Fact]
    public void UsernameLookupIsCaseInsensitive() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;
        string uppercaseUsername = targetUsername.ToUpperInvariant();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = uppercaseUsername });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal(targetUsername, profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString());
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = "", Username = "anyuser" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = "not-a-real-token", Username = "anyuser" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { Username = "anyuser" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeletedRequesterTokenReturnsUnauthorized() {
        string requesterEmail = $"delreq{Guid.NewGuid():N}@gmail.com";
        string targetEmail = $"tgt{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Target User", Email = targetEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage targetVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string targetCode = EmailVerificationNotification.ExtractVerificationCode(targetVerification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = targetEmail, VerificationCode = targetCode });

        using var dbContext = HappyPlaceDbContext.Create();
        string targetUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == targetEmail).Username;
        var requesterAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == requesterEmail);
        dbContext.UserAccounts.Remove(requesterAccount);
        dbContext.SaveChanges();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = targetUsername });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Cross-User Data Correctness

    [Fact]
    public void TwoDifferentUsersPublicProfilesReturnCorrectData() {
        string requesterEmail = $"req{Guid.NewGuid():N}@gmail.com";
        string target1Email = $"tgt1{Guid.NewGuid():N}@gmail.com";
        string target2Email = $"tgt2{Guid.NewGuid():N}@gmail.com";
        string target1Name = "Alice Thompson";
        string target2Name = "Bob Martinez";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Requester User", Email = requesterEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage requesterVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string requesterCode = EmailVerificationNotification.ExtractVerificationCode(requesterVerification);
        HttpResponseMessage requesterVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = requesterEmail, VerificationCode = requesterCode });
        string requesterToken = requesterVerify.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = target1Name, Email = target1Email, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage target1Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string target1Code = EmailVerificationNotification.ExtractVerificationCode(target1Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = target1Email, VerificationCode = target1Code });

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = target2Name, Email = target2Email, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage target2Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string target2Code = EmailVerificationNotification.ExtractVerificationCode(target2Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = target2Email, VerificationCode = target2Code });

        using var dbContext = HappyPlaceDbContext.Create();
        string target1Username = dbContext.UserAccounts.Single(field => field.EmailAddress == target1Email).Username;
        string target2Username = dbContext.UserAccounts.Single(field => field.EmailAddress == target2Email).Username;

        HttpResponseMessage profile1Response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = target1Username });
        HttpResponseMessage profile2Response = testingMockProvidersContainer.WebClient.PostJson("api/profile/getPublicUserProfile", new { AuthToken = requesterToken, Username = target2Username });
        var profile1 = profile1Response.ReadContentAsJsonDocument();
        var profile2 = profile2Response.ReadContentAsJsonDocument();

        Assert.Equal(target1Name, profile1.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(target1Username, profile1.RootElement.GetProperty("username").GetString());
        Assert.Equal(target2Name, profile2.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(target2Username, profile2.RootElement.GetProperty("username").GetString());
        Assert.NotEqual(profile1.RootElement.GetProperty("username").GetString(), profile2.RootElement.GetProperty("username").GetString());
    }
}
