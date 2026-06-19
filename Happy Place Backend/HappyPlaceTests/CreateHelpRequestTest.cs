using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CreateHelpRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = "", Topic = "I need help" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = "not-a-real-token-at-all", Topic = "I need help" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { Topic = "I need help" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Provisional Group Creation

    [Fact]
    public void CreateRequestCreatesExactlyOneChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void CreateRequestCreatesProvisionalPublicGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single();

        Assert.True(chatGroup.IsPublic);
        Assert.Equal(ChatGroupStatus.Provisional, chatGroup.Status);
    }

    [Fact]
    public void CreateRequestAddsSeekerAsActiveOwnerMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single();
        List<ChatGroupMember> members = [.. dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroup.Id)];

        Assert.Single(members);
        Assert.Equal(chatGroup.OwnerUserAccountId, members[0].UserAccountId);
        Assert.Equal(ChatGroupMemberRole.Owner, members[0].MemberRole);
        Assert.Equal(ChatGroupMemberStatus.Active, members[0].Status);
    }

    [Fact]
    public void CreateRequestUsesTopicAsChatGroupName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string topic = "I feel overwhelmed";

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(topic, dbContext.ChatGroups.Single().Name);
    }

    [Fact]
    public void CreateRequestWithBlankTopicProducesNonEmptyName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "" }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(string.IsNullOrWhiteSpace(dbContext.ChatGroups.Single().Name));
    }

    [Fact]
    public void WhitespaceOnlyTopicProducesNonEmptyName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "   " }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(string.IsNullOrWhiteSpace(dbContext.ChatGroups.Single().Name));
    }

    [Fact]
    public void MissingTopicFieldProducesNonEmptyName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(string.IsNullOrWhiteSpace(dbContext.ChatGroups.Single().Name));
    }

    [Fact]
    public void TopicOverMaxLengthIsTruncatedToMaxLength() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        string longTopic = new string('a', 150);

        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = longTopic }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(100, dbContext.ChatGroups.Single().Name.Length);
    }

    // Tests - Response Shape

    [Fact]
    public void CreateRequestReturnsWaitingStatusWithChatGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).ReadContentAsJsonDocument().RootElement;

        Assert.Equal("waiting", rootElement.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(rootElement.GetProperty("chatGroupId").GetString()));
    }

    [Fact]
    public void CreateRequestResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        var rootElement = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).ReadContentAsJsonDocument().RootElement;
        List<string> actualProperties = [.. rootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedProperties = ["chatGroupId", "chatGroupName", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - One Provisional Request Per Seeker

    [Fact]
    public void CreateRequestTwiceReturnsSameProvisionalGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string firstChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "First topic" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string secondChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Second topic" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();

        Assert.Equal(firstChatGroupId, secondChatGroupId);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void SeekerCanCreateNewRequestAfterPreviousBecameActive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        string firstChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "First" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        SetGroupStatus(firstChatGroupId, ChatGroupStatus.Active);
        string secondChatGroupId = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Second" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();

        Assert.NotEqual(firstChatGroupId, secondChatGroupId);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(2, dbContext.ChatGroups.Count());
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(secondChatGroupId)).Status);
    }

    [Fact]
    public void ConcurrentDuplicateCreateProducesOneGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());

        Thread firstThread = new(() => testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }));
        Thread secondThread = new(() => testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }));
        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count());
    }

    // Tests - Guest Group Limit

    [Fact]
    public void GuestAtGroupLimitGetsRegistrationRequired() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(guestAuthToken).Identifier);
        SeedActiveGroupMemberships(guestUserAccountId, 2);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = guestAuthToken, Topic = "Help" }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("registrationRequired", status);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(2, dbContext.ChatGroups.Count());
    }

    [Fact]
    public void GuestUnderGroupLimitCanCreateRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(guestAuthToken).Identifier);
        SeedActiveGroupMemberships(guestUserAccountId, 1);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = guestAuthToken, Topic = "Help" }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("waiting", status);
    }

    [Fact]
    public void VerifiedUserIsNeverCapped() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker " + Guid.NewGuid());
        Guid seekerUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(seekerAuthToken).Identifier);
        SeedActiveGroupMemberships(seekerUserAccountId, 2);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Help" }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("waiting", status);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(3, dbContext.ChatGroups.Count());
    }

    // Helpers

    private static void SetGroupStatus(string chatGroupId, ChatGroupStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(chatGroupId));
        chatGroup.Status = status;
        dbContext.SaveChanges();
    }

    private static void SeedActiveGroupMemberships(Guid userAccountId, int count) {
        using var dbContext = HappyPlaceDbContext.Create();
        for (int index = 0; index < count; index++) {
            Guid groupId = Guid.NewGuid();
            dbContext.ChatGroups.Add(new() { Id = groupId, Name = "Seed " + index, OwnerUserAccountId = userAccountId, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = DateTime.UtcNow });
            dbContext.ChatGroupMembers.Add(new() { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        }
        dbContext.SaveChanges();
    }
}
