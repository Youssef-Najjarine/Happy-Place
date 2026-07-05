using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RemoveMemberTest {
    // Tests - Authentication Failures

    [Fact]
    public void RemoveEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RemoveInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RemoveMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Removes An Active Member

    [Fact]
    public void OwnerRemovesActiveMemberRemovesMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.False(MembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void RemoveReturnsRemovedStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("removed", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RemovedMemberNoLongerInMembersList() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);
        JsonElement members = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members");

        Assert.False(ContainsUser(members, memberUserAccountId));
    }

    [Fact]
    public void RemovedMemberNoLongerSeesPrivateGroupInFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);
        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
    }

    [Fact]
    public void RemovingFromPublicGroupWorks() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("removed", root.GetProperty("status").GetString());
        Assert.False(MembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void RemovingIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);
        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Owner Cannot Remove Themselves

    [Fact]
    public void OwnerCannotRemoveSelfReturnsCannotRemoveOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, ownerUserAccountId);

        Assert.Equal("cannotRemoveOwner", root.GetProperty("status").GetString());
    }

    [Fact]
    public void OwnerMembershipSurvivesSelfRemoveAttempt() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, ownerUserAccountId);

        Assert.True(MembershipExists(groupId, ownerUserAccountId));
    }

    // Tests - Targets That Are Not Active Members

    [Fact]
    public void RemovingPendingMemberReturnsNotMemberAndKeepsRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void RemovingStrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid strangerUserAccountId = SeedUser("Stranger", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, strangerUserAccountId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RemovingUnknownUserReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid());

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotRemoveReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, strangerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(MembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void MemberCannotRemoveAnotherMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid targetMemberUserAccountId = SeedUser("Target Member", null);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        AddActiveMember(groupId, targetMemberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, memberAuthToken, groupId, targetMemberUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(MembershipExists(groupId, targetMemberUserAccountId));
    }

    [Fact]
    public void RemoveOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RemoveOnProvisionalGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(MembershipExists(groupId, memberUserAccountId));
    }

    // Tests - Isolation And Concurrency

    [Fact]
    public void RemovingOneMemberDoesNotAffectOthers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid removedMemberUserAccountId = SeedUser("Removed Member", null);
        Guid stayingMemberUserAccountId = SeedUser("Staying Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, removedMemberUserAccountId);
        AddActiveMember(groupId, stayingMemberUserAccountId);

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, removedMemberUserAccountId);

        Assert.False(MembershipExists(groupId, removedMemberUserAccountId));
        Assert.True(MembershipExists(groupId, stayingMemberUserAccountId));
        Assert.True(MembershipExists(groupId, ResolveUserAccountId(ownerAuthToken)));
    }

    [Fact]
    public void ConcurrentRemoveAndLeaveRemoveMembershipCleanly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        List<Exception> exceptions = RunConcurrently(
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.False(MembershipExists(groupId, memberUserAccountId));
        Assert.True(GroupExists(groupId));
    }

    // Tests - Response Shape

    [Fact]
    public void RemoveResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement RemoveMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement ListMembers(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static bool ListContainsGroup(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement;
        string target = chatGroupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
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

    private static bool PendingMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static bool GroupExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static bool ContainsUser(JsonElement arrayElement, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("userAccountId").GetString() == target)
                return true;
        return false;
    }
}
