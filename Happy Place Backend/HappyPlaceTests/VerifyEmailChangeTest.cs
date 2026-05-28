using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyEmailChangeTest {
    // Constants

    private const string TestPassword = "Seven74!";

    // Tests - Happy Path

    [Fact]
    public void VerifyEmailChangeForUserAddingFirstEmailSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerifyEmailChangeForUserReplacingExistingEmailSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerifyEmailChangeReturnsProfileContainingNewEmailAddress() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        var responseData = response.ReadContentAsJsonDocument();
        Assert.Equal(newEmail, responseData.RootElement.GetProperty("emailAddress").GetString());
    }

    [Fact]
    public void VerifyEmailChangePersistsNewEmailAddressOnUserAccount() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        Assert.Equal(newEmail, userAccount.EmailAddress);
    }

    [Fact]
    public void VerifyEmailChangeDeletesPendingRowAfterSuccess() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        Assert.False(dbContext.PendingEmailChanges.Any(field => field.UserAccountId == userAccount.Id));
    }

    [Fact]
    public void AuthTokenRemainsValidAfterEmailChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage getProfileResponse = container.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, getProfileResponse.StatusCode);
    }

    // Tests - Authentication

    [Fact]
    public void MissingAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = "", EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MalformedAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = "garbage", EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AuthTokenForDeletedUserReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            dbContext.UserAccounts.Remove(user);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Verification Code Format

    [Fact]
    public void WrongVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PartialVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithLettersReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - State and Replay

    [Fact]
    public void VerificationCodeReusedAfterSuccessReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage secondVerify = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    [Fact]
    public void VerifyingWithoutPendingChangeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string someEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = someEmail, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingWithMismatchedEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, _, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        string differentEmail = GenerateUniqueEmail();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = differentEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingWithAnotherUsersCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string firstEmail, string firstCode) = SetupPendingEmailChangeForPhoneUser(container);
        (string secondAuthToken, _) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = secondAuthToken, EmailAddress = firstEmail, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEqual(firstAuthToken, secondAuthToken);
    }

    // Tests - Verification Expiration

    [Fact]
    public void ExpiredVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt9Minutes59SecondsSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-59);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt10Minutes1SecondReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10).AddSeconds(-1);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithCorruptedCreatedAtReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            var pendingChange = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = default;
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Race Conditions and Conflict

    [Fact]
    public void VerifyingAfterAnotherUserClaimsTheEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string contestedEmail, string firstCode) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Other User", Email = contestedEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signupEmail = container.EmailProvider.EmailMessages.Last();
        string signupCode = EmailVerificationNotification.ExtractVerificationCode(signupEmail);
        container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = contestedEmail, VerificationCode = signupCode }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = firstAuthToken, EmailAddress = contestedEmail, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingAfterAnotherUserCompletesPendingChangeForSameEmailReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string contestedEmail, string firstCode) = SetupPendingEmailChangeForPhoneUser(container);
        (string secondAuthToken, _) = CreateAuthenticatedPhoneUser(container);
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = secondAuthToken, EmailAddress = contestedEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string secondCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = secondAuthToken, EmailAddress = contestedEmail, VerificationCode = secondCode }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = firstAuthToken, EmailAddress = contestedEmail, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingChangeToEmailThatHasBecomeVerifiedDoesNotLeavePendingRow() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, string firstPhoneNumber, string contestedEmail, string firstCode) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Other User", Email = contestedEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signupEmail = container.EmailProvider.EmailMessages.Last();
        string signupCode = EmailVerificationNotification.ExtractVerificationCode(signupEmail);
        container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = contestedEmail, VerificationCode = signupCode }).EnsureSuccessStatusCode();

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = firstAuthToken, EmailAddress = contestedEmail, VerificationCode = firstCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var firstUserAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == firstPhoneNumber);
        Assert.Null(firstUserAccount.EmailAddress);
    }

    [Fact]
    public void EmailComparisonForConflictIsCaseInsensitive() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string baseEmail, string firstCode) = SetupPendingEmailChangeForPhoneUser(container);
        string upperCaseVariant = baseEmail.ToUpperInvariant();
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Other User", Email = upperCaseVariant, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signupEmail = container.EmailProvider.EmailMessages.Last();
        string signupCode = EmailVerificationNotification.ExtractVerificationCode(signupEmail);
        container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = upperCaseVariant, VerificationCode = signupCode }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = firstAuthToken, EmailAddress = baseEmail, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Cross-Flow Contamination

    [Fact]
    public void SignupVerificationCodeCannotBeUsedForEmailChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string otherEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Pending User", Email = otherEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signupEmail = container.EmailProvider.EmailMessages.Last();
        string signupCode = EmailVerificationNotification.ExtractVerificationCode(signupEmail);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = otherEmail, VerificationCode = signupCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static (string authToken, string phoneNumber, string newEmail, string verificationCode) SetupPendingEmailChangeForPhoneUser(TestingMockProvidersContainer container) {
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        MailMessage email = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(email);
        return (authToken, phoneNumber, newEmail, verificationCode);
    }
}
