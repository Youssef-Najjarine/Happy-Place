using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SignInWithEmailTest {
    // Tests - Email SignIn Happy Path

    [Fact]
    public void SignInWithVerifiedEmailAndCorrectPasswordReturnsOk() {
        string uniqueEmail = $"signin{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithVerifiedEmailReturnsAuthToken() {
        string uniqueEmail = $"token{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal("verified", signInResult.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(signInResult.GetProperty("authToken").GetString()));
    }

    [Fact]
    public void SignInWithVerifiedEmailAuthTokenContainsCorrectUserId() {
        string uniqueEmail = $"userid{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);
        string authTokenString = signInResult.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        var decodedToken = UserAuthenticationToken.ValidateToken(authTokenString);

        Assert.Equal(userAccount.Id.ToString(), decodedToken.Identifier);
    }

    [Fact]
    public void SignInImmediatelyAfterVerificationSucceeds() {
        string uniqueEmail = $"immediate{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();
        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Email SignIn Wrong Credentials

    [Fact]
    public void SignInWithVerifiedEmailAndWrongPasswordReturnsBadRequest() {
        string uniqueEmail = $"wrongpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithNonExistentEmailReturnsBadRequest() {
        string uniqueEmail = $"noexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithEmptyEmailReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = "", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithEmptyPasswordForEmailReturnsBadRequest() {
        string uniqueEmail = $"emptypw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    // Tests - Email SignIn Pending Account

    [Fact]
    public void SignInWithPendingEmailAndCorrectPasswordReturnsPendingStatus() {
        string uniqueEmail = $"pending{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("pending", signInResult.GetProperty("status").GetString());
        Assert.Equal(uniqueEmail, signInResult.GetProperty("contact").GetString());
        Assert.Equal("email", signInResult.GetProperty("contactType").GetString());
    }

    [Fact]
    public void SignInWithPendingEmailResendsVerificationCode() {
        string uniqueEmail = $"resend{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(2, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void SignInWithPendingEmailAndWrongPasswordReturnsBadRequest() {
        string uniqueEmail = $"pendingwrong{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingEmailDoesNotResendCodeWhenPasswordIsWrong() {
        string uniqueEmail = $"noresend{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "WrongPassword1!" });

        Assert.Single(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    [Fact]
    public void SignInWithPendingEmailResendedCodeWorksForVerification() {
        string uniqueEmail = $"resendverify{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingEmailOldCodeInvalidatedAfterResend() {
        string uniqueEmail = $"oldcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage originalEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string originalCode = EmailVerificationNotification.ExtractVerificationCode(originalEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = originalCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingEmailAfterReSignUpUsesLatestPassword() {
        string uniqueEmail = $"resignup{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "OldPass1!" }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "NewPass2!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInWithOldPassword = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "OldPass1!" });
        HttpResponseMessage signInWithNewPassword = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "NewPass2!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInWithOldPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signInWithNewPassword.StatusCode);
    }

    // Tests - Email SignIn Security

    [Fact]
    public void SignInErrorForWrongPasswordAndNonExistentAccountAreIdentical() {
        string verifiedEmail = $"verified{Guid.NewGuid():N}@gmail.com";
        string nonExistentEmail = $"noexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = verifiedEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = verifiedEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage wrongPasswordResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = verifiedEmail, Password = "WrongPassword1!" });
        HttpResponseMessage nonExistentResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = nonExistentEmail, Password = "Seven74!" });

        Assert.Equal(wrongPasswordResponse.StatusCode, nonExistentResponse.StatusCode);
    }

    [Fact]
    public void MultipleFailedSignInAttemptsStillSucceedOnCorrectPassword() {
        string uniqueEmail = $"retry{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Wrong1!" });
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Wrong2!" });
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Wrong3!" });

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Email SignIn Case Sensitivity and Whitespace

    [Fact]
    public void SignInWithVerifiedEmailIsCaseInsensitive() {
        string uniqueEmail = $"CaseTest{Guid.NewGuid():N}@Gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail.ToLower(), Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingEmailIsCaseInsensitive() {
        string uniqueEmail = $"PendCase{Guid.NewGuid():N}@Gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail.ToLower(), Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithVerifiedEmailTrimsWhitespace() {
        string uniqueEmail = $"trim{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = $"  {uniqueEmail}  ", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithPendingEmailTrimsWhitespace() {
        string uniqueEmail = $"trimPend{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = $"  {uniqueEmail}  ", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Email SignIn Expired Pending Account

    [Fact]
    public void SignInWithPendingExpiredEmailStillReturnsPendingAndResends() {
        string uniqueEmail = $"expired{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var pending = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pending.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContext.SaveChanges();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("pending", signInResult.GetProperty("status").GetString());
        Assert.Equal(2, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    // Tests - Email SignIn Full Lifecycle

    [Fact]
    public void PendingSignInThenVerifyThenSignInReturnsVerified() {
        string uniqueEmail = $"lifecycle{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        MailMessage resendEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newCode = EmailVerificationNotification.ExtractVerificationCode(resendEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = newCode }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var signInResponseStream = signInResponse.Content.ReadAsStream();
        using var signInResponseReader = new StreamReader(signInResponseStream);
        string responseBody = signInResponseReader.ReadToEnd();
        var signInResult = JsonSerializer.Deserialize<JsonElement>(responseBody);

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("verified", signInResult.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(signInResult.GetProperty("authToken").GetString()));
    }

    [Fact]
    public void MultipleSuccessfulSignInsProduceDifferentTokens() {
        string uniqueEmail = $"multitoken{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        HttpResponseMessage firstSignIn = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var firstStream = firstSignIn.Content.ReadAsStream();
        using var firstReader = new StreamReader(firstStream);
        string firstBody = firstReader.ReadToEnd();
        string firstToken = JsonSerializer.Deserialize<JsonElement>(firstBody).GetProperty("authToken").GetString();

        HttpResponseMessage secondSignIn = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });
        using var secondStream = secondSignIn.Content.ReadAsStream();
        using var secondReader = new StreamReader(secondStream);
        string secondBody = secondReader.ReadToEnd();
        string secondToken = JsonSerializer.Deserialize<JsonElement>(secondBody).GetProperty("authToken").GetString();

        Assert.NotEqual(firstToken, secondToken);
    }

    [Fact]
    public void SignInDoesNotModifyUserAccountData() {
        string uniqueEmail = $"nomodify{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContextBefore = HappyPlaceDbContext.Create();
        var accountBefore = dbContextBefore.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        string usernameBefore = accountBefore.Username;
        string displayNameBefore = accountBefore.DisplayName;
        string hashedPasswordBefore = accountBefore.HashedPassword;

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        using var dbContextAfter = HappyPlaceDbContext.Create();
        var accountAfter = dbContextAfter.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(usernameBefore, accountAfter.Username);
        Assert.Equal(displayNameBefore, accountAfter.DisplayName);
        Assert.Equal(hashedPasswordBefore, accountAfter.HashedPassword);
        Assert.Equal(uniqueEmail, accountAfter.EmailAddress);
    }

    // Tests - Email SignIn Extreme Input

    [Fact]
    public void SignInWithExtremelyLongEmailReturnsBadRequest() {
        string longEmail = new string('a', 500) + "@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = longEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SignInWithExtremelyLongPasswordReturnsBadRequest() {
        string uniqueEmail = $"longpw{Guid.NewGuid():N}@gmail.com";
        string longPassword = new string('A', 10000) + "1!a";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = longPassword });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }
}
