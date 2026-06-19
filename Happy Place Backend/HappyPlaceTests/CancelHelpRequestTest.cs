using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CancelHelpRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void CancelEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CancelInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CancelMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Cancelling A Provisional Request

    [Fact]
    public void CancelRemovesProvisionalGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("cancelled", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void CancelRemovesOwnerMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.ChatGroupMembers.Count());
    }

    [Fact]
    public void CancelRemovesOffersOnTheRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void CancelFreesSeekerToCreateNewRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string firstChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "First" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        string secondChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Second" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();

        Assert.NotEqual(firstChatGroupId, secondChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void CancelIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        string secondStatus = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", secondStatus);
    }

    [Fact]
    public void CancelResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Only The Owner May Cancel A Provisional Request

    [Fact]
    public void CancelOnForeignGroupReturnsNoneAndLeavesItIntact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single().Status);
    }

    [Fact]
    public void CancelOnActiveGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Active, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    [Fact]
    public void CancelOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
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
}
