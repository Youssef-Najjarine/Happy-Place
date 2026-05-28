using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ForgotPasswordWithEmailTest {
    // Tests - Happy Path

    [Fact]
    public void VerifiedAccountReceivesResetCodeEmail() {
        string uniqueEmail = $"fpemail{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        int emailCountBeforeForgotPassword = testingMockProvidersContainer.EmailProvider.EmailMessages.Count();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(emailCountBeforeForgotPassword + 1, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void ResetCodeEmailContainsSixDigitCode() {
        string uniqueEmail = $"fpcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage resetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);
        Assert.Matches(@"^\d{6}$", resetCode);
    }

    [Fact]
    public void ResetCodeIsStoredInPasswordResetRequest() {
        string uniqueEmail = $"fpstored{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
        Assert.NotNull(resetRequest.VerificationCode);
        Assert.Equal(6, resetRequest.VerificationCode.Length);
        Assert.Null(resetRequest.ResetToken);
        Assert.Null(resetRequest.VerifiedAt);
        Assert.Null(resetRequest.UsedAt);
    }

    [Fact]
    public void ResetRequestExpiresAfterTenMinutes() {
        string uniqueEmail = $"fpexpiry{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
        double minutesUntilExpiry = (resetRequest.ExpiresAt - resetRequest.CreatedAt).TotalMinutes;
        Assert.Equal(10, minutesUntilExpiry, 0.1);
    }

    // Tests - Account Enumeration Prevention (HIPAA/PII/Security)

    [Fact]
    public void NonExistentEmailReturnsSuccessToPreventEnumeration() {
        string uniqueEmail = $"fpnoexist{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void NonExistentEmailDoesNotSendEmail() {
        string uniqueEmail = $"fpnomail{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        Assert.Empty(testingMockProvidersContainer.EmailProvider.EmailMessages);
    }

    [Fact]
    public void NonExistentEmailDoesNotCreateResetRequest() {
        string uniqueEmail = $"fpnodb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(field => field.EmailAddress == uniqueEmail);
        Assert.Null(resetRequest);
    }

    [Fact]
    public void PendingAccountReturnsSuccessToPreventEnumeration() {
        string uniqueEmail = $"fppending{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PendingAccountDoesNotSendResetEmail() {
        string uniqueEmail = $"fppendnomail{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        int emailCountAfterSignUp = testingMockProvidersContainer.EmailProvider.EmailMessages.Count();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        Assert.Equal(emailCountAfterSignUp, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void PendingAccountDoesNotCreateResetRequest() {
        string uniqueEmail = $"fppendnodb{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.SingleOrDefault(field => field.EmailAddress == uniqueEmail);
        Assert.Null(resetRequest);
    }

    // Tests - Email Validation

    [Fact]
    public void EmptyEmailReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyEmailReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void InvalidEmailFormatReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = "notanemail" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmailExceedingMaxLengthReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = new string('a', 246) + "@gmail.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Input Normalization

    [Fact]
    public void EmailWithLeadingTrailingWhitespaceIsTrimmed() {
        string uniqueEmail = $"fptrim{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        int emailCountBeforeForgotPassword = testingMockProvidersContainer.EmailProvider.EmailMessages.Count();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = $"  {uniqueEmail}  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(emailCountBeforeForgotPassword + 1, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    [Fact]
    public void EmailLookupIsCaseInsensitive() {
        string uniqueEmail = $"fpcase{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        int emailCountBeforeForgotPassword = testingMockProvidersContainer.EmailProvider.EmailMessages.Count();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail.ToUpper() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(emailCountBeforeForgotPassword + 1, testingMockProvidersContainer.EmailProvider.EmailMessages.Count());
    }

    // Tests - Code Replacement

    [Fact]
    public void NewRequestInvalidatesPreviousCode() {
        string uniqueEmail = $"fpinvalold{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage firstResetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string oldResetCode = EmailVerificationNotification.ExtractVerificationCode(firstResetEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = oldResetCode });

        Assert.Equal(HttpStatusCode.BadRequest, verifyResponse.StatusCode);
    }

    [Fact]
    public void NewCodeWorksAfterReplacingOldCode() {
        string uniqueEmail = $"fpnewcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();

        MailMessage latestResetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string newResetCode = EmailVerificationNotification.ExtractVerificationCode(latestResetEmail);

        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyForgotPasswordEmail", new { Email = uniqueEmail, VerificationCode = newResetCode });

        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.NotNull(verifyResponseData.RootElement.GetProperty("resetToken").GetString());
    }

    [Fact]
    public void EachRequestGeneratesDifferentCode() {
        string uniqueEmail = $"fpdiffcode{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = signUpCode }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage firstResetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string firstCode = EmailVerificationNotification.ExtractVerificationCode(firstResetEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/forgotPasswordWithEmail", new { Email = uniqueEmail }).EnsureSuccessStatusCode();
        MailMessage secondResetEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string secondCode = EmailVerificationNotification.ExtractVerificationCode(secondResetEmail);

        Assert.NotEqual(firstCode, secondCode);
    }
}
