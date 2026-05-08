using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ForgotPasswordWithPhoneTest {
    // Tests - Happy Path

    [Fact]
    public void VerifiedAccountReceivesResetCodeSms() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        int smsCountBeforeForgotPassword = testingMockProvidersContainer.SmsProvider.SentMessages.Count();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(smsCountBeforeForgotPassword + 1, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    [Fact]
    public void ResetCodeSmsContainsSixDigitCode() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage resetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);
        Assert.Matches(@"^\d{6}$", resetCode);
    }

    [Fact]
    public void ResetCodeIsStoredInPasswordResetRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.PhoneNumber == uniquePhone);
        Assert.NotNull(resetRequest.VerificationCode);
        Assert.Equal(6, resetRequest.VerificationCode.Length);
        Assert.Null(resetRequest.ResetToken);
        Assert.Null(resetRequest.VerifiedAt);
        Assert.Null(resetRequest.UsedAt);
    }

    [Fact]
    public void ResetRequestExpiresAfterTenMinutes() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.PhoneNumber == uniquePhone);
        double minutesUntilExpiry = (resetRequest.ExpiresAt - resetRequest.CreatedAt).TotalMinutes;
        Assert.Equal(10, minutesUntilExpiry, 0.1);
    }

    // Tests - Account Enumeration Prevention (HIPAA/PII/Security)

    [Fact]
    public void NonExistentPhoneReturnsSuccessToPreventEnumeration() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void NonExistentPhoneDoesNotSendSms() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        Assert.Empty(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    [Fact]
    public void NonExistentPhoneDoesNotCreateResetRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(field => field.PhoneNumber == uniquePhone);
        Assert.Null(resetRequest);
    }

    [Fact]
    public void PendingAccountReturnsSuccessToPreventEnumeration() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PendingAccountDoesNotSendResetSms() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        int smsCountAfterSignUp = testingMockProvidersContainer.SmsProvider.SentMessages.Count();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        Assert.Equal(smsCountAfterSignUp, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    [Fact]
    public void PendingAccountDoesNotCreateResetRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(field => field.PhoneNumber == uniquePhone);
        Assert.Null(resetRequest);
    }

    // Tests - Phone Validation

    [Fact]
    public void EmptyPhoneReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyPhoneReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneShorterThanTenDigitsReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PhoneExceedingMaxLengthReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = string.Concat(Enumerable.Repeat("1", 21)) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Input Normalization

    [Fact]
    public void PhoneWithLeadingTrailingWhitespaceIsTrimmed() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        int smsCountBeforeForgotPassword = testingMockProvidersContainer.SmsProvider.SentMessages.Count();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = $"  {uniquePhone}  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(smsCountBeforeForgotPassword + 1, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    // Tests - Code Replacement

    [Fact]
    public void NewRequestInvalidatesPreviousCode() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage firstResetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string oldResetCode = SmsVerificationNotification.ExtractVerificationCode(firstResetSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = oldResetCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void NewCodeWorksAfterReplacingOldCode() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        SmsMessage latestResetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newResetCode = SmsVerificationNotification.ExtractVerificationCode(latestResetSms);

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = newResetCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("resetToken").GetString());
    }

    [Fact]
    public void EachRequestGeneratesDifferentCode() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage firstResetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string firstCode = SmsVerificationNotification.ExtractVerificationCode(firstResetSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage secondResetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string secondCode = SmsVerificationNotification.ExtractVerificationCode(secondResetSms);

        Assert.NotEqual(firstCode, secondCode);
    }
}
