using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class WithdrawHelpOfferTest {
    // Tests - Authentication Failures

    [Fact]
    public void WithdrawOfferEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void WithdrawOfferInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void WithdrawOfferMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Withdrawing An Offer

    [Fact]
    public void WithdrawDeletesTheOfferedOffer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId));
    }

    [Fact]
    public void WithdrawReturnsWithdrawn() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("withdrawn", status);
    }

    [Fact]
    public void WithdrawWithNoOfferReturnsWithdrawn() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("withdrawn", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count());
    }

    [Fact]
    public void WithdrawnRequestReappearsAsAvailableInFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(1, rootElement.GetArrayLength());
        Assert.Equal(chatGroupId, rootElement[0].GetProperty("chatGroupId").GetString());
        Assert.Equal("none", rootElement[0].GetProperty("offerStatus").GetString());
    }

    [Fact]
    public void WithdrawnOfferCanBeReOffered() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId)));
        Assert.Equal(HelpOfferStatus.Offered, dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId)).Status);
    }

    [Fact]
    public void WithdrawAfterGroupStartedLeavesConnectedOfferIntact() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(HelpOfferStatus.Connected, dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId).Status);
    }

    // Helpers

    private static string CreateProvisionalRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        return testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
    }
}
