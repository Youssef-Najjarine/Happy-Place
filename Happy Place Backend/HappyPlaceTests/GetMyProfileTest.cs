using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GetMyProfileTest {
    // Tests - Happy Path

    [Fact]
    public void EmailUserGetsOkStatus() {
        string uniqueEmail = $"myprofile{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    [Fact]
    public void PhoneUserGetsOkStatus() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
    }

    // Tests - Field Accuracy

    [Fact]
    public void DisplayNameMatchesRegistration() {
        string uniqueEmail = $"dispname{Guid.NewGuid():N}@gmail.com";
        string registrationName = "Youssef Najjarine";
        var signUpData = new { Name = registrationName, Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.Equal(registrationName, profileData.RootElement.GetProperty("displayName").GetString());
    }

    [Fact]
    public void DisplayNamePreservesFullLengthAt200Characters() {
        string uniqueEmail = $"longname{Guid.NewGuid():N}@gmail.com";
        string longName = Guid.NewGuid().ToString("N") + new string('A', 168);
        var signUpData = new { Name = longName, Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string returnedName = profileData.RootElement.GetProperty("displayName").GetString();

        Assert.Equal(200, returnedName.Length);
        Assert.Equal(longName, returnedName);
    }

    [Fact]
    public void UsernameMatchesDb() {
        string uniqueEmail = $"uname{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedUsername = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("username").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);

        Assert.Equal(user.Username, returnedUsername);
    }

    [Fact]
    public void AvatarColorIsValidHex() {
        string uniqueEmail = $"hex{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string avatarColor = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", avatarColor);
        Assert.Contains(avatarColor, UserAccountRegistrar.AvatarColorPalette);
    }

    [Fact]
    public void AvatarColorMatchesDirectCalculation() {
        string uniqueEmail = $"colorcalc{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string endpointColor = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("avatarColor").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        string calculatedColor = UserAccountRegistrar.GetAvatarColor(user.Id);

        Assert.Equal(calculatedColor, endpointColor);
    }

    [Fact]
    public void EmailAddressMatchesRegistrationForEmailUser() {
        string uniqueEmail = $"emailmatch{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedEmail = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("emailAddress").GetString();

        Assert.Equal(uniqueEmail, returnedEmail);
    }

    [Fact]
    public void PhoneNumberIsNullForEmailOnlyUser() {
        string uniqueEmail = $"nophone{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        JsonElement phoneNumber = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("phoneNumber");

        Assert.Equal(JsonValueKind.Null, phoneNumber.ValueKind);
    }

    [Fact]
    public void PhoneNumberMatchesRegistrationForPhoneUser() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedPhone = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("phoneNumber").GetString();

        Assert.Equal(uniquePhone, returnedPhone);
    }

    [Fact]
    public void EmailAddressIsNullForPhoneOnlyUser() {
        string uniquePhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", PhoneNumber = uniquePhone, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", signUpData).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Single();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = uniquePhone, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        JsonElement emailAddress = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("emailAddress");

        Assert.Equal(JsonValueKind.Null, emailAddress.ValueKind);
    }

    // Tests - Fields Default / When Set

    [Fact]
    public void BothEmailAndPhoneReturnedWhenBothSet() {
        string uniqueEmail = $"dual{Guid.NewGuid():N}@gmail.com";
        string addedPhone = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        user.PhoneNumber = addedPhone;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        string returnedEmail = profileData.RootElement.GetProperty("emailAddress").GetString();
        string returnedPhone = profileData.RootElement.GetProperty("phoneNumber").GetString();

        Assert.Equal(uniqueEmail, returnedEmail);
        Assert.Equal(addedPhone, returnedPhone);
    }

    [Fact]
    public void BioIsNullForNewUser() {
        string uniqueEmail = $"nobio{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        JsonElement bio = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("bio");

        Assert.Equal(JsonValueKind.Null, bio.ValueKind);
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

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        JsonElement profilePhotoUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl");

        Assert.Equal(JsonValueKind.Null, profilePhotoUrl.ValueKind);
    }

    [Fact]
    public void BackgroundPhotoUrlIsNullForNewUser() {
        string uniqueEmail = $"nobg{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        JsonElement backgroundPhotoUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl");

        Assert.Equal(JsonValueKind.Null, backgroundPhotoUrl.ValueKind);
    }

    [Fact]
    public void BioIsReturnedWhenSet() {
        string uniqueEmail = $"hasbio{Guid.NewGuid():N}@gmail.com";
        string expectedBio = "Sometimes the world feels heavy, but I believe in kindness.";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        user.Bio = expectedBio;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedBio = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("bio").GetString();

        Assert.Equal(expectedBio, returnedBio);
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

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedPhotoUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("profilePhotoUrl").GetString();

        Assert.Equal(expectedPhotoUrl, returnedPhotoUrl);
    }

    [Fact]
    public void BackgroundPhotoUrlIsReturnedWhenSet() {
        string uniqueEmail = $"hasbg{Guid.NewGuid():N}@gmail.com";
        string expectedBgUrl = "https://happyplace.blob.core.windows.net/backgrounds/test-bg.jpg";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        user.BackgroundPhotoUrl = expectedBgUrl;
        dbContext.SaveChanges();

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        string returnedBgUrl = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("backgroundPhotoUrl").GetString();

        Assert.Equal(expectedBgUrl, returnedBgUrl);
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

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();
        List<string> actualProperties = [.. profileData.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["avatarColor", "backgroundPhotoUrl", "bio", "displayName", "emailAddress", "friendCount", "phoneNumber", "profilePhotoUrl", "username"];

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

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("userId", out _));
        Assert.False(profileData.RootElement.TryGetProperty("id", out _));
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

        HttpResponseMessage profileResponse = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });
        var profileData = profileResponse.ReadContentAsJsonDocument();

        Assert.False(profileData.RootElement.TryGetProperty("hashedPassword", out _));
        Assert.False(profileData.RootElement.TryGetProperty("password", out _));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = "not-a-real-token-at-all" });

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

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = tamperedToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeletedUserTokenReturnsUnauthorized() {
        string uniqueEmail = $"deleted{Guid.NewGuid():N}@gmail.com";
        var signUpData = new { Name = "Youssef Najjarine", Email = uniqueEmail, Password = "Seven74!" };
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", signUpData).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Single();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        HttpResponseMessage verifyResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = uniqueEmail, VerificationCode = verificationCode });
        string authToken = verifyResponse.ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.EmailAddress == uniqueEmail);
        dbContext.UserAccounts.Remove(user);
        dbContext.SaveChanges();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Cross-User Isolation

    [Fact]
    public void TwoUsersGetTheirOwnProfileData() {
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

        HttpResponseMessage profile1 = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = token1 });
        HttpResponseMessage profile2 = testingMockProvidersContainer.WebClient.PostJson("api/userProfile/getMyProfile", new { AuthToken = token2 });
        var data1 = profile1.ReadContentAsJsonDocument();
        var data2 = profile2.ReadContentAsJsonDocument();

        Assert.Equal(name1, data1.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(uniqueEmail1, data1.RootElement.GetProperty("emailAddress").GetString());
        Assert.Equal(name2, data2.RootElement.GetProperty("displayName").GetString());
        Assert.Equal(uniqueEmail2, data2.RootElement.GetProperty("emailAddress").GetString());
        Assert.NotEqual(data1.RootElement.GetProperty("username").GetString(), data2.RootElement.GetProperty("username").GetString());
    }
}
