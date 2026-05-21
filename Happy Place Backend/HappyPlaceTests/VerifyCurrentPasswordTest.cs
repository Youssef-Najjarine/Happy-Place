using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyCurrentPasswordTest {
    // Tests - Happy Path

    [Fact]
    public void CorrectPasswordReturnsIsValidTrue() {
        string uniqueEmail = $"verpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = authToken, Password = "Seven74!" });
        bool isValid = response.ReadContentAsJsonDocument().RootElement.GetProperty("isValid").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(isValid);
    }

    [Fact]
    public void WrongPasswordReturnsIsValidFalse() {
        string uniqueEmail = $"verpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = authToken, Password = "WrongPassword1!" });
        bool isValid = response.ReadContentAsJsonDocument().RootElement.GetProperty("isValid").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(isValid);
    }

    [Fact]
    public void EmptyPasswordReturnsIsValidFalse() {
        string uniqueEmail = $"verpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = authToken, Password = "" });
        bool isValid = response.ReadContentAsJsonDocument().RootElement.GetProperty("isValid").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(isValid);
    }

    [Fact]
    public void MissingPasswordFieldReturnsIsValidFalse() {
        string uniqueEmail = $"verpw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Test User", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = authToken });
        bool isValid = response.ReadContentAsJsonDocument().RootElement.GetProperty("isValid").GetBoolean();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(isValid);
    }

    // Tests - Auth Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = "", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { AuthToken = "not-a-real-token", Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/verifyCurrentPassword", new { Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
