using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CheckUsernameAvailabilityTest {
    // Tests - Happy Path

    [Fact]
    public void AvailableUsernameReturnsTrue() {
        string uniqueEmail = $"chkun{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = authToken, Username = $"avail{Guid.NewGuid():N}".Substring(0, 10) + "1" });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(isAvailable);
    }

    [Fact]
    public void TakenUsernameReturnsFalse() {
        string email1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"user2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User One", Email = email1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email1, VerificationCode = code1 });

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User Two", Email = email2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2Verification);
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email2, VerificationCode = code2 });
        string token2 = verify2.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string user1Username = dbContext.UserAccounts.Single(field => field.EmailAddress == email1).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = token2, Username = user1Username });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(isAvailable);
    }

    // Tests - Own Username Edge Cases

    [Fact]
    public void OwnUsernameReturnsTrue() {
        string uniqueEmail = $"chkun{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string ownUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = authToken, Username = ownUsername });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.True(isAvailable);
    }

    [Fact]
    public void OwnUsernameDifferentCaseReturnsTrue() {
        string uniqueEmail = $"chkun{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string ownUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = authToken, Username = ownUsername.ToUpperInvariant() });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.True(isAvailable);
    }

    [Fact]
    public void TakenUsernameCaseInsensitiveReturnsFalse() {
        string email1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"user2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User One", Email = email1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email1, VerificationCode = code1 });

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User Two", Email = email2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2Verification);
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email2, VerificationCode = code2 });
        string token2 = verify2.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string user1Username = dbContext.UserAccounts.Single(field => field.EmailAddress == email1).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = token2, Username = user1Username.ToUpperInvariant() });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.False(isAvailable);
    }

    // Tests - Pending Account

    [Fact]
    public void UsernameTakenByPendingAccountReturnsFalse() {
        string pendingEmail = $"pending{Guid.NewGuid():N}@gmail.com";
        string verifiedEmail = $"verified{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Pending User", Email = pendingEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Verified User", Email = verifiedEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verifiedVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verifiedCode = EmailVerificationNotification.ExtractVerificationCode(verifiedVerification);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = verifiedEmail, VerificationCode = verifiedCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string pendingUsername = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == pendingEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = authToken, Username = pendingUsername });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.False(isAvailable);
    }

    // Tests - Invalid Input

    [Fact]
    public void EmptyUsernameReturnsFalse() {
        string uniqueEmail = $"chkun{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = authToken, Username = "" });
        bool isAvailable = response.ReadContentAsJsonDocument().RootElement.GetProperty("isAvailable").GetBoolean();

        Assert.False(isAvailable);
    }

    // Tests - Auth Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = "", Username = "someuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { AuthToken = "not-a-real-token", Username = "someuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/checkUsernameAvailability", new { Username = "someuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
