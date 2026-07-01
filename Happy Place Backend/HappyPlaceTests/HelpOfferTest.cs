using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HelpOfferTest {
    // Tests - Authentication Failures (Create Offer)

    [Fact]
    public void CreateOfferEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CreateOfferInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CreateOfferMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Authentication Failures (Decline Offer)

    [Fact]
    public void DeclineOfferEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeclineOfferInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeclineOfferMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Authentication Failures (Open Requests)

    [Fact]
    public void OpenRequestsEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OpenRequestsInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OpenRequestsMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Creating An Offer

    [Fact]
    public void CreateOfferCreatesOfferedOffer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.Single();
        Assert.Equal(Guid.Parse(chatGroupId), offer.ChatGroupId);
        Assert.Equal(helperUserAccountId, offer.HelperUserAccountId);
        Assert.Equal(HelpOfferStatus.Offered, offer.Status);
    }

    [Fact]
    public void CreateOfferIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void CreateOfferOnNonProvisionalGroupReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        SetGroupStatus(chatGroupId, ChatGroupStatus.Active);
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void CreateOfferOnOwnRequestReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
    }

    [Fact]
    public void CreateOfferOnUnknownGroupReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
    }

    [Fact]
    public void TwoHelpersCanOfferOnSameRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string firstHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "First Helper " + Guid.NewGuid());
        string secondHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Second Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = firstHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = secondHelperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(2, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId)));
    }

    [Fact]
    public void HelperCanOfferOnTwoDifferentRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string firstChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "First request");
        string secondChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(2, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void ConcurrentDuplicateOfferProducesOneOffer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        Thread firstThread = new(() => testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }));
        Thread secondThread = new(() => testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }));
        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void CreateOfferResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Declining A Request

    [Fact]
    public void DeclineOfferRecordsDeclinedOffer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(HelpOfferStatus.Declined, dbContext.HelpOffers.Single().Status);
    }

    [Fact]
    public void DeclineOfferIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count());
        Assert.Equal(HelpOfferStatus.Declined, dbContext.HelpOffers.Single().Status);
    }

    [Fact]
    public void DeclineThenOfferEndsAsOffered() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count());
        Assert.Equal(HelpOfferStatus.Offered, dbContext.HelpOffers.Single().Status);
    }

    [Fact]
    public void DeclineOfferOnNonProvisionalGroupReturnsRequestClosed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        SetGroupStatus(chatGroupId, ChatGroupStatus.Active);
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("requestClosed", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count());
    }

    // Tests - Open Requests Feed

    [Fact]
    public void OpenRequestsListsOtherSeekersProvisionalRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(1, rootElement.GetArrayLength());
        Assert.Equal(chatGroupId, rootElement[0].GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void OwnRequestExcludedFromOpenRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = seekerAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    [Fact]
    public void AlreadyOfferedRequestAppearsTaggedOffered() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(1, rootElement.GetArrayLength());
        Assert.Equal(chatGroupId, rootElement[0].GetProperty("chatGroupId").GetString());
        Assert.Equal("offered", rootElement[0].GetProperty("offerStatus").GetString());
    }

    [Fact]
    public void AvailableRequestIsTaggedNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(1, rootElement.GetArrayLength());
        Assert.Equal("none", rootElement[0].GetProperty("offerStatus").GetString());
    }

    [Fact]
    public void DeclinedRequestExcludedFromOpenRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    [Fact]
    public void ActiveGroupExcludedFromOpenRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        SetGroupStatus(chatGroupId, ChatGroupStatus.Active);
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(0, rootElement.GetArrayLength());
    }

    [Fact]
    public void MultipleSeekersRequestsAllAppearInOpenRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string firstChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "First request");
        string secondChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> chatGroupIds = [.. rootElement.EnumerateArray().Select(element => element.GetProperty("chatGroupId").GetString())];

        Assert.Equal(2, rootElement.GetArrayLength());
        Assert.Contains(firstChatGroupId, chatGroupIds);
        Assert.Contains(secondChatGroupId, chatGroupIds);
    }

    [Fact]
    public void OpenRequestsAreOrderedOldestFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string olderChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "Older");
        string newerChatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "Newer");
        SetGroupCreatedAtUtc(olderChatGroupId, DateTime.UtcNow.AddMinutes(-10));
        SetGroupCreatedAtUtc(newerChatGroupId, DateTime.UtcNow.AddMinutes(-1));
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(2, rootElement.GetArrayLength());
        Assert.Equal(olderChatGroupId, rootElement[0].GetProperty("chatGroupId").GetString());
        Assert.Equal(newerChatGroupId, rootElement[1].GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void OpenRequestItemsContainExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "createdAtUtc", "offerStatus"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers

    private static string CreateProvisionalRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        return testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
    }

    private static void SetGroupStatus(string chatGroupId, ChatGroupStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.Status = status;
        dbContext.SaveChanges();
    }

    private static void SetGroupCreatedAtUtc(string chatGroupId, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.CreatedAtUtc = createdAtUtc;
        dbContext.SaveChanges();
    }
}
