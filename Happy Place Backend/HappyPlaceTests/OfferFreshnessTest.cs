using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class OfferFreshnessTest {
    // Tests - Stale Offers Are Not Connectable

    [Fact]
    public void ConnectSkipsStaleOfferAndPicksFreshOne() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string staleHelperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Stale Helper " + Guid.NewGuid());
        Guid staleHelperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(staleHelperAuthToken).Identifier);
        string freshHelperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Fresh Helper " + Guid.NewGuid());
        Guid freshHelperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(freshHelperAuthToken).Identifier);
        SetOfferLastSeenAtUtc(chatGroupId, staleHelperUserAccountId, DateTime.UtcNow.AddMinutes(-10));

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer connectedOffer = dbContext.HelpOffers.Single(field => field.Status == HelpOfferStatus.Connected);
        Assert.Equal(freshHelperUserAccountId, connectedOffer.HelperUserAccountId);
    }

    [Fact]
    public void ConnectWithOnlyStaleOffersReturnsNoOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        SetOfferLastSeenAtUtc(chatGroupId, helperUserAccountId, DateTime.UtcNow.AddMinutes(-10));

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("noOffers", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    // Tests - Stale Offers Are Not Counted As Ready

    [Fact]
    public void ReadyHelperCountExcludesStaleOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string staleHelperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Stale Helper " + Guid.NewGuid());
        Guid staleHelperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(staleHelperAuthToken).Identifier);
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Fresh Helper " + Guid.NewGuid());
        SetOfferLastSeenAtUtc(chatGroupId, staleHelperUserAccountId, DateTime.UtcNow.AddMinutes(-10));

        int readyHelperCount = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("readyHelperCount").GetInt32();

        Assert.Equal(1, readyHelperCount);
    }

    // Tests - Polling Refreshes The Offer Heartbeat

    [Fact]
    public void PollOfferRefreshesOfferHeartbeat() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        DateTime staleLastSeen = DateTime.UtcNow.AddMinutes(-10);
        SetOfferLastSeenAtUtc(chatGroupId, helperUserAccountId, staleLastSeen);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId).LastSeenAtUtc > staleLastSeen);
    }

    [Fact]
    public void RefreshedOfferRemainsClaimableOnConnect() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        SetOfferLastSeenAtUtc(chatGroupId, helperUserAccountId, DateTime.UtcNow.AddMinutes(-10));

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = helperAuthToken }).EnsureSuccessStatusCode();
        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("connected", status);
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

    private static void SetOfferLastSeenAtUtc(string chatGroupId, Guid helperUserAccountId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.HelperUserAccountId == helperUserAccountId);
        offer.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }
}
