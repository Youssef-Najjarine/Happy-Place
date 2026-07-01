using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class MyOpenRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void MyOpenRequestEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MyOpenRequestInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MyOpenRequestMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Returns The Caller's Open Request

    [Fact]
    public void MyOpenRequestWithProvisionalRequestReturnsWaitingWithChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("waiting", rootElement.GetProperty("status").GetString());
        Assert.Equal(chatGroupId, rootElement.GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void MyOpenRequestResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, _) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - No Open Request

    [Fact]
    public void MyOpenRequestWithNoRequestReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    [Fact]
    public void MyOpenRequestAfterCancelReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    [Fact]
    public void MyOpenRequestAfterConnectReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    // Tests - Scoped To The Caller

    [Fact]
    public void MyOpenRequestReturnsEachSeekersOwnRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (firstSeekerAuthToken, firstChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "First topic");
        var (secondSeekerAuthToken, secondChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "Second topic");

        string firstResolved = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = firstSeekerAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string secondResolved = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = secondSeekerAuthToken }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();

        Assert.Equal(firstChatGroupId, firstResolved);
        Assert.Equal(secondChatGroupId, secondResolved);
        Assert.NotEqual(firstResolved, secondResolved);
    }

    // Tests - Refreshes Freshness

    [Fact]
    public void MyOpenRequestRefreshesLastSeenAtUtc() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        SetGroupLastSeenAtUtc(chatGroupId, DateTime.UtcNow.AddMinutes(-30));

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = seekerAuthToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        Assert.True(chatGroup.LastSeenAtUtc > DateTime.UtcNow.AddMinutes(-1));
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

    private static void SetGroupLastSeenAtUtc(string chatGroupId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }
}
