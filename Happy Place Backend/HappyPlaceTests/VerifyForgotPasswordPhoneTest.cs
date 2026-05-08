using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyForgotPasswordPhoneTest {
    // Tests - Happy Path

    [Fact]
    public void CorrectCodeReturnsResetToken() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage resetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = resetCode });

        var responseData = response.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(responseData.RootElement.GetProperty("resetToken").GetString());
        Assert.NotEmpty(responseData.RootElement.GetProperty("resetToken").GetString());
    }

    [Fact]
    public void VerificationSetsVerifiedAtAndPersistsResetToken() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage resetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = resetCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.PhoneNumber == uniquePhone);
        Assert.NotNull(resetRequest.VerifiedAt);
        Assert.NotNull(resetRequest.ResetToken);
        Assert.NotEmpty(resetRequest.ResetToken);
    }

    // Tests - Incorrect Code

    [Fact]
    public void IncorrectCodeReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void IncorrectCodeDoesNotSetVerifiedAt() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "000000" });

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.PhoneNumber == uniquePhone);
        Assert.Null(resetRequest.VerifiedAt);
        Assert.Null(resetRequest.ResetToken);
    }

    // Tests - Expiration

    [Fact]
    public void ExpiredCodeReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage resetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var resetRequest = dbContext.PasswordResetRequests.Single(field => field.PhoneNumber == uniquePhone);
            resetRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = resetCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Input Validation

    [Fact]
    public void EmptyPhoneReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = "", VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyVerificationCodeReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PartialCodeReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void CodeWithLettersReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Account Enumeration Prevention (HIPAA/PII/Security)

    [Fact]
    public void NonExistentPhoneReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PendingAccountPhoneReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Replay Prevention

    [Fact]
    public void CodeReusedAfterSuccessReturnsBadRequest() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = uniquePhone }).EnsureSuccessStatusCode();
        SmsMessage resetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = resetCode }).EnsureSuccessStatusCode();
        HttpResponseMessage secondVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = uniquePhone, VerificationCode = resetCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    // Tests - Cross-Account Code Rejection

    [Fact]
    public void CodeFromDifferentAccountReturnsBadRequest() {
        string firstPhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        string secondPhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "First User", PhoneNumber = firstPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage firstSignUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string firstSignUpCode = SmsVerificationNotification.ExtractVerificationCode(firstSignUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = firstPhone, VerificationCode = firstSignUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Second User", PhoneNumber = secondPhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage secondSignUpSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string secondSignUpCode = SmsVerificationNotification.ExtractVerificationCode(secondSignUpSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = secondPhone, VerificationCode = secondSignUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = firstPhone }).EnsureSuccessStatusCode();
        SmsMessage firstResetSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string firstResetCode = SmsVerificationNotification.ExtractVerificationCode(firstResetSms);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = secondPhone, VerificationCode = firstResetCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
