using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class AccountVerifiedNotificationTest {
    // Fields
    private const string SixDigitPattern = @"\b(\d{6})\b";

    // Tests - Email Confirmation Sent On Success

    [Fact]
    public void SuccessfulEmailVerificationSendsConfirmationEmail() {
        string uniqueEmail = $"confirm{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        Assert.Equal(2, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void EmailConfirmationGoesToCorrectRecipient() {
        string uniqueEmail = $"recipient{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        MailMessage confirmationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        var toAddressesProp = confirmationEmail.GetType().GetProperty("ToAddresses");
        var toAddresses = toAddressesProp?.GetValue(confirmationEmail) as IList<String>;

        Assert.Contains(uniqueEmail, toAddresses);
    }

    [Fact]
    public void EmailConfirmationContainsUserDisplayName() {
        string uniqueEmail = $"name{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        MailMessage confirmationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        var bodyProp = confirmationEmail.GetType().GetProperty("BodyText");
        string body = bodyProp?.GetValue(confirmationEmail) as string ?? string.Empty;

        Assert.Contains("Timmy Turner", body);
    }

    [Fact]
    public void EmailConfirmationDoesNotContainVerificationCode() {
        string uniqueEmail = $"nocode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        MailMessage confirmationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        var bodyProp = confirmationEmail.GetType().GetProperty("BodyText");
        string body = bodyProp?.GetValue(confirmationEmail) as string ?? string.Empty;

        Assert.DoesNotMatch(SixDigitPattern, body);
    }

    [Fact]
    public void EmailConfirmationHasCorrectSubject() {
        string uniqueEmail = $"subject{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code }).EnsureSuccessStatusCode();

        MailMessage confirmationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();

        Assert.Equal("Welcome to Happy Place!", confirmationEmail.Subject);
    }

    // Tests - Email Confirmation NOT Sent On Failure

    [Fact]
    public void WrongEmailCodeDoesNotSendConfirmation() {
        string uniqueEmail = $"wrongcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "000000" });

        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    [Fact]
    public void ExpiredEmailCodeDoesNotSendConfirmation() {
        string uniqueEmail = $"expired{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContext = HappyPlaceDbContext.Create();
        var pending = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = code });

        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    [Fact]
    public void NonExistentEmailDoesNotSendConfirmation() {
        string uniqueEmail = $"noexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "123456" });

        Assert.Empty(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    // Tests - Email Confirmation After Resend

    [Fact]
    public void ResendThenVerifyEmailSendsExactlyThreeEmails() {
        string uniqueEmail = $"resend{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Timmy Turner", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/resendEmailCode", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode }).EnsureSuccessStatusCode();

        Assert.Equal(3, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    // Tests - Phone Confirmation Sent On Success

    [Fact]
    public void SuccessfulPhoneVerificationSendsConfirmationSms() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code }).EnsureSuccessStatusCode();

        Assert.Equal(2, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    [Fact]
    public void PhoneConfirmationGoesToCorrectRecipient() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code }).EnsureSuccessStatusCode();

        SmsMessage confirmationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();

        Assert.Equal(uniquePhone, confirmationSms.ToPhoneNumber);
    }

    [Fact]
    public void PhoneConfirmationContainsUserDisplayName() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code }).EnsureSuccessStatusCode();

        SmsMessage confirmationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();

        Assert.Contains("Timmy Turner", confirmationSms.BodyText);
    }

    [Fact]
    public void PhoneConfirmationDoesNotContainVerificationCode() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code }).EnsureSuccessStatusCode();

        SmsMessage confirmationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();

        Assert.DoesNotMatch(SixDigitPattern, confirmationSms.BodyText);
    }

    // Tests - Phone Confirmation NOT Sent On Failure

    [Fact]
    public void WrongPhoneCodeDoesNotSendConfirmation() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "000000" });

        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    [Fact]
    public void ExpiredPhoneCodeDoesNotSendConfirmation() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string code = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContext = HappyPlaceDbContext.Create();
        var pending = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = code });

        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    [Fact]
    public void NonExistentPhoneDoesNotSendConfirmation() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = "123456" });

        Assert.Empty(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    // Tests - Phone Confirmation After Resend

    [Fact]
    public void ResendThenVerifyPhoneSendsExactlyThreeSms() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { Name = "Timmy Turner", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/resendPhoneCode", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode }).EnsureSuccessStatusCode();

        Assert.Equal(3, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }
}
