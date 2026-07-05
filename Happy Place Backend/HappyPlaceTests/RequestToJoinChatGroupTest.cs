using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RequestToJoinChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void RequestToJoinEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RequestToJoinInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RequestToJoinMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Requesting A Private Group

    [Fact]
    public void RequestToJoinPrivateGroupCreatesPendingMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.True(PendingMembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void RequestReturnsRequestedStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("requested", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RequestDoesNotAddActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal(1, CountActiveMembers(groupId));
    }

    [Fact]
    public void RequestingWhenAlreadyRequestedReturnsAlreadyRequested() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("alreadyRequested", root.GetProperty("status").GetString());
        Assert.Equal(2, CountMembers(groupId));
    }

    // Tests - Requesting When Already A Member

    [Fact]
    public void RequestingWhenAlreadyActiveMemberReturnsAlreadyMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = RequestToJoin(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("alreadyMember", root.GetProperty("status").GetString());
        Assert.Equal(2, CountMembers(groupId));
    }

    [Fact]
    public void OwnerRequestingOwnGroupReturnsAlreadyMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("alreadyMember", root.GetProperty("status").GetString());
    }

    // Tests - Requests That Are Not Allowed

    [Fact]
    public void RequestingPublicGroupReturnsNoneAndCreatesNoRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Public Group", true);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.False(PendingMembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void RequestingUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, Guid.NewGuid());

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RequestingProvisionalGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateProvisionalGroup(SeedUser("Owner", null), "Waiting For Help", false);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    // Tests - Effect On Feed

    [Fact]
    public void RequesterSeesGroupWithJoinRequestTrue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);
        JsonElement group = GetGroupFromList(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.True(group.GetProperty("joinRequest").GetBoolean());
        Assert.False(group.GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void RequestedUserAppearsAsPendingToOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);
        JsonElement group = GetGroupFromList(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.True(group.GetProperty("pendingMembers").GetBoolean());
    }

    // Tests - Response Shape

    [Fact]
    public void RequestToJoinResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        JsonElement root = RequestToJoin(testingMockProvidersContainer, requesterAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Concurrency And Isolation

    [Fact]
    public void ConcurrentDuplicateRequestsCreateOnePendingRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        List<Exception> exceptions = RunConcurrently(
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.Equal(1, CountMembershipRows(groupId, requesterUserAccountId));
    }

    [Fact]
    public void ConcurrentRequestAndCancelNeverCorruptsMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        List<Exception> exceptions = RunConcurrently(
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.True(CountMembershipRows(groupId, requesterUserAccountId) <= 1);
        Assert.False(ActiveMembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void MultipleUsersCanRequestTheSameGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string firstRequesterAuthToken = CreateUser(testingMockProvidersContainer, "First Requester");
        string secondRequesterAuthToken = CreateUser(testingMockProvidersContainer, "Second Requester");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        RequestToJoin(testingMockProvidersContainer, firstRequesterAuthToken, groupId);
        RequestToJoin(testingMockProvidersContainer, secondRequesterAuthToken, groupId);

        Assert.True(PendingMembershipExists(groupId, ResolveUserAccountId(firstRequesterAuthToken)));
        Assert.True(PendingMembershipExists(groupId, ResolveUserAccountId(secondRequesterAuthToken)));
        Assert.Equal(3, CountMembers(groupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement RequestToJoin(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement GetGroupFromList(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
        string target = chatGroupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Chat group was not present in the response.");
    }

    private static List<Exception> RunConcurrently(params Action[] actions) {
        List<Exception> caughtExceptions = [];
        List<Thread> threads = [];
        foreach (Action action in actions) {
            Thread thread = new(() => {
                try { action(); }
                catch (Exception exception) { lock (caughtExceptions) { caughtExceptions.Add(exception); } }
            });
            threads.Add(thread);
        }
        foreach (Thread thread in threads) thread.Start();
        foreach (Thread thread in threads) thread.Join();
        return caughtExceptions;
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static Guid SeedUser(string displayName, string profilePhotoUrl) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName, IsAnonymous = false, CreatedAtUtc = DateTime.UtcNow, ProfilePhotoUrl = profilePhotoUrl });
        dbContext.SaveChanges();
        return userAccountId;
    }

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        return CreateGroup(ownerUserAccountId, name, isPublic, ChatGroupStatus.Active);
    }

    private static Guid CreateProvisionalGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        return CreateGroup(ownerUserAccountId, name, isPublic, ChatGroupStatus.Provisional);
    }

    private static Guid CreateGroup(Guid ownerUserAccountId, string name, bool isPublic, ChatGroupStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = status, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool PendingMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static int CountActiveMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.Status == ChatGroupMemberStatus.Active);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }

    private static int CountMembershipRows(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }

    private static bool ActiveMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active);
    }
}
