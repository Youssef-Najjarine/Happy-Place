using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ValidateTokenTest {
    // Tests - Valid Token Returns User Data

    [Fact]
    public void ValidTokenFromEmailSignInReturnsOk() {
        string uniqueEmail = $"valid{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact]
    public void ValidTokenFromPhoneSignInReturnsOk() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact]
    public void ValidTokenFromEmailVerificationReturnsOk() {
        string uniqueEmail = $"verify{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        using var verifyStream = verifyResponse.Content.ReadAsStream();
        using var verifyReader = new StreamReader(verifyStream);
        string verifyBody = verifyReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(verifyBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact]
    public void ValidTokenFromPhoneVerificationReturnsOk() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        using var verifyStream = verifyResponse.Content.ReadAsStream();
        using var verifyReader = new StreamReader(verifyStream);
        string verifyBody = verifyReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(verifyBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    // Tests - Valid Token Returns Correct User Data

    [Fact]
    public void ValidTokenReturnsCorrectUserId() {
        string uniqueEmail = $"userid{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });
        using var validateStream = validateResponse.Content.ReadAsStream();
        using var validateReader = new StreamReader(validateStream);
        string validateBody = validateReader.ReadToEnd();
        var userData = JsonSerializer.Deserialize<JsonElement>(validateBody);

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(userAccount.Id.ToString(), userData.GetProperty("userId").GetString());
    }

    [Fact]
    public void ValidTokenReturnsCorrectDisplayName() {
        string uniqueEmail = $"name{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });
        using var validateStream = validateResponse.Content.ReadAsStream();
        using var validateReader = new StreamReader(validateStream);
        string validateBody = validateReader.ReadToEnd();
        var userData = JsonSerializer.Deserialize<JsonElement>(validateBody);

        Assert.Equal("Youssef Najjarine", userData.GetProperty("displayName").GetString());
    }

    [Fact]
    public void ValidTokenReturnsCorrectUsername() {
        string uniqueEmail = $"uname{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });
        using var validateStream = validateResponse.Content.ReadAsStream();
        using var validateReader = new StreamReader(validateStream);
        string validateBody = validateReader.ReadToEnd();
        var userData = JsonSerializer.Deserialize<JsonElement>(validateBody);

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(userAccount.Username, userData.GetProperty("username").GetString());
    }

    [Fact]
    public void ValidTokenForEmailUserReturnsEmailAddress() {
        string uniqueEmail = $"emailfield{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });
        using var validateStream = validateResponse.Content.ReadAsStream();
        using var validateReader = new StreamReader(validateStream);
        string validateBody = validateReader.ReadToEnd();
        var userData = JsonSerializer.Deserialize<JsonElement>(validateBody);

        Assert.Equal(uniqueEmail, userData.GetProperty("emailAddress").GetString());
    }

    [Fact]
    public void ValidTokenForPhoneUserReturnsPhoneNumber() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });
        using var validateStream = validateResponse.Content.ReadAsStream();
        using var validateReader = new StreamReader(validateStream);
        string validateBody = validateReader.ReadToEnd();
        var userData = JsonSerializer.Deserialize<JsonElement>(validateBody);

        Assert.Equal(uniquePhone, userData.GetProperty("phoneNumber").GetString());
    }

    // Tests - Multiple Tokens

    [Fact]
    public void MultipleValidTokensForSameUserAllValidate() {
        string uniqueEmail = $"multi{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage firstSignIn = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var firstStream = firstSignIn.Content.ReadAsStream();
        using var firstReader = new StreamReader(firstStream);
        string firstToken = JsonSerializer.Deserialize<JsonElement>(firstReader.ReadToEnd()).GetProperty("authToken").GetString();

        HttpResponseMessage secondSignIn = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var secondStream = secondSignIn.Content.ReadAsStream();
        using var secondReader = new StreamReader(secondStream);
        string secondToken = JsonSerializer.Deserialize<JsonElement>(secondReader.ReadToEnd()).GetProperty("authToken").GetString();

        HttpResponseMessage firstValidate = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = firstToken });
        HttpResponseMessage secondValidate = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = secondToken });

        Assert.Equal(HttpStatusCode.OK, firstValidate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondValidate.StatusCode);
    }

    // Tests - Invalid Tokens

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void GarbageTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = "this-is-not-a-valid-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void RandomBase64TokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string fakeBase64 = Convert.ToBase64String(new byte[64]);

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = fakeBase64 });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void ExpiredTokenReturnsUnauthorized() {
        string uniqueEmail = $"expired{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        var decodedToken = UserAuthenticationToken.ValidateToken(authToken);
        decodedToken.ExpirationDateUtc = DateTimeOffset.UtcNow.AddDays(-1);
        string expiredTokenString = decodedToken.ToAuthTokenString();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = expiredTokenString });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void TokenForDeletedUserReturnsUnauthorized() {
        string uniqueEmail = $"deleted{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        dbContext.UserAccounts.Remove(userAccount);
        dbContext.SaveChanges();

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void TamperedTokenReturnsUnauthorized() {
        string uniqueEmail = $"tamper{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInStream = signInResponse.Content.ReadAsStream();
        using var signInReader = new StreamReader(signInStream);
        string signInBody = signInReader.ReadToEnd();
        string authToken = JsonSerializer.Deserialize<JsonElement>(signInBody).GetProperty("authToken").GetString();

        string tamperedToken = authToken[..^5] + "AAAAA";

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = tamperedToken });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }

    [Fact]
    public void ExtremelyLongTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string longToken = Convert.ToBase64String(new byte[100000]);

        HttpResponseMessage validateResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/validateToken", new { AuthToken = longToken });

        Assert.Equal(HttpStatusCode.Unauthorized, validateResponse.StatusCode);
    }
}
