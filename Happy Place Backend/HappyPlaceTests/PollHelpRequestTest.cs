using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class PollHelpRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void PollRequestEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollRequestInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollRequestMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Waiting State

    [Fact]
    public void PollWhileProvisionalWithNoOffersReturnsWaitingWithZeroReady() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("waiting", rootElement.GetProperty("status").GetString());
        Assert.Equal(0, rootElement.GetProperty("readyHelperCount").GetInt32());
    }

    [Fact]
    public void PollWhileProvisionalWithOffersReturnsReadyCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "First Helper " + Guid.NewGuid());
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Second Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("waiting", rootElement.GetProperty("status").GetString());
        Assert.Equal(2, rootElement.GetProperty("readyHelperCount").GetInt32());
    }

    [Fact]
    public void PollReadyCountExcludesDeclinedOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Offering Helper " + Guid.NewGuid());
        DeclineOnRequest(testingMockProvidersContainer, chatGroupId, "Declining Helper " + Guid.NewGuid());

        int readyHelperCount = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("readyHelperCount").GetInt32();

        Assert.Equal(1, readyHelperCount);
    }

    [Fact]
    public void PollReadyCountIncludesStaleOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string staleHelperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Stale Helper " + Guid.NewGuid());
        Guid staleHelperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(staleHelperAuthToken).Identifier);
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Fresh Helper " + Guid.NewGuid());
        SetOfferLastSeenAtUtc(chatGroupId, staleHelperUserAccountId, DateTime.UtcNow.AddMinutes(-30));

        int readyHelperCount = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("readyHelperCount").GetInt32();

        Assert.Equal(2, readyHelperCount);
    }

    [Fact]
    public void PollWhileWaitingRefreshesLastSeenAtUtc() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        SetGroupLastSeenAtUtc(chatGroupId, DateTime.UtcNow.AddMinutes(-30));

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        Assert.True(chatGroup.LastSeenAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    // Tests - Connected State

    [Fact]
    public void PollAfterConnectReturnsConnected() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("connected", status);
    }

    // Tests - Only The Owner May Poll

    [Fact]
    public void PollOnForeignGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    [Fact]
    public void PollOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    // Tests - Response Shape

    [Fact]
    public void PollRequestResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "readyHelperCount", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers

    private static (string AuthToken, string ChatGroupId) CreateSeekerWithRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return (seekerAuthToken, chatGroupId);
    }

    private static string OfferOnRequest(TestingMockProvidersContainer testingMockProvidersContainer, string chatGroupId, string helperName) {
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, helperName);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return helperAuthToken;
    }

    private static void DeclineOnRequest(TestingMockProvidersContainer testingMockProvidersContainer, string chatGroupId, string helperName) {
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, helperName);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void SetOfferLastSeenAtUtc(string chatGroupId, Guid helperUserAccountId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId);
        offer.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }

    private static void SetGroupLastSeenAtUtc(string chatGroupId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }
}
