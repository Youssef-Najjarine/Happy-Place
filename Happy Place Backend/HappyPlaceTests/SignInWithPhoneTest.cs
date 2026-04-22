using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Sms;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SignInWithPhoneTest {
    // Tests - Phone SignIn Happy Path

    [Fact]
    public void SignInWithVerifiedPhoneAndCorrectPasswordReturnsOk() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithVerifiedPhoneReturnsAuthToken() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("verified", signInResult.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(signInResult.GetProperty("authToken").GetString()));
    }

    [Fact]
    public void SignInWithVerifiedPhoneAuthTokenContainsCorrectUserId() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);
        string authTokenString = signInResult.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        var decodedToken = UserAuthenticationToken.ValidateToken(authTokenString);

        Assert.Equal(userAccount.Id.ToString(), decodedToken.Identifier);
    }

    // Tests - Phone SignIn Wrong Credentials

    [Fact]
    public void SignInWithVerifiedPhoneAndWrongPasswordReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithNonExistentPhoneReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithEmptyPhoneReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = "", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithEmptyPasswordForPhoneReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    // Tests - Phone SignIn Pending Account

    [Fact]
    public void SignInWithPendingPhoneAndCorrectPasswordReturnsPendingStatus() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("pending", signInResult.GetProperty("status").GetString());
        Assert.Equal(uniquePhone, signInResult.GetProperty("contact").GetString());
        Assert.Equal("phone", signInResult.GetProperty("contactType").GetString());
    }

    [Fact]
    public void SignInWithPendingPhoneResendsVerificationCode() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });

        Assert.Equal(2, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    [Fact]
    public void SignInWithPendingPhoneAndWrongPasswordReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingPhoneDoesNotResendCodeWhenPasswordIsWrong() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "WrongPassword1!" });

        Assert.Single(testingMockProvidersContainer.SmsProvider.SentMessages);
    }

    [Fact]
    public void SignInWithPendingPhoneResendedCodeWorksForVerification() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });

        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingPhoneOldCodeInvalidatedAfterResend() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage originalSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string originalCode = SmsVerificationNotification.ExtractVerificationCode(originalSms);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = originalCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    // Tests - Phone SignIn Whitespace

    [Fact]
    public void SignInWithVerifiedPhoneTrimsWhitespace() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = $"  {uniquePhone}  ", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingPhoneTrimsWhitespace() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = $"  {uniquePhone}  ", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Phone SignIn Expired Pending Account

    [Fact]
    public void SignInWithPendingExpiredPhoneStillReturnsPendingAndResends() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var pending = dbContext.PendingUserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("pending", signInResult.GetProperty("status").GetString());
        Assert.Equal(2, testingMockProvidersContainer.SmsProvider.SentMessages.Count());
    }

    // Tests - Phone SignIn Full Lifecycle

    [Fact]
    public void PendingPhoneSignInThenVerifyThenSignInReturnsVerified() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        SmsMessage resendSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string newCode = SmsVerificationNotification.ExtractVerificationCode(resendSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = newCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("verified", signInResult.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(signInResult.GetProperty("authToken").GetString()));
    }

    // Tests - Phone SignIn Extreme Input

    [Fact]
    public void SignInWithExtremelyLongPhoneReturnsBadRequest() {
        string longPhone = new('9', 50);
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = longPhone, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithFormattedPhoneNumberReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        string formattedPhone = $"({uniquePhone[..3]}) {uniquePhone[3..6]}-{uniquePhone[6..]}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = formattedPhone, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    // Tests - Cross-Contact Type SignIn

    [Fact]
    public void SignInWithEmailThatWasRegisteredWithPhoneReturnsBadRequest() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = $"{uniquePhone}@gmail.com", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }
}
