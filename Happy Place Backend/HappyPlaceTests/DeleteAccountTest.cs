using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeleteAccountTest {
    // Tests - Happy Path

    [Fact]
    public void CorrectPasswordDeletesAccountReturnsOk() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void CannotSignInAfterDeletion() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void TokenInvalidAfterDeletion() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/profile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.Unauthorized, profileResponse.StatusCode);
    }

    [Fact]
    public void AccountDataIsRemovedFromDatabase() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var deletedUser = dbContext.UserAccounts.SingleOrDefault(field => field.EmailAddress == uniqueEmail);

        Assert.Null(deletedUser);
    }

    [Fact]
    public void PasswordResetRequestsCleanedUpOnDeletion() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using (var dbContextBefore = HappyPlaceDbContext.Create()) {
            int resetRequestsBefore = dbContextBefore.PasswordResetRequests.Count(field => field.EmailAddress == uniqueEmail);
            Assert.True(resetRequestsBefore > 0);
        }

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        int resetRequestsAfter = dbContextAfter.PasswordResetRequests.Count(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(0, resetRequestsAfter);
    }

    [Fact]
    public void CanReRegisterWithSameEmailAfterDeletion() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage reRegisterResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "New User", Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, reRegisterResponse.StatusCode);
    }

    // Tests - Password Validation

    [Fact]
    public void WrongPasswordReturnsError() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var dbContext = HappyPlaceDbContext.Create();
        var userStillExists = dbContext.UserAccounts.SingleOrDefault(field => field.EmailAddress == uniqueEmail);
        Assert.NotNull(userStillExists);
    }

    [Fact]
    public void EmptyPasswordReturnsError() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken, Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void MissingPasswordFieldReturnsError() {
        string uniqueEmail = $"del{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Auth Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = "", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/deleteAccount", new { AuthToken = "not-a-real-token", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
