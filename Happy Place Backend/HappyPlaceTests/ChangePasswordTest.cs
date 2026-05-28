using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChangePasswordTest {
    // Tests - Happy Path

    [Fact]
    public void CorrectCurrentPasswordAndValidNewPasswordReturnsOk() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NewPass1!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void CanSignInWithNewPasswordAfterChange() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NewPass1!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = uniqueEmail, Password = "NewPass1!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void CannotSignInWithOldPasswordAfterChange() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NewPass1!" }).EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void PasswordNotChangedOnValidationFailure() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "short" });

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Current Password Validation

    [Fact]
    public void WrongCurrentPasswordReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "WrongPassword1!", NewPassword = "NewPass1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyCurrentPasswordReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "", NewPassword = "NewPass1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - New Password Criteria

    [Fact]
    public void NewPasswordTooShortReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "Short1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NewPasswordWithoutNumberReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NoNumbers!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NewPasswordWithoutUppercaseReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "nouppercase1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NewPasswordWithoutLowercaseReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NOLOWERCASE1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void NewPasswordWithoutSpecialCharReturnsError() {
        string uniqueEmail = $"chpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = authToken, CurrentPassword = "Seven74!", NewPassword = "NoSpecial1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Auth Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = "", CurrentPassword = "Seven74!", NewPassword = "NewPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/changePassword", new { AuthToken = "not-a-real-token", CurrentPassword = "Seven74!", NewPassword = "NewPass1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
