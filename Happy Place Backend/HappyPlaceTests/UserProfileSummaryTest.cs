using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UserProfileSummaryTest {
    // Tests - Happy Path

    [Fact]
    public void ValidTokenForEmailUserReturnsOk() {
        string uniqueEmail = $"profile{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public void ValidTokenForPhoneUserReturnsOk() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public void DisplayNameMatchesRegistrationName() {
        string uniqueEmail = $"name{Guid.NewGuid():N}@gmail.com";
        string registrationName = "Youssef Najjarine";
        var signUpData = new { Name = registrationName, Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.Equal(registrationName, profileData.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public void DisplayNamePreservesFullLengthAt200Characters() {
        string uniqueEmail = $"long{Guid.NewGuid():N}@gmail.com";
        string longName = Guid.NewGuid().ToString("N") + new string('A', 168);
        var signUpData = new { Name = longName, Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string returnedName = profileData.RootElement.GetProperty("displayName").GetString();

        Assert.Equal(200, returnedName.Length);
        Assert.Equal(longName, returnedName);
    }

    [Fact]
    public void UsernameIsNonNullAndNonEmpty() {
        string uniqueEmail = $"uname{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string username = profileData.RootElement.GetProperty("username").GetString();

        Assert.NotNull(username);
        Assert.NotEmpty(username);
    }

    [Fact]
    public void AvatarColorIsValidHexColor() {
        string uniqueEmail = $"hex{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string avatarColor = profileData.RootElement.GetProperty("avatarColor").GetString();

        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", avatarColor);
    }

    [Fact]
    public void AvatarColorIsConsistentAcrossMultipleCalls() {
        string uniqueEmail = $"consistent{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage firstProfileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        string firstColor = firstProfileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        HttpResponseMessage secondProfileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        string secondColor = secondProfileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        Assert.Equal(firstColor, secondColor);
    }

    [Fact]
    public void ProfilePhotoUrlIsNullForNewUser() {
        string uniqueEmail = $"nophoto{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        JsonElement profilePhotoUrl = profileData.RootElement.GetProperty("profilePhotoUrl");

        Assert.Equal(JsonValueKind.Null, profilePhotoUrl.ValueKind);
    }

    [Fact]
    public void ProfilePhotoUrlIsReturnedWhenSet() {
        string uniqueEmail = $"hasphoto{Guid.NewGuid():N}@gmail.com";
        string expectedPhotoUrl = "https://happyplace.blob.core.windows.net/photos/test-avatar.jpg";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        user.ProfilePhotoUrl = expectedPhotoUrl;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string returnedPhotoUrl = profileData.RootElement.GetProperty("profilePhotoUrl").GetString();

        Assert.Equal(expectedPhotoUrl, returnedPhotoUrl);
    }

    [Fact]
    public void AvatarColorFromEndpointMatchesDirectCalculation() {
        string uniqueEmail = $"colormatch{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        string endpointColor = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        string calculatedColor = UserAccountRegistrar.GetAvatarColor(user.Id);

        Assert.Equal(calculatedColor, endpointColor);
    }

    [Fact]
    public void AvatarColorFromEndpointMatchesDirectCalculationForPhoneUser() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        string endpointColor = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.PhoneNumber == uniquePhone);
        string calculatedColor = UserAccountRegistrar.GetAvatarColor(user.Id);

        Assert.Equal(calculatedColor, endpointColor);
    }

    // Tests - Response Security

    [Fact]
    public void ResponseContainsExactlyExpectedProperties() {
        string uniqueEmail = $"shape{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        List<string> actualProperties = [.. profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = [.. new List<string> { "avatarColor", "displayName", "isAnonymous", "profilePhotoUrl", "username" }.OrderBy(name => name)];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void ResponseDoesNotContainUserId() {
        string uniqueEmail = $"noid{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("userId", out _));
        Assert.False(profileData.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public void ResponseDoesNotContainEmailAddress() {
        string uniqueEmail = $"noemail{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("emailAddress", out _));
        Assert.False(profileData.RootElement.TryGetProperty("email", out _));
    }

    [Fact]
    public void ResponseDoesNotContainPhoneNumber() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("phoneNumber", out _));
        Assert.False(profileData.RootElement.TryGetProperty("phone", out _));
    }

    [Fact]
    public void ResponseDoesNotContainHashedPassword() {
        string uniqueEmail = $"nopw{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("hashedPassword", out _));
        Assert.False(profileData.RootElement.TryGetProperty("password", out _));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TamperedTokenReturnsUnauthorized() {
        string uniqueEmail = $"tamper{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
        string tamperedToken = authToken[..^1] + (authToken[^1] == 'A' ? 'B' : 'A');

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = tamperedToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Cross-User Data Isolation

    [Fact]
    public void TwoUsersReceiveTheirOwnProfileData() {
        string uniqueEmail1 = $"user1{Guid.NewGuid():N}@gmail.com";
        string uniqueEmail2 = $"user2{Guid.NewGuid():N}@gmail.com";
        string name1 = "Alice Thompson";
        string name2 = "Bob Martinez";
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = name1, Email = uniqueEmail1, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email1 = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string code1 = EmailVerificationNotification.ExtractVerificationCode(email1);
        HttpResponseMessage verify1 = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail1, VerificationCode = code1 });
        string token1 = verify1.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { Name = name2, Email = uniqueEmail2, Password = "Seven74!" }).EnsureSuccessStatusCode();
        MailMessage email2 = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string code2 = EmailVerificationNotification.ExtractVerificationCode(email2);
        HttpResponseMessage verify2 = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail2, VerificationCode = code2 });
        string token2 = verify2.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profile1 = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = token1 });
        HttpResponseMessage profile2 = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/validateToken", new { AuthToken = token2 });
        string displayName1 = profile1.ReadContentAsJsonDocument().RootElement.GetProperty("displayName").GetString();
        string displayName2 = profile2.ReadContentAsJsonDocument().RootElement.GetProperty("displayName").GetString();

        Assert.Equal(name1, displayName1);
        Assert.Equal(name2, displayName2);
        Assert.NotEqual(displayName1, displayName2);
    }
}
