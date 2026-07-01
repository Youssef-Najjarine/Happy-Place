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

    // Tests - Nothing To Join Yet

    [Fact]
    public void PollWithNoOffersReturnsEmpty() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    [Fact]
    public void PollWithOfferedButNotStartedReturnsEmpty() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    // Tests - A Started Group Surfaces To Its Invited Helper

    [Fact]
    public void PollAfterGroupStartedContainsTheGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(1, rootElement.GetArrayLength());
        Assert.Equal(chatGroupId, rootElement[0].GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void PollReturnsAllStartedGroupsNotYetJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (firstSeekerAuthToken, firstChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "First request");
        var (secondSeekerAuthToken, secondChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = firstSeekerAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = secondSeekerAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> chatGroupIds = [.. rootElement.EnumerateArray().Select(element => element.GetProperty("chatGroupId").GetString())];

        Assert.Equal(2, rootElement.GetArrayLength());
        Assert.Contains(firstChatGroupId, chatGroupIds);
        Assert.Contains(secondChatGroupId, chatGroupIds);
    }

    // Tests - Joining Drops The Group From The Poll

    [Fact]
    public void PollAfterJoiningDropsTheGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    // Tests - Response Shape

    [Fact]
    public void PollOfferItemContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers

    private static (string AuthToken, string ChatGroupId) CreateSeekerWithRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return (seekerAuthToken, chatGroupId);
    }
}
