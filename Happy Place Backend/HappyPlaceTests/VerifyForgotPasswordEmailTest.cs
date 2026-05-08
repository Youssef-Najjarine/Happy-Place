using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerifyForgotPasswordEmailTest {
    // Tests - Happy Path

    [Fact]
    public void CorrectCodeReturnsResetToken() {
        string uniqueEmail = $"vfpe{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage resetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = resetCode });

        var responseData = response.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(responseData.RootElement.GetProperty("resetToken").GetString());
        Assert.NotEmpty(responseData.RootElement.GetProperty("resetToken").GetString());
    }

    [Fact]
    public void VerificationSetsVerifiedAtAndPersistsResetToken() {
        string uniqueEmail = $"vfpset{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage resetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = resetCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
        Assert.NotNull(resetRequest.VerifiedAt);
        Assert.NotNull(resetRequest.ResetToken);
        Assert.NotEmpty(resetRequest.ResetToken);
    }

    // Tests - Incorrect Code

    [Fact]
    public void IncorrectCodeReturnsBadRequest() {
        string uniqueEmail = $"vfpwrong{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void IncorrectCodeDoesNotSetVerifiedAt() {
        string uniqueEmail = $"vfpnoset{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "000000" });

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
        Assert.Null(resetRequest.VerifiedAt);
        Assert.Null(resetRequest.ResetToken);
    }

    // Tests - Expiration

    [Fact]
    public void ExpiredCodeReturnsBadRequest() {
        string uniqueEmail = $"vfpexp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage resetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
            resetRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = resetCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Input Validation

    [Fact]
    public void EmptyEmailReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = "", VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyVerificationCodeReturnsBadRequest() {
        string uniqueEmail = $"vfpemptycode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PartialCodeReturnsBadRequest() {
        string uniqueEmail = $"vfppartial{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void CodeWithLettersReturnsBadRequest() {
        string uniqueEmail = $"vfpletters{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "abcdef" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Account Enumeration Prevention (HIPAA/PII/Security)

    [Fact]
    public void NonExistentEmailReturnsBadRequest() {
        string uniqueEmail = $"vfpnoexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PendingAccountEmailReturnsBadRequest() {
        string uniqueEmail = $"vfppending{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = "123456" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Replay Prevention

    [Fact]
    public void CodeReusedAfterSuccessReturnsBadRequest() {
        string uniqueEmail = $"vfpreplay{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage resetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = resetCode }).EnsureSuccessStatusCode();
        HttpResponseMessage secondVerify = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = resetCode });

        Assert.Equal(HttpStatusCode.BadRequest, secondVerify.StatusCode);
    }

    // Tests - Cross-Account Code Rejection

    [Fact]
    public void CodeFromDifferentAccountReturnsBadRequest() {
        string firstEmail = $"vfpfirst{Guid.NewGuid():N}@gmail.com";
        string secondEmail = $"vfpsecond{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "First User", Email = firstEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage firstSignUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string firstSignUpCode = EmailVerificationNotification.ExtractVerificationCode(firstSignUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = firstEmail, VerificationCode = firstSignUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Second User", Email = secondEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage secondSignUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string secondSignUpCode = EmailVerificationNotification.ExtractVerificationCode(secondSignUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = secondEmail, VerificationCode = secondSignUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = firstEmail }).EnsureSuccessStatusCode();
        MailMessage firstResetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string firstResetCode = EmailVerificationNotification.ExtractVerificationCode(firstResetEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = secondEmail, VerificationCode = firstResetCode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
