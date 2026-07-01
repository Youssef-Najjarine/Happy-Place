using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ConnectHelpRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void ConnectEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ConnectInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ConnectMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Connecting Invites Every Offer

    [Fact]
    public void ConnectFlipsGroupToActive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Active, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    [Fact]
    public void ConnectMarksEveryOfferedOfferConnected() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "First Helper " + Guid.NewGuid());
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Second Helper " + Guid.NewGuid());
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Third Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(3, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.Status == HelpOfferStatus.Connected));
        Assert.Equal(0, dbContext.HelpOffers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.Status != HelpOfferStatus.Connected));
    }

    [Fact]
    public void ConnectReleasesNoOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "First Helper " + Guid.NewGuid());
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Second Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.HelpOffers.Count(field => field.Status == HelpOfferStatus.Released));
    }

    [Fact]
    public void ConnectAddsNoMembersBeyondTheOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "First Helper " + Guid.NewGuid());
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Second Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        List<ChatGroupMember> members = [.. dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == Guid.Parse(chatGroupId))];
        Assert.Single(members);
        Assert.Equal(ChatGroupMemberRole.Owner, members[0].MemberRole);
    }

    [Fact]
    public void ConnectInvitesEvenStaleOffers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        SetOfferLastSeenAtUtc(chatGroupId, helperUserAccountId, DateTime.UtcNow.AddMinutes(-30));

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(HelpOfferStatus.Connected, dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId).Status);
    }

    [Fact]
    public void ConnectReturnsConnectedWithChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("connected", rootElement.GetProperty("status").GetString());
        Assert.Equal(chatGroupId, rootElement.GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void ConnectResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Connecting With Nothing To Connect To

    [Fact]
    public void ConnectWithNoOffersReturnsNoOffersAndStaysProvisional() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("noOffers", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    // Tests - Idempotent Double Tap

    [Fact]
    public void ConnectTwiceReturnsConnectedWithoutAddingExtraMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        string secondStatus = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("connected", secondStatus);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId)));
    }

    // Tests - Only The Owner May Connect

    [Fact]
    public void ConnectOnForeignGroupReturnsNoneAndLeavesGroupProvisional() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "I need help");
        OfferOnRequest(testingMockProvidersContainer, chatGroupId, "Helper " + Guid.NewGuid());
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId)).Status);
    }

    [Fact]
    public void ConnectOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("none", status);
    }

    // Tests - A Helper May Be Invited To Multiple Groups

    [Fact]
    public void HelperOfferedToTwoSeekersIsInvitedToBothOnConnect() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (firstSeekerAuthToken, firstChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "First request");
        var (secondSeekerAuthToken, secondChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Shared Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = firstSeekerAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = secondSeekerAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(HelpOfferStatus.Connected, dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId && field.ChatGroupId == Guid.Parse(firstChatGroupId)).Status);
        Assert.Equal(HelpOfferStatus.Connected, dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId && field.ChatGroupId == Guid.Parse(secondChatGroupId)).Status);
    }

    [Fact]
    public void ConnectingOneRequestLeavesTheHelpersOtherOfferUntouched() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (firstSeekerAuthToken, firstChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "First request");
        var (_, secondChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = firstSeekerAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(HelpOfferStatus.Connected, dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId && field.ChatGroupId == Guid.Parse(firstChatGroupId)).Status);
        Assert.Equal(HelpOfferStatus.Offered, dbContext.HelpOffers.Single(field => field.HelperUserAccountId == helperUserAccountId && field.ChatGroupId == Guid.Parse(secondChatGroupId)).Status);
    }

    // Tests - Concurrency

    [Fact]
    public void TwoSeekersSharingOneHelperBothConnectIndependently() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (firstSeekerAuthToken, firstChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "First request");
        var (secondSeekerAuthToken, secondChatGroupId) = CreateSeekerWithRequest(testingMockProvidersContainer, "Second request");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Shared Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = firstChatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = secondChatGroupId }).EnsureSuccessStatusCode();

        HttpResponseMessage firstResponse = null;
        HttpResponseMessage secondResponse = null;
        Thread firstThread = new(() => firstResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = firstSeekerAuthToken, ChatGroupId = firstChatGroupId }));
        Thread secondThread = new(() => secondResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = secondSeekerAuthToken, ChatGroupId = secondChatGroupId }));
        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();

        List<string> statuses = [firstResponse.ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString(), secondResponse.ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString()];
        Assert.Equal(2, statuses.Count(status => status == "connected"));

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(2, dbContext.HelpOffers.Count(field => field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Connected));
        Assert.Equal(0, dbContext.ChatGroupMembers.Count(field => field.UserAccountId == helperUserAccountId));
        Assert.Equal(2, dbContext.ChatGroups.Count(field => (field.Id == Guid.Parse(firstChatGroupId) || field.Id == Guid.Parse(secondChatGroupId)) && field.Status == ChatGroupStatus.Active));
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
