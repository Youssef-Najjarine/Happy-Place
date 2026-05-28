using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyPhoneChangeTest {
    // Constants

    private const string TestPassword = "Seven74!";

    // Tests - Happy Path

    [Fact]
    public void VerifyPhoneChangeForUserAddingFirstPhoneSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerifyPhoneChangeForUserReplacingExistingPhoneSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerifyPhoneChangeReturnsProfileContainingNewPhoneNumber() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        var responseData = response.ReadContentAsJsonDocument();
        Assert.Equal(newPhone, responseData.RootElement.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public void VerifyPhoneChangePersistsNewPhoneNumberOnUserAccount() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
        Assert.Equal(newPhone, userAccount.PhoneNumber);
    }

    [Fact]
    public void VerifyPhoneChangeDeletesPendingRowAfterSuccess() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
        Assert.False(dbContext.PendingPhoneChanges.Any(field => field.UserAccountId == userAccount.Id));
    }

    [Fact]
    public void AuthTokenRemainsValidAfterPhoneChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage getProfileResponse = container.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, getProfileResponse.StatusCode);
    }

    // Tests - Authentication

    [Fact]
    public void MissingAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = "", PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MalformedAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (_, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = "garbage", PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AuthTokenForDeletedUserReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            dbContext.UserAccounts.Remove(user);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Verification Code Format

    [Fact]
    public void WrongVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PartialVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithLettersReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - State and Replay

    [Fact]
    public void VerificationCodeReusedAfterSuccessReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage secondVerify = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    [Fact]
    public void VerifyingWithoutPendingChangeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string somePhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = somePhone, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingWithMismatchedPhoneNumberReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, _, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        string differentPhone = GenerateUniquePhone();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = differentPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingWithAnotherUsersCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string firstPhone, string firstCode) = SetupPendingPhoneChangeForEmailUser(container);
        (string secondAuthToken, _) = CreateAuthenticatedEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = secondAuthToken, PhoneNumber = firstPhone, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEqual(firstAuthToken, secondAuthToken);
    }

    // Tests - Verification Expiration

    [Fact]
    public void ExpiredVerificationCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt9Minutes59SecondsSucceeds() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-59);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeAt10Minutes1SecondReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10).AddSeconds(-1);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeWithCorruptedCreatedAtReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            var pendingChange = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
            pendingChange.CreatedAtUtc = default;
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Race Conditions and Conflict

    [Fact]
    public void VerifyingAfterAnotherUserClaimsThePhoneReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string contestedPhone, string firstCode) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Other User", PhoneNumber = contestedPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signupSms = container.SmsProvider.SentMessages.Last();
        string signupCode = SmsVerificationNotification.ExtractVerificationCode(signupSms);
        container.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = contestedPhone, VerificationCode = signupCode }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = contestedPhone, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingAfterAnotherUserCompletesPendingChangeForSamePhoneReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, _, string contestedPhone, string firstCode) = SetupPendingPhoneChangeForEmailUser(container);
        (string secondAuthToken, _) = CreateAuthenticatedEmailUser(container);
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = secondAuthToken, PhoneNumber = contestedPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string secondCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = secondAuthToken, PhoneNumber = contestedPhone, VerificationCode = secondCode }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = contestedPhone, VerificationCode = firstCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerifyingChangeToPhoneThatHasBecomeVerifiedDoesNotLeavePendingRow() {
        using var container = new TestingMockProvidersContainer();
        (string firstAuthToken, string firstEmailAddress, string contestedPhone, string firstCode) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Other User", PhoneNumber = contestedPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signupSms = container.SmsProvider.SentMessages.Last();
        string signupCode = SmsVerificationNotification.ExtractVerificationCode(signupSms);
        container.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = contestedPhone, VerificationCode = signupCode }).EnsureSuccessStatusCode();

        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = firstAuthToken, PhoneNumber = contestedPhone, VerificationCode = firstCode });

        using var dbContext = HappyPlaceDbContext.Create();
        var firstUserAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == firstEmailAddress);
        Assert.Null(firstUserAccount.PhoneNumber);
    }

    // Tests - Cross-Flow Contamination

    [Fact]
    public void SignupVerificationCodeCannotBeUsedForPhoneChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string otherPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Pending User", PhoneNumber = otherPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signupSms = container.SmsProvider.SentMessages.Last();
        string signupCode = SmsVerificationNotification.ExtractVerificationCode(signupSms);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = otherPhone, VerificationCode = signupCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private static (string authToken, string emailAddress, string newPhone, string verificationCode) SetupPendingPhoneChangeForEmailUser(TestingMockProvidersContainer container) {
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        SmsMessage sms = container.SmsProvider.SentMessages.Last();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(sms);
        return (authToken, emailAddress, newPhone, verificationCode);
    }
}
