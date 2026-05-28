using System.Net;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyEmailChangeEdgeCasesTest {
    // Fields

    private const string TestPassword = "Seven74!";

    // Tests - Cross-Field Isolation

    [Fact]
    public void EmailChangePreservesAllOtherUserAccountFields() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        Guid preUserId;
        string preUsername;
        string preDisplayName;
        string preBio;
        string preProfilePhotoUrl;
        string preBackgroundPhotoUrl;
        string preHashedPassword;
        string prePhoneNumber;
        DateTime preCreatedAtUtc;
        using (var preDbContext = HappyPlaceDbContext.Create()) {
            var preUser = preDbContext.UserAccounts.AsNoTracking().Single(field => field.PhoneNumber == phoneNumber);
            preUserId = preUser.Id;
            preUsername = preUser.Username;
            preDisplayName = preUser.DisplayName;
            preBio = preUser.Bio;
            preProfilePhotoUrl = preUser.ProfilePhotoUrl;
            preBackgroundPhotoUrl = preUser.BackgroundPhotoUrl;
            preHashedPassword = preUser.HashedPassword;
            prePhoneNumber = preUser.PhoneNumber;
            preCreatedAtUtc = preUser.CreatedAtUtc;
        }

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var postDbContext = HappyPlaceDbContext.Create();
        var postUser = postDbContext.UserAccounts.AsNoTracking().Single(field => field.Id == preUserId);
        Assert.Equal(preUsername, postUser.Username);
        Assert.Equal(preDisplayName, postUser.DisplayName);
        Assert.Equal(preBio, postUser.Bio);
        Assert.Equal(preProfilePhotoUrl, postUser.ProfilePhotoUrl);
        Assert.Equal(preBackgroundPhotoUrl, postUser.BackgroundPhotoUrl);
        Assert.Equal(preHashedPassword, postUser.HashedPassword);
        Assert.Equal(prePhoneNumber, postUser.PhoneNumber);
        Assert.Equal(preCreatedAtUtc, postUser.CreatedAtUtc);
        Assert.Equal(newEmail, postUser.EmailAddress);
    }

    [Fact]
    public void EmailChangeDoesNotAffectConcurrentPendingPhoneChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string newPhone = GenerateUniquePhone();
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string phoneChangeCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string emailChangeCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = emailChangeCode }).EnsureSuccessStatusCode();

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
            var pendingPhone = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == user.Id);
            Assert.Equal(newPhone, pendingPhone.NewPhoneNumber);
            Assert.Equal(phoneChangeCode, pendingPhone.VerificationCode);
        }
        HttpResponseMessage phoneVerify = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = phoneChangeCode });
        Assert.Equal(HttpStatusCode.OK, phoneVerify.StatusCode);
    }

    // Tests - End-to-End Sign-In

    [Fact]
    public void SignInWithNewEmailSucceedsAfterEmailChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = container.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = newEmail, Password = TestPassword });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithOldEmailFailsAfterEmailChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string oldEmail) = CreateAuthenticatedEmailUser(container);
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = container.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = oldEmail, Password = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    // Tests - Trimming On Verify

    [Fact]
    public void VerifyEmailChangeTrimsWhitespaceOnEmailInRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        string paddedEmail = $"  {newEmail}  ";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = paddedEmail, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Null Input Safety

    [Fact]
    public void NullEmailAddressInVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, _, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = (string)null, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NullVerificationCodeInVerifyEmailChangeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = (string)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Same-User-Multiple-Accounts Security Invariant

    [Fact]
    public void CannotMoveEmailFromOneOwnedAccountToAnotherOwnedAccount() {
        using var container = new TestingMockProvidersContainer();
        string emailOwnedByOtherAccount = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Same Person", Email = emailOwnedByOtherAccount, Password = TestPassword }).EnsureSuccessStatusCode();
        string emailSignUpCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = emailOwnedByOtherAccount, VerificationCode = emailSignUpCode }).EnsureSuccessStatusCode();
        (string phoneAuthToken, _) = CreateAuthenticatedPhoneUser(container);
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = phoneAuthToken, EmailAddress = emailOwnedByOtherAccount, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string changeCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = phoneAuthToken, EmailAddress = emailOwnedByOtherAccount, VerificationCode = changeCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Multiple Sequential Changes

    [Fact]
    public void UserCanChangeEmailTwiceInSuccession() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string firstNewEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string firstCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = firstNewEmail, VerificationCode = firstCode }).EnsureSuccessStatusCode();
        string secondNewEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string secondCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = secondNewEmail, VerificationCode = secondCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        Assert.Equal(secondNewEmail, user.EmailAddress);
        Assert.False(dbContext.PendingEmailChanges.Any(field => field.UserAccountId == user.Id));
    }

    // Tests - Code Format Completion

    [Fact]
    public void VerificationCodeWithLeadingWhitespaceInEmailVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, string verificationCode) = SetupPendingEmailChangeForPhoneUser(container);
        string paddedCode = $" {verificationCode}";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = paddedCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeLongerThanSixCharactersInEmailVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = "1234567" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Cross-Flow Contamination

    [Fact]
    public void PhoneChangeCodeCannotBeUsedForEmailChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string phoneChangeCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = phoneChangeCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ForgotPasswordVerificationCodeCannotBeUsedForEmailChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber, string newEmail, _) = SetupPendingEmailChangeForPhoneUser(container);
        container.WebClient.PostJson("api/userAuthentication/forgotPasswordWithPhone", new { PhoneNumber = phoneNumber }).EnsureSuccessStatusCode();
        string forgotPasswordCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = forgotPasswordCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Stale Old Email With Valid New Code

    [Fact]
    public void VerifyingWithStaleOldEmailButValidNewCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedPhoneUser(container);
        string oldRequestedEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = oldRequestedEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string newRequestedEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newRequestedEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string currentValidCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = oldRequestedEmail, VerificationCode = currentValidCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Email Case Preservation In Storage

    [Fact]
    public void EmailChangePreservesUserTypedCasingInStorage() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string phoneNumber) = CreateAuthenticatedPhoneUser(container);
        string mixedCaseEmail = $"MixedCase.User{Guid.NewGuid():N}@Example.COM";
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = mixedCaseEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = mixedCaseEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        Assert.Equal(mixedCaseEmail, user.EmailAddress);
    }

    // Tests - Information Leakage Smoke Check

    [Fact]
    public void WrongCodeAndExpiredAndConflictReturnIdenticalBadRequestStatusForEmail() {
        using var wrongCodeContainer = new TestingMockProvidersContainer();
        (string wrongCodeAuthToken, _, string wrongCodeEmail, _) = SetupPendingEmailChangeForPhoneUser(wrongCodeContainer);
        HttpResponseMessage wrongCodeResponse = wrongCodeContainer.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = wrongCodeAuthToken, EmailAddress = wrongCodeEmail, VerificationCode = "000000" });

        using var expiredContainer = new TestingMockProvidersContainer();
        (string expiredAuthToken, string expiredPhone, string expiredEmail, string expiredCode) = SetupPendingEmailChangeForPhoneUser(expiredContainer);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == expiredPhone);
            var pending = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == userAccount.Id);
            pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
            dbContext.SaveChanges();
        }
        HttpResponseMessage expiredResponse = expiredContainer.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = expiredAuthToken, EmailAddress = expiredEmail, VerificationCode = expiredCode });

        using var conflictContainer = new TestingMockProvidersContainer();
        (string conflictAuthToken, _, string contestedEmail, string conflictCode) = SetupPendingEmailChangeForPhoneUser(conflictContainer);
        conflictContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Other", Email = contestedEmail, Password = TestPassword }).EnsureSuccessStatusCode();
        string signupCode = EmailVerificationNotification.ExtractVerificationCode(conflictContainer.EmailProvider.EmailMessages.Last());
        conflictContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = contestedEmail, VerificationCode = signupCode }).EnsureSuccessStatusCode();
        HttpResponseMessage conflictResponse = conflictContainer.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = conflictAuthToken, EmailAddress = contestedEmail, VerificationCode = conflictCode });

        Assert.Equal(HttpStatusCode.BadRequest, wrongCodeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, conflictResponse.StatusCode);
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
        container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = TestPassword }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        verifyResponse.EnsureSuccessStatusCode();
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
        return (authToken, uniqueEmail);
    }

    private static (string authToken, string phoneNumber) CreateAuthenticatedPhoneUser(TestingMockProvidersContainer container) {
        string uniquePhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Test User", PhoneNumber = uniquePhone, Password = TestPassword }).EnsureSuccessStatusCode();
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
