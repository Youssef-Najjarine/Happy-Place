using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ResendVerificationCodeTest {
    // Tests - Email Resend Happy Path

    [Fact]
    public void ResendEmailCodeReturnsOk() {
        string uniqueEmail = $"resend{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    [Fact]
    public void NewCodeWorksAfterEmailResend() {
        string uniqueEmail = $"newcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void OldCodeFailsAfterEmailResend() {
        string uniqueEmail = $"oldcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage originalEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string oldCode = EmailVerificationNotification.ExtractVerificationCode(originalEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = oldCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void OnlyLatestCodeWorksAfterMultipleEmailResends() {
        string uniqueEmail = $"multi{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.EmailProvider.EmailMessages.Last());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.EmailProvider.EmailMessages.Last());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        string code3 = EmailVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.EmailProvider.EmailMessages.Last());

        HttpResponseMessage verify1 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code1 });
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code2 });
        HttpResponseMessage verify3 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code3 });

        Assert.Equal(HttpStatusCode.BadRequest, verify1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, verify2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verify3.StatusCode);
    }

    // Tests - Email Resend Expiration

    [Fact]
    public void EmailResendResetsExpirationTimer() {
        string uniqueEmail = $"timer{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextAge = HappyPlaceDbContext.Create();
        var pendingBefore = dbContextAge.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingBefore.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-50);
        dbContextAge.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContextCheck = HappyPlaceDbContext.Create();
        var pendingAfter = dbContextCheck.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.True((DateTime.UtcNow - pendingAfter.CreatedAtUtc).TotalSeconds < 5);
    }

    [Fact]
    public void ResendWorksAfterEmailCodeExpires() {
        string uniqueEmail = $"expired{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextExpire = HappyPlaceDbContext.Create();
        var pending = dbContextExpire.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContextExpire.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    // Tests - Email Resend Security

    [Fact]
    public void ResendEmailCodeForNonExistentEmailReturnsOk() {
        string uniqueEmail = $"noexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    [Fact]
    public void ResendEmailCodeForNonExistentEmailSendsNoNotification() {
        string uniqueEmail = $"nonotif{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail });

        Assert.Empty(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    [Fact]
    public void ResendEmailCodeAfterVerificationReturnsOk() {
        string uniqueEmail = $"afterverify{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    // Tests - Email Resend Data Integrity

    [Fact]
    public void EmailResendDoesNotDuplicatePendingRecord() {
        string uniqueEmail = $"nodup{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        int pendingCount = dbContext.PendingUserAccounts.Count(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(1, pendingCount);
    }

    [Fact]
    public void EmailResendPreservesAccountData() {
        string uniqueEmail = $"preserve{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextBefore = HappyPlaceDbContext.Create();
        var pendingBefore = dbContextBefore.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        string originalUsername = pendingBefore.Username;
        string originalDisplayName = pendingBefore.DisplayName;
        string originalHashedPassword = pendingBefore.HashedPassword;
        string originalCode = pendingBefore.VerificationCode;

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        var pendingAfter = dbContextAfter.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(uniqueEmail, pendingAfter.EmailAddress);
        Assert.Equal(originalUsername, pendingAfter.Username);
        Assert.Equal(originalDisplayName, pendingAfter.DisplayName);
        Assert.Equal(originalHashedPassword, pendingAfter.HashedPassword);
        Assert.NotEqual(originalCode, pendingAfter.VerificationCode);
    }

    [Fact]
    public void EmailResendGeneratesNewCode() {
        string uniqueEmail = $"newgen{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage originalEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string originalCode = EmailVerificationNotification.ExtractVerificationCode(originalEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pending = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(newCode, pending.VerificationCode);
    }

    [Fact]
    public void EmailResendSendsExactlyOneAdditionalNotification() {
        string uniqueEmail = $"count{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        Assert.Equal(2, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        Assert.Equal(3, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void PasswordMatchesAfterEmailResendAndVerify() {
        string uniqueEmail = $"pwresend{Guid.NewGuid():N}@gmail.com";
        string originalPassword = "Seven74!";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = originalPassword }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.True(PasswordHasher.VerifyPassword(originalPassword, userAccount.HashedPassword));
    }

    // Tests - Phone Resend Happy Path

    [Fact]
    public void ResendPhoneCodeReturnsOk() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    [Fact]
    public void NewCodeWorksAfterPhoneResend() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void OldCodeFailsAfterPhoneResend() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage originalSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string oldCode = SmsVerificationNotification.ExtractVerificationCode(originalSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = oldCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void OnlyLatestCodeWorksAfterMultiplePhoneResends() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        string code1 = SmsVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.SmsProvider.SentMessages.Last());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        string code2 = SmsVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.SmsProvider.SentMessages.Last());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        string code3 = SmsVerificationNotification.ExtractVerificationCode(testingMockProvidersContainer.SmsProvider.SentMessages.Last());

        HttpResponseMessage verify1 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code1 });
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code2 });
        HttpResponseMessage verify3 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code3 });

        Assert.Equal(HttpStatusCode.BadRequest, verify1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, verify2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verify3.StatusCode);
    }

    // Tests - Phone Resend Expiration

    [Fact]
    public void PhoneResendResetsExpirationTimer() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextAge = HappyPlaceDbContext.Create();
        var pendingBefore = dbContextAge.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pendingBefore.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-9).AddSeconds(-50);
        dbContextAge.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContextCheck = HappyPlaceDbContext.Create();
        var pendingAfter = dbContextCheck.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.True((DateTime.UtcNow - pendingAfter.CreatedAtUtc).TotalSeconds < 5);
    }

    [Fact]
    public void ResendWorksAfterPhoneCodeExpires() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextExpire = HappyPlaceDbContext.Create();
        var pending = dbContextExpire.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContextExpire.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    // Tests - Phone Resend Security

    [Fact]
    public void ResendPhoneCodeForNonExistentPhoneReturnsOk() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    [Fact]
    public void ResendPhoneCodeForNonExistentPhoneSendsNoNotification() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone });

        Assert.Empty(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    [Fact]
    public void ResendPhoneCodeAfterVerificationReturnsOk() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code }).EnsureSuccessStatusCode();

        HttpResponseMessage resendResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
    }

    // Tests - Phone Resend Data Integrity

    [Fact]
    public void PhoneResendDoesNotDuplicatePendingRecord() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        int pendingCount = dbContext.PendingUserAccounts.Count(field => field.PhoneNumber == uniquePhone);

        Assert.Equal(1, pendingCount);
    }

    [Fact]
    public void PhoneResendPreservesAccountData() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContextBefore = HappyPlaceDbContext.Create();
        var pendingBefore = dbContextBefore.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        string originalUsername = pendingBefore.Username;
        string originalDisplayName = pendingBefore.DisplayName;
        string originalHashedPassword = pendingBefore.HashedPassword;
        string originalCode = pendingBefore.VerificationCode;

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        var pendingAfter = dbContextAfter.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.Equal(uniquePhone, pendingAfter.PhoneNumber);
        Assert.Equal(originalUsername, pendingAfter.Username);
        Assert.Equal(originalDisplayName, pendingAfter.DisplayName);
        Assert.Equal(originalHashedPassword, pendingAfter.HashedPassword);
        Assert.NotEqual(originalCode, pendingAfter.VerificationCode);
    }

    [Fact]
    public void PhoneResendSendsExactlyOneAdditionalNotification() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        Assert.Equal(2, testingMockProvidersContainer.SmsProvider.SentMessages.Count());

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        Assert.Equal(3, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    [Fact]
    public void PasswordMatchesAfterPhoneResendAndVerify() {
        string uniquePhone = new string(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10).ToArray());
        string originalPassword = "Seven74!";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = originalPassword }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.True(PasswordHasher.VerifyPassword(originalPassword, userAccount.HashedPassword));
    }
}
