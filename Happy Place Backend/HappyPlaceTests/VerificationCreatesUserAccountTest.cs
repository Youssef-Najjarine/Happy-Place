using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class VerificationCreatesUserAccountTest {
    // Tests - Email Verification Data Integrity

    [Fact]
    public void VerifiedEmailUserHasCorrectAccountData() {
        string uniqueEmail = $"data{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.NotEqual(Guid.Empty, userAccount.Id);
        Assert.Equal("Youssef Najjarine", userAccount.DisplayName);
        Assert.Equal(uniqueEmail, userAccount.EmailAddress);
        Assert.Null(userAccount.PhoneNumber);
        Assert.Null(userAccount.Bio);
        Assert.Null(userAccount.ProfilePhotoUrl);
        Assert.NotNull(userAccount.Username);
        Assert.Equal(userAccount.Username, userAccount.Username.ToLower());
        Assert.DoesNotContain(" ", userAccount.Username);
        Assert.True(userAccount.Username.Length <= 20);
        Assert.True((DateTime.UtcNow - userAccount.CreatedAtUtc).TotalSeconds < 30);
    }

    [Fact]
    public void VerifiedEmailUserPasswordMatchesOriginal() {
        string uniqueEmail = $"pwmatch{Guid.NewGuid():N}@gmail.com";
        string originalPassword = "Seven74!";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = originalPassword
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.True(PasswordHasher.VerifyPassword(originalPassword, userAccount.HashedPassword));
        Assert.False(PasswordHasher.VerifyPassword("WrongPassword1!", userAccount.HashedPassword));
    }

    [Fact]
    public void VerifiedEmailPendingRecordIsDeleted() {
        string uniqueEmail = $"pending{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContextBefore = HappyPlaceDbContext.Create();
        Assert.True(dbContextBefore.PendingUserAccounts.Any(field => field.EmailAddress == uniqueEmail));

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        Assert.False(dbContextAfter.PendingUserAccounts.Any(field => field.EmailAddress == uniqueEmail));
    }

    [Fact]
    public void VerifiedEmailAuthTokenContainsCorrectUserId() {
        string uniqueEmail = $"token{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        string authTokenString = verifyResponseData.RootElement.GetProperty("authToken").GetString();

        UserAuthenticationToken validatedToken = UserAuthenticationToken.ValidateToken(authTokenString);
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.NotNull(validatedToken);
        Assert.Equal(userAccount.Id.ToString(), validatedToken.Identifier);
        Assert.True(validatedToken.ExpirationDateUtc > DateTimeOffset.UtcNow);
    }

    // Tests - Phone Verification Data Integrity

    [Fact]
    public void VerifiedPhoneUserHasCorrectAccountData() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.NotEqual(Guid.Empty, userAccount.Id);
        Assert.Equal("Youssef Najjarine", userAccount.DisplayName);
        Assert.Equal(uniquePhone, userAccount.PhoneNumber);
        Assert.Null(userAccount.EmailAddress);
        Assert.Null(userAccount.Bio);
        Assert.Null(userAccount.ProfilePhotoUrl);
        Assert.NotNull(userAccount.Username);
        Assert.Equal(userAccount.Username, userAccount.Username.ToLower());
        Assert.DoesNotContain(" ", userAccount.Username);
        Assert.True(userAccount.Username.Length <= 20);
        Assert.True((DateTime.UtcNow - userAccount.CreatedAtUtc).TotalSeconds < 30);
    }

    [Fact]
    public void VerifiedPhoneUserPasswordMatchesOriginal() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        string originalPassword = "Seven74!";
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = originalPassword
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.True(PasswordHasher.VerifyPassword(originalPassword, userAccount.HashedPassword));
        Assert.False(PasswordHasher.VerifyPassword("WrongPassword1!", userAccount.HashedPassword));
    }

    [Fact]
    public void VerifiedPhonePendingRecordIsDeleted() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);

        using var dbContextBefore = HappyPlaceDbContext.Create();
        Assert.True(dbContextBefore.PendingUserAccounts.Any(field => field.PhoneNumber == uniquePhone));

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        Assert.False(dbContextAfter.PendingUserAccounts.Any(field => field.PhoneNumber == uniquePhone));
    }

    [Fact]
    public void VerifiedPhoneAuthTokenContainsCorrectUserId() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var jsonData = new {
            Name = "Youssef Najjarine",
            PhoneNumber = uniquePhone,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithPhone", jsonData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        var verifyResponseData = verifyResponse.ReadContentAsJsonDocument();
        string authTokenString = verifyResponseData.RootElement.GetProperty("authToken").GetString();

        UserAuthenticationToken validatedToken = UserAuthenticationToken.ValidateToken(authTokenString);
        using var dbContext = HappyPlaceDbContext.Create();
        var userAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);

        Assert.NotNull(validatedToken);
        Assert.Equal(userAccount.Id.ToString(), validatedToken.Identifier);
        Assert.True(validatedToken.ExpirationDateUtc > DateTimeOffset.UtcNow);
    }

    // Tests - Failed Verification Does Not Create UserAccount

    [Fact]
    public void WrongCodeDoesNotCreateUserAccount() {
        string uniqueEmail = $"nouser{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = "000000" });

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.UserAccounts.Any(field => field.EmailAddress == uniqueEmail));
        Assert.True(dbContext.PendingUserAccounts.Any(field => field.EmailAddress == uniqueEmail));
    }

    [Fact]
    public void ExpiredCodeDoesNotCreateUserAccount() {
        string uniqueEmail = $"expnouser{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);

        using var dbContextExpire = HappyPlaceDbContext.Create();
        var pendingAccount = dbContextExpire.PendingUserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        pendingAccount.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-11);
        dbContextExpire.SaveChanges();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.UserAccounts.Any(field => field.EmailAddress == uniqueEmail));
        Assert.True(dbContext.PendingUserAccounts.Any(field => field.EmailAddress == uniqueEmail));
    }

    // Tests - Multi-User Isolation

    [Fact]
    public void TwoDifferentEmailUsersGetSeparateAccounts() {
        string email1 = $"multi1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"multi2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User One", Email = email1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1Msg = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1Msg);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email1, VerificationCode = code1 }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User Two", Email = email2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2Msg = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2Msg);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email2, VerificationCode = code2 }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        var user1 = dbContext.UserAccounts.Single(field => field.EmailAddress == email1);
        var user2 = dbContext.UserAccounts.Single(field => field.EmailAddress == email2);

        Assert.NotEqual(user1.Id, user2.Id);
        Assert.NotEqual(user1.Username, user2.Username);
        Assert.Equal("User One", user1.DisplayName);
        Assert.Equal("User Two", user2.DisplayName);
    }

    [Fact]
    public void UserAccountTableGainsExactlyOneRecordAfterVerification() {
        string uniqueEmail = $"count{Guid.NewGuid():N}@gmail.com";
        var jsonData = new {
            Name = "Youssef Najjarine",
            Email = uniqueEmail,
            Password = "Seven74!"
        };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        using var dbContextBefore = HappyPlaceDbContext.Create();
        int countBefore = dbContextBefore.UserAccounts.Count();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", jsonData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode }).EnsureSuccessStatusCode();

        using var dbContextAfter = HappyPlaceDbContext.Create();
        int countAfter = dbContextAfter.UserAccounts.Count();

        Assert.Equal(countBefore + 1, countAfter);
    }
}
