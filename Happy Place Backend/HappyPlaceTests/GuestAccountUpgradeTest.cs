using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GuestAccountUpgradeTest {
    // Tests - Core identity + carryover

    [Fact]
    public void SigningUpWithAGuestTokenKeepsTheSameAccountId() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);

        string accountToken = SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.Equal(guestAccountId, AccountId(accountToken));
    }

    [Fact]
    public void AGuestsPendingRequestIsStillOwnedByTheUpgradedAccount() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");

        SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.True(GroupExists(chatGroupId));
        Assert.Equal(guestAccountId, GroupOwner(chatGroupId));
        Assert.Equal(ChatGroupStatus.Provisional, GroupStatus(chatGroupId));
    }

    [Fact]
    public void TheUpgradedAccountIsNoLongerAnonymous() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Assert.True(IsAnonymous(guestAccountId));

        SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.False(IsAnonymous(guestAccountId));
    }

    [Fact]
    public void TheUpgradedAccountKeepsTheDetailsSuppliedAtSignup() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        string email = NewEmail();

        SignUpAndVerifyEmail(container, guestToken, "Casey Jordan", email, Password);

        Assert.Equal("Casey Jordan", DisplayName(guestAccountId));
        Assert.Equal(guestAccountId, AccountId(SignInAndGetToken(container, email, Password)));
    }

    [Fact]
    public void UpgradingDoesNotCreateASecondAccount() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        int accountsBefore = AccountCount();

        SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.Equal(accountsBefore, AccountCount());
    }

    // Tests - Active group / offers carryover

    [Fact]
    public void AGuestsActiveGroupSurvivesTheUpgrade() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");
        CreateOffer(container, CreateGuest(container), chatGroupId);
        Connect(container, guestToken, chatGroupId);

        SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.Equal(ChatGroupStatus.Active, GroupStatus(chatGroupId));
        Assert.Equal(guestAccountId, GroupOwner(chatGroupId));
    }

    [Fact]
    public void OffersOnAGuestsRequestSurviveTheUpgrade() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");
        CreateOffer(container, CreateGuest(container), chatGroupId);

        SignUpAndVerifyEmail(container, guestToken, "Casey", NewEmail(), Password);

        Assert.Equal(1, OfferedCount(chatGroupId));
    }

    // Tests - Verify-during-sign-in path

    [Fact]
    public void AGuestWhoSignsUpThenSignsInBeforeVerifyingStillUpgradesOnVerify() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");
        string email = NewEmail();
        SignUpWithEmail(container, guestToken, "Casey", email, Password).EnsureSuccessStatusCode();
        container.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = email, Password });
        string code = LastVerificationCode(container);

        VerifyEmail(container, email, code).EnsureSuccessStatusCode();

        Assert.Equal(guestAccountId, GroupOwner(chatGroupId));
        Assert.False(IsAnonymous(guestAccountId));
    }

    // Tests - No token / separate account

    [Fact]
    public void SigningUpWithoutAGuestTokenCreatesASeparateAccount() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);

        string accountToken = SignUpAndVerifyEmail(container, "", "Casey", NewEmail(), Password);

        Assert.NotEqual(guestAccountId, AccountId(accountToken));
    }

    [Fact]
    public void SigningUpWithoutAGuestTokenLeavesTheGuestUntouched() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");

        SignUpAndVerifyEmail(container, "", "Casey", NewEmail(), Password);

        Assert.True(IsAnonymous(guestAccountId));
        Assert.Equal(guestAccountId, GroupOwner(chatGroupId));
    }

    // Tests - Invalid / non-guest token

    [Fact]
    public void SigningUpWithAGarbageTokenFallsBackToANewAccount() {
        using var container = new TestingMockProvidersContainer();

        string accountToken = SignUpAndVerifyEmail(container, "not-a-real-token-at-all", "Casey", NewEmail(), Password);

        Assert.False(IsAnonymous(AccountId(accountToken)));
    }

    [Fact]
    public void SigningUpWithAVerifiedUsersTokenDoesNotUpgradeThatUser() {
        using var container = new TestingMockProvidersContainer();
        string existingToken = TestUserFactory.CreateVerifiedEmailUser(container, "Existing");
        Guid existingAccountId = AccountId(existingToken);

        string newToken = SignUpAndVerifyEmail(container, existingToken, "Casey", NewEmail(), Password);

        Assert.NotEqual(existingAccountId, AccountId(newToken));
        Assert.Equal("Existing", DisplayName(existingAccountId));
    }

    // Tests - Duplicate contact collision

    [Fact]
    public void AGuestSigningUpWithAnAlreadyVerifiedEmailIsRejected() {
        using var container = new TestingMockProvidersContainer();
        string takenEmail = NewEmail();
        SignUpAndVerifyEmail(container, "", "Owner", takenEmail, Password);
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);

        HttpResponseMessage response = SignUpWithEmail(container, guestToken, "Casey", takenEmail, Password);

        Assert.False(response.IsSuccessStatusCode);
        Assert.True(IsAnonymous(guestAccountId));
    }

    [Fact]
    public void ARejectedDuplicateSignupLeavesTheGuestsRequestIntact() {
        using var container = new TestingMockProvidersContainer();
        string takenEmail = NewEmail();
        SignUpAndVerifyEmail(container, "", "Owner", takenEmail, Password);
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        Guid chatGroupId = CreateRequest(container, guestToken, "I need help");

        SignUpWithEmail(container, guestToken, "Casey", takenEmail, Password);

        Assert.Equal(guestAccountId, GroupOwner(chatGroupId));
        Assert.Equal(ChatGroupStatus.Provisional, GroupStatus(chatGroupId));
    }

    // Tests - Fallback guard (guest gone / no longer anonymous at verify)

    [Fact]
    public void AGuestDeletedBetweenSignupAndVerifyFallsBackToANewAccount() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        string email = NewEmail();
        SignUpWithEmail(container, guestToken, "Casey", email, Password).EnsureSuccessStatusCode();
        string code = LastVerificationCode(container);
        DeleteAccount(guestAccountId);

        string accountToken = VerifyEmail(container, email, code).ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        Assert.NotEqual(guestAccountId, AccountId(accountToken));
        Assert.False(IsAnonymous(AccountId(accountToken)));
    }

    [Fact]
    public void AGuestNoLongerAnonymousAtVerifyFallsBackToANewAccount() {
        using var container = new TestingMockProvidersContainer();
        string guestToken = CreateGuest(container);
        Guid guestAccountId = AccountId(guestToken);
        string email = NewEmail();
        SignUpWithEmail(container, guestToken, "Casey", email, Password).EnsureSuccessStatusCode();
        string code = LastVerificationCode(container);
        MarkAccountNonAnonymous(guestAccountId);

        string accountToken = VerifyEmail(container, email, code).ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();

        Assert.NotEqual(guestAccountId, AccountId(accountToken));
    }

    // Tests - Races and collisions

    [Fact]
    public void TwoGuestsUpgradingToDistinctEmailsStayDistinctAccounts() {
        using var container = new TestingMockProvidersContainer();
        string firstGuest = CreateGuest(container);
        string secondGuest = CreateGuest(container);
        Guid firstGuestId = AccountId(firstGuest);
        Guid secondGuestId = AccountId(secondGuest);

        string firstToken = SignUpAndVerifyEmail(container, firstGuest, "First", NewEmail(), Password);
        string secondToken = SignUpAndVerifyEmail(container, secondGuest, "Second", NewEmail(), Password);

        Assert.Equal(firstGuestId, AccountId(firstToken));
        Assert.Equal(secondGuestId, AccountId(secondToken));
        Assert.NotEqual(AccountId(firstToken), AccountId(secondToken));
    }

    [Fact]
    public void ADifferentGuestsTokenDoesNotUpgradeTheWrongAccount() {
        using var container = new TestingMockProvidersContainer();
        string signingGuest = CreateGuest(container);
        string otherGuest = CreateGuest(container);
        Guid signingGuestId = AccountId(signingGuest);
        Guid otherGuestId = AccountId(otherGuest);

        SignUpAndVerifyEmail(container, signingGuest, "Casey", NewEmail(), Password);

        Assert.False(IsAnonymous(signingGuestId));
        Assert.True(IsAnonymous(otherGuestId));
    }

    // Helpers - Acting

    private static readonly string Password = "Seven74!";

    private static string CreateGuest(TestingMockProvidersContainer container) {
        return TestUserFactory.CreateGuestUser(container);
    }

    private static string NewEmail() {
        return $"user{Guid.NewGuid():N}@gmail.com";
    }

    private static HttpResponseMessage SignUpWithEmail(TestingMockProvidersContainer container, string authToken, string name, string email, string password) {
        return container.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { AuthToken = authToken, Name = name, Email = email, Password = password });
    }

    private static HttpResponseMessage VerifyEmail(TestingMockProvidersContainer container, string email, string verificationCode) {
        return container.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = email, VerificationCode = verificationCode });
    }

    private static string LastVerificationCode(TestingMockProvidersContainer container) {
        MailMessage verificationEmail = container.EmailProvider.EmailMessages.Last();
        return EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
    }

    private static string SignUpAndVerifyEmail(TestingMockProvidersContainer container, string guestToken, string name, string email, string password) {
        SignUpWithEmail(container, guestToken, name, email, password).EnsureSuccessStatusCode();
        string code = LastVerificationCode(container);
        return VerifyEmail(container, email, code).ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    private static string SignInAndGetToken(TestingMockProvidersContainer container, string email, string password) {
        return container.WebClient.PostJson("api/userAuthentication/signInWithEmail", new { Email = email, Password = password })
            .ReadContentAsJsonDocument().RootElement.GetProperty("authToken").GetString();
    }

    private static Guid CreateRequest(TestingMockProvidersContainer container, string authToken, string topic) {
        string chatGroupId = container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = authToken, Topic = topic })
            .ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return Guid.Parse(chatGroupId);
    }

    private static void CreateOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    // Helpers - Asserting

    private static Guid AccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static int AccountCount() {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Count();
    }

    private static bool IsAnonymous(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userAccountId).IsAnonymous;
    }

    private static string DisplayName(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userAccountId).DisplayName;
    }

    private static void MarkAccountNonAnonymous(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts.Where(field => field.Id == userAccountId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.IsAnonymous, false));
    }

    private static void DeleteAccount(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts.Where(field => field.Id == userAccountId).ExecuteDelete();
    }

    private static bool GroupExists(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == chatGroupId);
    }

    private static Guid GroupOwner(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId).OwnerUserAccountId;
    }

    private static ChatGroupStatus GroupStatus(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId).Status;
    }

    private static int OfferedCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered);
    }
}
