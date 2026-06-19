using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class PollHelpOfferTest {
    // Tests - Authentication Failures

    [Fact]
    public void PollOfferEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollOfferInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollOfferMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - No Pending Offer

    [Fact]
    public void PollWithNoOffersReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    // Tests - Waiting After Offering

    [Fact]
    public void PollAfterOfferingReturnsOffered() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("offered", status);
    }

    // Tests - Connected State

    [Fact]
    public void PollAfterBeingConnectedReturnsConnectedWithChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("connected", rootElement.GetProperty("status").GetString());
        Assert.Equal(chatGroupId, rootElement.GetProperty("chatGroupId").GetString());
    }

    // Tests - Released After Not Being Chosen

    [Fact]
    public void PollAfterBeingReleasedReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string chosenHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Chosen Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = chosenHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        string releasedHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Released Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = releasedHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = releasedHelperAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    // Tests - Response Shape

    [Fact]
    public void PollOfferConnectedResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers

    private static (string AuthToken, string ChatGroupId) CreateSeekerWithRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return (seekerAuthToken, chatGroupId);
    }
}
