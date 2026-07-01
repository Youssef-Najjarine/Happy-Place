using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeclineInviteTest {
    // Tests - Authentication Failures

    [Fact]
    public void DeclineInviteEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeclineInviteInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeclineInviteMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Declining A Connected Invite

    [Fact]
    public void DeclineConnectedInviteReturnsDeclined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("declined", status);
    }

    [Fact]
    public void DeclineConnectedInviteSetsOfferDeclined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId);
        Assert.Equal(HelpOfferStatus.Declined, offer.Status);
    }

    [Fact]
    public void DeclinedInviteDropsFromStartedGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var startedGroups = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        bool present = startedGroups.EnumerateArray().Any(group => group.GetProperty("chatGroupId").GetString() == chatGroupId);
        Assert.False(present);
    }

    [Fact]
    public void DeclineInviteIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("declined", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Declined));
    }

    [Fact]
    public void DeclineInviteResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Nothing To Decline

    [Fact]
    public void DeclineInviteOnProvisionalGroupReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, helperAuthToken) = CreateProvisionalRequestWithOffer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId);
        Assert.Equal(HelpOfferStatus.Offered, offer.Status);
    }

    [Fact]
    public void DeclineInviteByOwnerReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, seekerAuthToken, _) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
    }

    [Fact]
    public void DeclineInviteWithoutConnectedOfferReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, _) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
    }

    [Fact]
    public void DeclineInviteAfterJoiningReturnsRequestClosedAndKeepsMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == helperUserAccountId));
    }

    [Fact]
    public void DeclineInviteUnknownGroupReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
    }

    // Tests - Declining Is Per Helper

    [Fact]
    public void OneHelperDecliningLeavesOtherHelperInvitedAndAbleToJoin() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, firstHelperAuthToken, secondHelperAuthToken) = StartGroupWithTwoOfferers(testingMockProvidersContainer, "I need help");

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineInvite", new { AuthToken = firstHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var firstStarted = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = firstHelperAuthToken }).ReadContentAsJsonDocument().RootElement;
        Assert.DoesNotContain(firstStarted.EnumerateArray(), group => group.GetProperty("chatGroupId").GetString() == chatGroupId);

        var secondStarted = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = secondHelperAuthToken }).ReadContentAsJsonDocument().RootElement;
        Assert.Contains(secondStarted.EnumerateArray(), group => group.GetProperty("chatGroupId").GetString() == chatGroupId);

        string secondJoinStatus = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = secondHelperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();
        Assert.Equal("joined", secondJoinStatus);
    }

    // Helpers

    private static (string ChatGroupId, string SeekerAuthToken, string HelperAuthToken) StartGroupWithOfferer(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return (chatGroupId, seekerAuthToken, helperAuthToken);
    }

    private static (string ChatGroupId, string FirstHelperAuthToken, string SecondHelperAuthToken) StartGroupWithTwoOfferers(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string firstHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = firstHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        string secondHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = secondHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return (chatGroupId, firstHelperAuthToken, secondHelperAuthToken);
    }

    private static (string ChatGroupId, string HelperAuthToken) CreateProvisionalRequestWithOffer(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return (chatGroupId, helperAuthToken);
    }
}
