using System.Net;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GuestUpgradeTest {
    // Tests - Upgrade In Place

    [Fact]
    public void RegisteringAsGuestUpgradesTheSameAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = GetGuestUserAccountId();
        string email = $"upgraded{Guid.NewGuid():N}@gmail.com";

        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Real Name", email, "Seven74!").EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount upgradedAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == email);
        Assert.Equal(guestUserAccountId, upgradedAccount.Id);
        Assert.False(upgradedAccount.IsAnonymous);
    }

    [Fact]
    public void RegisteringAsGuestDoesNotCreateANewAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        string email = $"upgraded{Guid.NewGuid():N}@gmail.com";

        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Real Name", email, "Seven74!").EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.UserAccounts.Count());
    }

    [Fact]
    public void GuestChatMembershipSurvivesRegistration() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = GetGuestUserAccountId();

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = guestAuthToken, Topic = "I need help" }).EnsureSuccessStatusCode();

        string email = $"upgraded{Guid.NewGuid():N}@gmail.com";
        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Real Name", email, "Seven74!").EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount upgradedAccount = dbContext.UserAccounts.Single(field => field.EmailAddress == email);
        Assert.Equal(guestUserAccountId, upgradedAccount.Id);
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.UserAccountId == upgradedAccount.Id));
    }

    [Fact]
    public void RegisteringAsGuestWithPhoneUpgradesTheSameAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = GetGuestUserAccountId();
        string phoneNumber = string.Concat(Guid.NewGuid().ToString().Where(char.IsDigit).Take(10));

        RegisterGuestWithPhone(testingMockProvidersContainer, guestAuthToken, "Real Name", phoneNumber, "Seven74!").EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount upgradedAccount = dbContext.UserAccounts.Single(field => field.PhoneNumber == phoneNumber);
        Assert.Equal(guestUserAccountId, upgradedAccount.Id);
        Assert.False(upgradedAccount.IsAnonymous);
    }

    // Tests - Credentials Apply

    [Fact]
    public void RegisteredGuestCanSignInWithNewCredentials() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        string email = $"upgraded{Guid.NewGuid():N}@gmail.com";
        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Real Name", email, "Seven74!").EnsureSuccessStatusCode();

        HttpResponseMessage signInResponse = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = email, Password = "Seven74!" });

        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);
        Assert.Equal("verified", signInResponse.ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString());
    }

    // Tests - Normal Signup Unaffected

    [Fact]
    public void NonGuestSignupProducesVerifiedAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string email = $"normal{Guid.NewGuid():N}@gmail.com";

        RegisterGuestWithEmail(testingMockProvidersContainer, null, "Normal Person", email, "Seven74!").EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount account = dbContext.UserAccounts.Single(field => field.EmailAddress == email);
        Assert.False(account.IsAnonymous);
    }

    // Helpers

    private static Guid GetGuestUserAccountId() {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.IsAnonymous).Id;
    }

    private static HttpResponseMessage RegisterGuestWithEmail(TestingMockProvidersContainer testingMockProvidersContainer, string guestAuthToken, string name, string email, string password) {
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { AuthToken = guestAuthToken, Name = name, Email = email, Password = password }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        return testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = email, VerificationCode = verificationCode });
    }

    private static HttpResponseMessage RegisterGuestWithPhone(TestingMockProvidersContainer testingMockProvidersContainer, string guestAuthToken, string name, string phoneNumber, string password) {
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithPhone", new { AuthToken = guestAuthToken, Name = name, PhoneNumber = phoneNumber, Password = password }).EnsureSuccessStatusCode();
        SmsMessage verificationSms = testingMockProvidersContainer.SmsProvider.SentMessages.Last();
        string verificationCode = SmsVerificationNotification.ExtractVerificationCode(verificationSms);
        return testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyPhone", new { PhoneNumber = phoneNumber, VerificationCode = verificationCode });
    }
}
