using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RequestEmailChangeTest {
    // Constants

    private const string TestPassword = "Seven74!";

    // Tests - Happy Path

    [Fact]
    public void RequestEmailChangeForUserWithoutEmailSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RequestEmailChangeForUserWithExistingEmailSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RequestEmailChangeSendsExactlyOneEmail() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();

        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        Assert.Single(container.EmailProvider.EmailMessages);
    }

    [Fact]
    public void RequestEmailChangeCreatesPendingRowForUser() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();

        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
        Assert.Equal(newEmail, pendingChange.NewEmailAddress);
    }

    // Tests - Authentication

    [Fact]
    public void MissingAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = "", EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void WhitespaceAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = "   ", EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MalformedAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = "garbage", EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AuthTokenForDeletedUserReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            dbContext.UserAccounts.Remove(user);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Email Validation

    [Fact]
    public void EmptyEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "   ", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingAtSymbolReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarinegmail.com", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailWithMultipleAtSignsReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "user@@gmail.com", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailWithSpacesReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarine @gmail.com", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingDomainReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarine@", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingUsernameReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "@gmail.com", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailMissingDotInDomainReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarine@gmailcom", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailDomainStartingWithDotReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarine@.gmail.com", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailDomainEndingWithDotReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = "ynajjarine@gmail.com.", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailAtExactly255CharactersSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string guid = Guid.NewGuid().ToString("N");
        string localPart = guid + new string('a', 245 - guid.Length);
        string newEmail = $"{localPart}@gmail.com";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void EmailExceedingMaxLengthReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string overLengthEmail = new string('a', 246) + "@gmail.com";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = overLengthEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Business Rules

    [Fact]
    public void ChangingToSameCurrentEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string currentEmail) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = currentEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ChangingToSameCurrentEmailDifferentCaseReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string currentEmail) = CreateAuthenticatedEmailUser(container);
        string differentCase = currentEmail.ToUpperInvariant();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = differentCase, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ChangingToAnotherUsersVerifiedEmailAtRequestStepSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _) = CreateAuthenticatedPhoneUser(container);
        string takenEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = firstAuthToken, EmailAddress = takenEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string firstCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = firstAuthToken, EmailAddress = takenEmail, VerificationCode = firstCode }).EnsureSuccessStatusCode();
        (string secondAuthToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = secondAuthToken, EmailAddress = takenEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ChangingToEmailInUnverifiedPendingSignupSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string pendingSignupEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Other User", Email = pendingSignupEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = pendingSignupEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ChangingToAnotherUsersPendingChangeEmailSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _) = CreateAuthenticatedPhoneUser(container);
        string contestedEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = firstAuthToken, EmailAddress = contestedEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        (string secondAuthToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = secondAuthToken, EmailAddress = contestedEmail, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Re-Request and Overwrite

    [Fact]
    public void ReRequestingEmailChangeOverwritesPreviousPendingRow() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string firstNewEmail = GenerateUniqueEmail();
        string secondNewEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
        Assert.Equal(secondNewEmail, pendingChange.NewEmailAddress);
    }

    [Fact]
    public void OldVerificationCodeInvalidAfterReRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string firstNewEmail = GenerateUniqueEmail();
        string secondNewEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string oldVerificationCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Single());

        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, VerificationCode = oldVerificationCode });
        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void NewVerificationCodeWorksAfterReRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string firstNewEmail = GenerateUniqueEmail();
        string secondNewEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        string newVerificationCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, VerificationCode = newVerificationCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void ReRequestingSameEmailIssuesNewCode() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string firstCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Single());

        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        string secondCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        Assert.NotEqual(firstCode, secondCode);
        HttpResponseMessage verifyOldCode = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = firstCode });
        Assert.Equal(HttpStatusCode.BadRequest, verifyOldCode.StatusCode);
    }

    // Helpers

    private static string GenerateUniqueEmail() {
        return $"user{Guid.NewGuid():N}@gmail.com";
    }

    private static string GenerateUniquePhone() {
        return string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
    }

    private static (string authToken, string emailAddress) CreateAuthenticatedEmailUser(TestingMockProvidersContainer container) {
        string uniqueEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        verifyResponse.EnsureSuccessStatusCode();
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
        return (authToken, uniqueEmail);
    }

    private static (string authToken, string phoneNumber) CreateAuthenticatedPhoneUser(TestingMockProvidersContainer container) {
        string uniquePhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Test User", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = container.SmsProvider.SentMessages.Last();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        verifyResponse.EnsureSuccessStatusCode();
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
        return (authToken, uniquePhone);
    }
}
