using System.Net;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyPhoneChangeEdgeCasesTest {
    // Fields

    private const string TestPassword = "Seven74!";

    // Tests - Cross-Field Isolation

    [Fact]
    public void PhoneChangePreservesAllOtherUserAccountFields() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        Guid preUserId;
        string preUsername;
        string preDisplayName;
        string preBio;
        string preProfilePhotoUrl;
        string preBackgroundPhotoUrl;
        string preHashedPassword;
        string preEmailAddress;
        DateTime preCreatedAtUtc;
        using (var preDbContext = HappyPlaceDbContext.Create()) {
            var preUser = preDbContext.UserAccounts.AsNoTracking().Single(field => field.EmailAddress == emailAddress);
            preUserId = preUser.Id;
            preUsername = preUser.Username;
            preDisplayName = preUser.DisplayName;
            preBio = preUser.Bio;
            preProfilePhotoUrl = preUser.ProfilePhotoUrl;
            preBackgroundPhotoUrl = preUser.BackgroundPhotoUrl;
            preHashedPassword = preUser.HashedPassword;
            preEmailAddress = preUser.EmailAddress;
            preCreatedAtUtc = preUser.CreatedAtUtc;
        }

        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var postDbContext = HappyPlaceDbContext.Create();
        var postUser = postDbContext.UserAccounts.AsNoTracking().Single(field => field.Id == preUserId);
        Assert.Equal(preUsername, postUser.Username);
        Assert.Equal(preDisplayName, postUser.DisplayName);
        Assert.Equal(preBio, postUser.Bio);
        Assert.Equal(preProfilePhotoUrl, postUser.ProfilePhotoUrl);
        Assert.Equal(preBackgroundPhotoUrl, postUser.BackgroundPhotoUrl);
        Assert.Equal(preHashedPassword, postUser.HashedPassword);
        Assert.Equal(preEmailAddress, postUser.EmailAddress);
        Assert.Equal(preCreatedAtUtc, postUser.CreatedAtUtc);
        Assert.Equal(newPhone, postUser.PhoneNumber);
    }

    [Fact]
    public void PhoneChangeDoesNotAffectConcurrentPendingEmailChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string emailChangeCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string phoneChangeCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = phoneChangeCode }).EnsureSuccessStatusCode();

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var user = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
            var pendingEmail = dbContext.PendingEmailChanges.Single(field => field.UserAccountId == user.Id);
            Assert.Equal(newEmail, pendingEmail.NewEmailAddress);
            Assert.Equal(emailChangeCode, pendingEmail.VerificationCode);
        }
        HttpResponseMessage emailVerify = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = emailChangeCode });
        Assert.Equal(HttpStatusCode.OK, emailVerify.StatusCode);
    }

    // Tests - End-to-End Sign-In

    [Fact]
    public void SignInWithNewPhoneSucceedsAfterPhoneChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = container.WebClient.PostJson("api/userAuthentication/signInWithPhone", new { PhoneNumber = newPhone, Password = TestPassword });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithOldPhoneFailsAfterPhoneChange() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string oldPhone) = CreateAuthenticatedPhoneUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = container.WebClient.PostJson("api/userAuthentication/signInWithPhone", new { PhoneNumber = oldPhone, Password = TestPassword });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    // Tests - Trimming On Verify

    [Fact]
    public void VerifyPhoneChangeTrimsWhitespaceOnPhoneInRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        string paddedPhone = $"  {newPhone}  ";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = paddedPhone, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Tests - Null Input Safety

    [Fact]
    public void NullPhoneNumberInVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, _, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = (string)null, VerificationCode = verificationCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NullVerificationCodeInVerifyPhoneChangeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = (string)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Same-User-Multiple-Accounts Security Invariant

    [Fact]
    public void CannotMovePhoneFromOneOwnedAccountToAnotherOwnedAccount() {
        using var container = new TestingMockProvidersContainer();
        string phoneOwnedByOtherAccount = GenerateUniquePhone();
        container.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Same Person", PhoneNumber = phoneOwnedByOtherAccount, Password = TestPassword }).EnsureSuccessStatusCode();
        string phoneSignUpCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = phoneOwnedByOtherAccount, VerificationCode = phoneSignUpCode }).EnsureSuccessStatusCode();
        (string emailAuthToken, _) = CreateAuthenticatedEmailUser(container);
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = emailAuthToken, PhoneNumber = phoneOwnedByOtherAccount, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string changeCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = emailAuthToken, PhoneNumber = phoneOwnedByOtherAccount, VerificationCode = changeCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Multiple Sequential Changes

    [Fact]
    public void UserCanChangePhoneTwiceInSuccession() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string firstNewPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string firstCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = firstNewPhone, VerificationCode = firstCode }).EnsureSuccessStatusCode();
        string secondNewPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string secondCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = secondNewPhone, VerificationCode = secondCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == emailAddress);
        Assert.Equal(secondNewPhone, user.PhoneNumber);
        Assert.False(dbContext.PendingPhoneChanges.Any(field => field.UserAccountId == user.Id));
    }

    [Fact]
    public void UserCanInterleavePhoneAndEmailChanges() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string originalEmail) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string phoneCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());
        container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = phoneCode }).EnsureSuccessStatusCode();
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string emailCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, VerificationCode = emailCode });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == newEmail);
        Assert.Equal(newPhone, user.PhoneNumber);
        Assert.Equal(newEmail, user.EmailAddress);
        Assert.NotEqual(originalEmail, user.EmailAddress);
    }

    // Tests - Code Format Completion

    [Fact]
    public void VerificationCodeWithLeadingWhitespaceInPhoneVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, string verificationCode) = SetupPendingPhoneChangeForEmailUser(container);
        string paddedCode = $" {verificationCode}";

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = paddedCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void VerificationCodeLongerThanSixCharactersInPhoneVerifyReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = "1234567" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Cross-Flow Contamination

    [Fact]
    public void EmailChangeCodeCannotBeUsedForPhoneChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string newEmail = GenerateUniqueEmail();
        container.WebClient.PostJson("api/userProfile/requestEmailChange", new { AuthToken = authToken, EmailAddress = newEmail, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string emailChangeCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = emailChangeCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ForgotPasswordVerificationCodeCannotBeUsedForPhoneChangeVerify() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, string emailAddress, string newPhone, _) = SetupPendingPhoneChangeForEmailUser(container);
        container.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = emailAddress }).EnsureSuccessStatusCode();
        string forgotPasswordCode = EmailVerificationNotification.ExtractVerificationCode(container.EmailProvider.EmailMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, VerificationCode = forgotPasswordCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Stale Old Phone With Valid New Code

    [Fact]
    public void VerifyingWithStaleOldPhoneButValidNewCodeReturnsBadRequest() {
        using var container = new TestingMockProvidersContainer();
        (string authToken, _) = CreateAuthenticatedEmailUser(container);
        string oldRequestedPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = oldRequestedPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string newRequestedPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newRequestedPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        string currentValidCode = SmsVerificationNotification.ExtractVerificationCode(container.SmsProvider.SentMessages.Last());

        HttpResponseMessage response = container.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = authToken, PhoneNumber = oldRequestedPhone, VerificationCode = currentValidCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Information Leakage Smoke Check

    [Fact]
    public void WrongCodeAndExpiredAndConflictReturnIdenticalBadRequestStatus() {
        using var wrongCodeContainer = new TestingMockProvidersContainer();
        (string wrongCodeAuthToken, _, string wrongCodePhone, _) = SetupPendingPhoneChangeForEmailUser(wrongCodeContainer);
        HttpResponseMessage wrongCodeResponse = wrongCodeContainer.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = wrongCodeAuthToken, PhoneNumber = wrongCodePhone, VerificationCode = "000000" });

        using var expiredContainer = new TestingMockProvidersContainer();
        (string expiredAuthToken, string expiredEmail, string expiredPhone, string expiredCode) = SetupPendingPhoneChangeForEmailUser(expiredContainer);
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == expiredEmail);
            var pending = dbContext.PendingPhoneChanges.Single(field => field.UserAccountId == userAccount.Id);
            pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
            dbContext.SaveChanges();
        }
        HttpResponseMessage expiredResponse = expiredContainer.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = expiredAuthToken, PhoneNumber = expiredPhone, VerificationCode = expiredCode });

        using var conflictContainer = new TestingMockProvidersContainer();
        (string conflictAuthToken, _, string contestedPhone, string conflictCode) = SetupPendingPhoneChangeForEmailUser(conflictContainer);
        conflictContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Other", PhoneNumber = contestedPhone, Password = TestPassword }).EnsureSuccessStatusCode();
        string signupCode = SmsVerificationNotification.ExtractVerificationCode(conflictContainer.SmsProvider.SentMessages.Last());
        conflictContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = contestedPhone, VerificationCode = signupCode }).EnsureSuccessStatusCode();
        HttpResponseMessage conflictResponse = conflictContainer.WebClient.PostJson("api/userProfile/verifyPhoneChange", new { AuthToken = conflictAuthToken, PhoneNumber = contestedPhone, VerificationCode = conflictCode });

        Assert.Equal(HttpStatusCode.BadRequest, wrongCodeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, expiredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, conflictResponse.StatusCode);
    }

    // Helpers

    private static string GenerateUniquePhone() {
        return string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
    }

    private static string GenerateUniqueEmail() {
        return $"user{Guid.NewGuid():N}@gmail.com";
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

    private static (string authToken, string emailAddress, string newPhone, string verificationCode) SetupPendingPhoneChangeForEmailUser(TestingMockProvidersContainer container) {
        (string authToken, string emailAddress) = CreateAuthenticatedEmailUser(container);
        string newPhone = GenerateUniquePhone();
        container.WebClient.PostJson("api/userProfile/requestPhoneChange", new { AuthToken = authToken, PhoneNumber = newPhone, CurrentPassword = TestPassword }).EnsureSuccessStatusCode();
        SmsMessage sms = container.SmsProvider.SentMessages.Last();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(sms);
        return (authToken, emailAddress, newPhone, verificationCode);
    }
}
