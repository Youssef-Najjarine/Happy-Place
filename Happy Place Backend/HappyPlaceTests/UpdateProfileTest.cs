using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UpdateProfileTest {
    // Tests - Happy Path

    [Fact]
    public void UpdateAllFieldsReturnsOk() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "newuser1", DisplayName = "New Name", Bio = "My new bio" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void UpdatedFieldsAreReflectedInResponse() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "updated1", DisplayName = "Updated Name", Bio = "Updated bio text" });
        var data = response.ReadContentAsJsonDocument();

        Assert.Equal("updated1", data.RootElement.GetProperty("username").GetString());
        Assert.Equal("Updated Name", data.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("Updated bio text", data.RootElement.GetProperty("bio").GetString());
    }

    [Fact]
    public void ClearBioBySettingEmptyStringSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        string currentUsername = user.Username;
        user.Bio = "Existing bio to be cleared";
        dbContext.SaveChanges();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = "Original Name", Bio = "" });
        var data = response.ReadContentAsJsonDocument();
        JsonElement bio = data.RootElement.GetProperty("bio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(bio.ValueKind == JsonValueKind.Null || bio.GetString() == "");
    }

    // Tests - Username Auto-Lowercase

    [Fact]
    public void UsernameIsSavedAsLowercase() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "MyUser1", DisplayName = "Original Name", Bio = "" });
        string returnedUsername = response.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString();

        Assert.Equal("myuser1", returnedUsername);
    }

    [Fact]
    public void MixedCaseUsernameIsSavedAsLowercase() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "YoUsSeF1", DisplayName = "Original Name", Bio = "" });
        string returnedUsername = response.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString();

        Assert.Equal("youssef1", returnedUsername);

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        Assert.Equal("youssef1", user.Username);
    }

    // Tests - Own Username Edge Cases

    [Fact]
    public void SameUsernameAsCurrentSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void SameUsernameAsCurrentDifferentCaseSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername.ToUpperInvariant(), DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(currentUsername, response.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString());
    }

    // Tests - Username Uniqueness

    [Fact]
    public void UsernameTakenByAnotherUserReturnsError() {
        string email1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"user2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User One", Email = email1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email1, VerificationCode = code1 });

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User Two", Email = email2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2Verification);
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email2, VerificationCode = code2 });
        string token2 = verify2.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string user1Username = dbContext.UserAccounts.Single(field => field.EmailAddress == email1).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = token2, Username = user1Username, DisplayName = "User Two", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameTakenCaseInsensitiveReturnsError() {
        string email1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string email2 = $"user2{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User One", Email = email1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1Verification);
        testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email1, VerificationCode = code1 });

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "User Two", Email = email2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2Verification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2Verification);
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = email2, VerificationCode = code2 });
        string token2 = verify2.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string user1Username = dbContext.UserAccounts.Single(field => field.EmailAddress == email1).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = token2, Username = user1Username.ToUpperInvariant(), DisplayName = "User Two", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameTakenByPendingAccountReturnsError() {
        string pendingEmail = $"pending{Guid.NewGuid():N}@gmail.com";
        string verifiedEmail = $"verified{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Pending User", Email = pendingEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Verified User", Email = verifiedEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verifiedVerification = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verifiedCode = EmailVerificationNotification.ExtractVerificationCode(verifiedVerification);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = verifiedEmail, VerificationCode = verifiedCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string pendingUsername = dbContext.PendingUserAccounts.Single(field => field.EmailAddress == pendingEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = pendingUsername, DisplayName = "Verified User", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Username Format Validation

    [Fact]
    public void UsernameTooShortReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "ab1c", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameAtMinimumLengthSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "abcd1", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void UsernameTooLongReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "abcdefghijklmnopqrst1", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameAtMaximumLengthSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "abcdefghijklmnopqrs1", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void UsernameWithSpecialCharactersReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "user@1!", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameWithSpacesReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "my user1", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameWithUnderscoresReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "my_user1", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void UsernameWithoutNumberReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = "abcdefgh", DisplayName = "Original Name", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Name Validation

    [Fact]
    public void EmptyDisplayNameReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = "", Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void DisplayNameAt200CharactersSucceeds() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        string longName = Guid.NewGuid().ToString("N") + new string('A', 168);
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = longName, Bio = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(longName, response.ReadContentAsJsonDocument().RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public void DisplayNameOver200CharactersReturnsError() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        string tooLongName = Guid.NewGuid().ToString("N") + new string('A', 169);
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = tooLongName, Bio = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Tests - Auth Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = "", Username = "test1", DisplayName = "Test", Bio = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = "not-a-real-token", Username = "test1", DisplayName = "Test", Bio = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Response Shape

    [Fact]
    public void ResponseContainsExactlyMyProfileResultProperties() {
        string uniqueEmail = $"upd{Guid.NewGuid():N}@gmail.com";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/authentication/signUpWithEmail", new { Name = "Original Name", Email = uniqueEmail, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/authentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        string currentUsername = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail).Username;

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/profile/updateProfile", new { AuthToken = authToken, Username = currentUsername, DisplayName = "Original Name", Bio = "A bio" });
        var data = response.ReadContentAsJsonDocument();
        List<string> actualProperties = data.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToList();
        List<string> expectedProperties = new List<string> { "avatarColor", "backgroundPhotoUrl", "bio", "displayName", "emailAddress", "phoneNumber", "profilePhotoUrl", "username" }.OrderBy(name => name).ToList();

        Assert.Equal(expectedProperties, actualProperties);
    }
}
