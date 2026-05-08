using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ResetPasswordTest {
    // Tests - Happy Path

    [Fact]
    public void ValidTokenAndPasswordResetsPassword() {
        string uniqueEmail = $"rp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void UserCanSignInWithNewPasswordAfterReset() {
        string uniqueEmail = $"rpsignin{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" }).EnsureSuccessStatusCode();
        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "NewPass99!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    [Fact]
    public void UserCannotSignInWithOldPasswordAfterReset() {
        string uniqueEmail = $"rpold{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" }).EnsureSuccessStatusCode();
        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithEmail", new { Email = uniqueEmail, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.BadRequest, signInResponse.StatusCode);
    }

    [Fact]
    public void SuccessfulResetMarksRequestAsUsed() {
        string uniqueEmail = $"rpused{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
        Assert.NotNull(resetRequest.UsedAt);
    }

    [Fact]
    public void HashedPasswordIsUpdatedInUserAccount() {
        string uniqueEmail = $"rphash{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        string oldHashedPassword;
        using (var dbContext = HappyPlaceDbContext.Create()) {
            oldHashedPassword = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).HashedPassword;
        }

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" }).EnsureSuccessStatusCode();

        using (var dbContext = HappyPlaceDbContext.Create()) {
            string newHashedPassword = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).HashedPassword;
            Assert.NotEqual(oldHashedPassword, newHashedPassword);
        }
    }

    [Fact]
    public void ResetWorksForPhoneAccount() {
        string uniquePhone = $"949{Random.Shared.Next(1000000, 10000000)}";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForPhone(testingMockProvidersContainer, uniquePhone);

        HttpResponseMessage resetResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" });
        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/signInWithPhone", new { PhoneNumber = uniquePhone, Password = "NewPass99!" });

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
    }

    // Tests - Token Validation

    [Fact]
    public void InvalidResetTokenReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = "fakeresettoken12345", NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void EmptyResetTokenReturnsBadRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = "", NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void TamperedResetTokenReturnsBadRequest() {
        string uniqueEmail = $"rptamper{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);
        string tamperedToken = resetToken[..^1] + (resetToken[^1] == 'a' ? 'b' : 'a');

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = tamperedToken, NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ExpiredResetTokenReturnsBadRequest() {
        string uniqueEmail = $"rpexp{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        using (var dbContext = HappyPlaceDbContext.Create()) {
            var resetRequest = dbContext.PasswordResetRequests.Single(field => field.EmailAddress == uniqueEmail);
            resetRequest.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            dbContext.SaveChanges();
        }

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void ResetTokenReusedAfterSuccessReturnsBadRequest() {
        string uniqueEmail = $"rpreplay{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" }).EnsureSuccessStatusCode();
        HttpResponseMessage secondReset = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "AnotherPw99!" });

        Assert.Equal(HttpStatusCode.BadRequest, secondReset.StatusCode);
    }

    // Tests - Password Validation

    [Fact]
    public void EmptyPasswordReturnsBadRequest() {
        string uniqueEmail = $"rpemptypw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void WhitespaceOnlyPasswordReturnsBadRequest() {
        string uniqueEmail = $"rpwspw{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "        " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordUnderEightCharactersReturnsBadRequest() {
        string uniqueEmail = $"rpshort{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "Se7en!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordAtExactlyEightCharactersSucceeds() {
        string uniqueEmail = $"rp8chars{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "Seven7!a" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingUppercaseReturnsBadRequest() {
        string uniqueEmail = $"rpnouppr{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "newpass99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingLowercaseReturnsBadRequest() {
        string uniqueEmail = $"rpnolowr{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NEWPASS99!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingNumberReturnsBadRequest() {
        string uniqueEmail = $"rpnonum{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPassPw!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordMissingSpecialCharacterReturnsBadRequest() {
        string uniqueEmail = $"rpnospec{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass991" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void PasswordValidationFailureDoesNotConsumeToken() {
        string uniqueEmail = $"rpnoconsume{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string resetToken = CreateVerifiedResetTokenForEmail(testingMockProvidersContainer, uniqueEmail);

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "weak" });
        HttpResponseMessage retryResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/resetPassword", new { ResetToken = resetToken, NewPassword = "NewPass99!" });

        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
    }

    // Helpers

    private static string CreateVerifiedResetTokenForEmail(TestingMockProvidersContainer container, string email) {
        container.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Youssef Najjarine", Email = email, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage signUpEmail = container.EmailProvider.EmailMessages.Single();
        string signUpCode = EmailVerificationNotification.ExtractVerificationCode(signUpEmail);
        container.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        container.WebClient.PostJson("api/authentication/forgotPasswordWithEmail", new { Email = email }).EnsureSuccessStatusCode();
        MailMessage resetEmail = container.EmailProvider.EmailMessages.Last();
        string resetCode = EmailVerificationNotification.ExtractVerificationCode(resetEmail);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/authentication/verifyForgotPasswordEmail", new { Email = email, VerificationCode = resetCode });
        verifyResponse.EnsureSuccessStatusCode();
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("resetToken").GetString();
    }

    private static string CreateVerifiedResetTokenForPhone(TestingMockProvidersContainer container, string phone) {
        container.WebClient.PostJson("api/authentication/signUpWithPhone", new { Name = "Youssef Najjarine", PhoneNumber = phone, Password = "Seven74!" }).EnsureSuccessStatusCode();
        SmsMessage signUpSms = container.SmsProvider.SentMessages.Single();
        string signUpCode = SmsVerificationNotification.ExtractVerificationCode(signUpSms);
        container.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = phone, VerificationCode = signUpCode }).EnsureSuccessStatusCode();
        container.WebClient.PostJson("api/authentication/forgotPasswordWithPhone", new { PhoneNumber = phone }).EnsureSuccessStatusCode();
        SmsMessage resetSms = container.SmsProvider.SentMessages.Last();
        string resetCode = SmsVerificationNotification.ExtractVerificationCode(resetSms);
        HttpResponseMessage verifyResponse = container.WebClient.PostJson("api/authentication/verifyForgotPasswordPhone", new { PhoneNumber = phone, VerificationCode = resetCode });
        verifyResponse.EnsureSuccessStatusCode();
        return verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("resetToken").GetString();
    }
}
