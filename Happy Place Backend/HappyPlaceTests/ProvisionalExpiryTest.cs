using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ProvisionalExpiryTest {
    // Tests - Stale Provisional Requests Are Swept When A Helper Browses

    [Fact]
    public void StaleProvisionalIsRemovedFromFeedAndDeletedWhenHelperBrowses() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        SetGroupLastSeenAtUtc(chatGroupId, DateTime.UtcNow.AddMinutes(-10));
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        int feedLength = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement.GetArrayLength();

        Assert.Equal(0, feedLength);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void FreshProvisionalIsListedAndKeptWhenHelperBrowses() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        int feedLength = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement.GetArrayLength();

        Assert.Equal(1, feedLength);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void ActiveGroupIsNotSweptDespiteStaleHeartbeat() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        SetGroupLastSeenAtUtc(chatGroupId, DateTime.UtcNow.AddMinutes(-10));
        string browsingHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Browser " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = browsingHelperAuthToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Active, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    // Tests - Heartbeat Keeps A Provisional Request Alive

    [Fact]
    public void PollRequestRefreshesProvisionalHeartbeat() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        DateTime staleLastSeen = DateTime.UtcNow.AddMinutes(-10);
        SetGroupLastSeenAtUtc(chatGroupId, staleLastSeen);

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).LastSeenAtUtc > staleLastSeen);
    }

    [Fact]
    public void CreateRequestResumeRefreshesProvisionalHeartbeat() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        DateTime staleLastSeen = DateTime.UtcNow.AddMinutes(-10);
        SetGroupLastSeenAtUtc(chatGroupId, staleLastSeen);

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I still need help" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).LastSeenAtUtc > staleLastSeen);
    }

    // Tests - Sweep Cascades To Dependents

    [Fact]
    public void StaleProvisionalSweepCascadesToOwnerMemberAndOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Offering Helper " + Guid.NewGuid());
        SetGroupLastSeenAtUtc(chatGroupId, DateTime.UtcNow.AddMinutes(-10));
        string browsingHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Browser " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = browsingHelperAuthToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.ChatGroups.Count());
        Assert.Equal(0, dbContext.ChatGroupMembers.Count());
        Assert.Equal(0, dbContext.HelpOffers.Count());
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
