using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class LeaveChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void LeaveEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void LeaveInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void LeaveMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Members Leaving

    [Fact]
    public void ActiveMemberCanLeaveRemovesMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.False(MembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void LeaveReturnsLeftStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("left", root.GetProperty("status").GetString());
    }

    [Fact]
    public void LeavingDoesNotAffectOtherMembersOrGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string leavingMemberAuthToken = CreateUser(testingMockProvidersContainer, "Leaving Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        Guid stayingMemberUserAccountId = SeedUser("Staying Member", null);
        AddActiveMember(groupId, ResolveUserAccountId(leavingMemberAuthToken));
        AddActiveMember(groupId, stayingMemberUserAccountId);

        Leave(testingMockProvidersContainer, leavingMemberAuthToken, groupId);

        Assert.True(GroupExists(groupId));
        Assert.True(MembershipExists(groupId, stayingMemberUserAccountId));
        Assert.Equal(2, CountMembers(groupId));
    }

    [Fact]
    public void LeavingIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Owner And Non Members

    [Fact]
    public void OwnerCannotLeaveReturnsOwnerCannotLeave() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("ownerCannotLeave", root.GetProperty("status").GetString());
    }

    [Fact]
    public void OwnerRemainsMemberAfterLeaveAttempt() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

        Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.True(MembershipExists(groupId, ownerUserAccountId));
    }

    [Fact]
    public void StrangerCannotLeaveReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberCannotLeaveReturnsNotMemberAndKeepsRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);

        JsonElement root = Leave(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.True(MembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void LeaveUnknownGroupReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid());

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Effect On Feed

    [Fact]
    public void LeftMemberNoLongerJoinedInPrivateGroupFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Assert.True(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.False(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void LeavingPublicGroupStillVisibleAsDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Public Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
        Assert.False(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());
    }

    // Tests - Response Shape

    [Fact]
    public void LeaveResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Concurrency And Isolation

    [Fact]
    public void ConcurrentLeavesRemoveMembershipExactlyOnce() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        List<Exception> exceptions = RunConcurrently(
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.False(MembershipExists(groupId, memberUserAccountId));
        Assert.True(GroupExists(groupId));
    }

    [Fact]
    public void LeavingOneGroupDoesNotAffectMembershipInAnother() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid firstGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Group A", true);
        Guid secondGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Group B", true);
        AddActiveMember(firstGroupId, memberUserAccountId);
        AddActiveMember(secondGroupId, memberUserAccountId);

        Leave(testingMockProvidersContainer, memberAuthToken, firstGroupId);

        Assert.False(MembershipExists(firstGroupId, memberUserAccountId));
        Assert.True(MembershipExists(secondGroupId, memberUserAccountId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Leave(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static bool ListContainsGroup(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement;
        string target = chatGroupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
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
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Active);
    }

    private static void AddPendingMember(Guid groupId, Guid userAccountId) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Pending);
    }

    private static void AddMember(Guid groupId, Guid userAccountId, ChatGroupMemberStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool MembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }

    private static bool GroupExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }
}
