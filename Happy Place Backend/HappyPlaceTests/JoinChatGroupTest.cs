using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class JoinChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void JoinEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void JoinInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void JoinMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Joining A Started Public Group

    [Fact]
    public void JoinActivePublicGroupAddsActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroupMember member = dbContext.ChatGroupMembers.Single(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == helperUserAccountId);
        Assert.Equal(ChatGroupMemberRole.Member, member.MemberRole);
        Assert.Equal(ChatGroupMemberStatus.Active, member.Status);
    }

    [Fact]
    public void JoinIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == helperUserAccountId));
    }

    [Fact]
    public void JoinReturnsJoinedWithChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("joined", rootElement.GetProperty("status").GetString());
        Assert.Equal(chatGroupId, rootElement.GetProperty("chatGroupId").GetString());
    }

    [Fact]
    public void NonOffererCanJoinPublicStartedGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, _) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());
        Guid strangerUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(strangerAuthToken).Identifier);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("joined", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == strangerUserAccountId));
    }

    [Fact]
    public void OwnerJoiningOwnGroupStaysSingleMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, seekerAuthToken, _) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        Guid seekerUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(seekerAuthToken).Identifier);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("joined", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == seekerUserAccountId));
    }

    [Fact]
    public void JoinResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Nothing To Join

    [Fact]
    public void JoinProvisionalGroupReturnsUnavailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string chatGroupId = CreateProvisionalRequest(testingMockProvidersContainer, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("unavailable", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(0, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == helperUserAccountId));
    }

    [Fact]
    public void JoinUnknownGroupReturnsUnavailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("unavailable", status);
    }

    // Tests - Private Groups Are Invite Only

    [Fact]
    public void JoinPrivateGroupWithoutInviteReturnsUnavailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, _) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        SetGroupIsPublic(chatGroupId, false);
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Stranger " + Guid.NewGuid());

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = strangerAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("unavailable", status);
    }

    [Fact]
    public void JoinPrivateGroupWithConnectedOfferReturnsJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        var (chatGroupId, _, helperAuthToken) = StartGroupWithOfferer(testingMockProvidersContainer, "I need help");
        SetGroupIsPublic(chatGroupId, false);
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("joined", status);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == Guid.Parse(chatGroupId) && field.UserAccountId == helperUserAccountId));
    }

    // Helpers

    private static (string ChatGroupId, string SeekerAuthToken, string HelperAuthToken) StartGroupWithOfferer(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string chatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return (chatGroupId, seekerAuthToken, helperAuthToken);
    }

    private static string CreateProvisionalRequest(TestingMockProvidersContainer testingMockProvidersContainer, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        return testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
    }

    private static void SetGroupIsPublic(string chatGroupId, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.IsPublic = isPublic;
        dbContext.SaveChanges();
    }
}
