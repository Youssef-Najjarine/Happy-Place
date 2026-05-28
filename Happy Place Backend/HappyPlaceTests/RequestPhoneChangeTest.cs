using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RequestPhoneChangeTest {
    // Constants

    private const string TestPassword = "Seven74!";

    // Tests - Happy Path

    [Fact]
    public void RequestPhoneChangeForUserWithoutPhoneSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RequestPhoneChangeForUserWithExistingPhoneSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void RequestPhoneChangeSendsExactlyOneSms() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();

        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        Assert.Single(container.SmsProvider.SentMessages);
    }

    [Fact]
    public void RequestPhoneChangeCreatesPendingRowForUser() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();

        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
        var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
        Assert.Equal(newPhone, pendingChange.NewPhoneNumber);
    }

    // Tests - Authentication

    [Fact]
    public void MissingAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = "", PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void WhitespaceAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = "   ", PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MalformedAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        string newPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = "not-a-real-token", PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AuthTokenForDeletedUserReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            dbContext.UserAccounts.Remove(user);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Phone Number Validation

    [Fact]
    public void EmptyPhoneNumberReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespacePhoneNumberReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "   ", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithLettersReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "949abc5148", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithPlusSignReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "+19497359148", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithDashesReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "949-735-9148", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberWithSpacesReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "949 735 9148", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberTooShortReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "12345", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberAtExactly7DigitsSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newPhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(7));

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberAtExactly20DigitsSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string guidDigits = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        string newPhone = guidDigits + "0123456789";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PhoneNumberExceedingMaxLengthReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = "123456789012345678901", CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Business Rules

    [Fact]
    public void ChangingToSameCurrentPhoneNumberReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string currentPhone) = CreateAuthenticatedPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = currentPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ChangingToAnotherUsersVerifiedPhoneAtRequestStepSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _) = CreateAuthenticatedEmailUser(container);
        string takenPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = takenPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        SmsMessage firstSms = container.SmsProvider.SentMessages.Single();
        string firstCode = SmsVerificationNotification.ExtractVerificationCode(firstSms);
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = takenPhone, VerificationCode = firstCode }).EnsureSuccessStatusCode();
        (string secondAuthToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = secondAuthToken, PhoneNumber = takenPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ChangingToPhoneInUnverifiedPendingSignupSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string pendingSignupPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Other User", PhoneNumber = pendingSignupPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = pendingSignupPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void ChangingToAnotherUsersPendingChangePhoneSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _) = CreateAuthenticatedEmailUser(container);
        string contestedPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = contestedPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        (string secondAuthToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = secondAuthToken, PhoneNumber = contestedPhone, CurrentPassword = TestPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Re-Request and Overwrite

    [Fact]
    public void ReRequestingPhoneChangeOverwritesPreviousPendingRow() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string firstNewPhone = GenerateUniquePhone();
        string secondNewPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
        var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
        Assert.Equal(secondNewPhone, pendingChange.NewPhoneNumber);
    }

    [Fact]
    public void OldVerificationCodeInvalidAfterReRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string firstNewPhone = GenerateUniquePhone();
        string secondNewPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        SmsMessage firstSms = container.SmsProvider.SentMessages.Single();
        string oldVerificationCode = SmsVerificationNotification.ExtractVerificationCode(firstSms);

        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, VerificationCode = oldVerificationCode });
        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void NewVerificationCodeWorksAfterReRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string firstNewPhone = GenerateUniquePhone();
        string secondNewPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        SmsMessage secondSms = container.SmsProvider.SentMessages.Last();
        string newVerificationCode = SmsVerificationNotification.ExtractVerificationCode(secondSms);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, VerificationCode = newVerificationCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void ReRequestingSamePhoneIssuesNewCode() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        SmsMessage firstSms = container.SmsProvider.SentMessages.Single();
        string firstCode = SmsVerificationNotification.ExtractVerificationCode(firstSms);

        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        SmsMessage secondSms = container.SmsProvider.SentMessages.Last();
        string secondCode = SmsVerificationNotification.ExtractVerificationCode(secondSms);
        Assert.NotEqual(firstCode, secondCode);
        HttpResponseMessage verifyOldCode = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = firstCode });
        Assert.Equal(HttpStatusCode.BadRequest, verifyOldCode.StatusCode);
    }

    // Helpers

    private static string GenerateUniquePhone() {
        return string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
    }

    private static (string authToken, string emailAddress) CreateAuthenticatedEmailUser(TestingMockProvidersContainer container) {
        string uniqueEmail = $"user{Guid.NewGuid():N}@gmail.com";
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
