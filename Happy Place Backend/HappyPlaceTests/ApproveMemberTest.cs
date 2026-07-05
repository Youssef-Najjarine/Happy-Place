using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ApproveMemberTest {
    // Tests - Authentication Failures

    [Fact]
    public void ApproveEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ApproveInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ApproveMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Approves A Pending Member

    [Fact]
    public void OwnerApprovesPendingMemberBecomesActive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.True(ActiveMembershipExists(groupId, pendingUserAccountId));
        Assert.False(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void ApproveReturnsApprovedStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("approved", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ApprovedMemberAppearsAsJoinedInTheirFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, memberUserAccountId);

        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);
        JsonElement group = GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.True(group.GetProperty("joined").GetBoolean());
        Assert.False(group.GetProperty("joinRequest").GetBoolean());
    }

    [Fact]
    public void OwnerNoLongerSeesPendingAfterApproval() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        JsonElement group = GetGroupFromList(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(group.GetProperty("pendingMembers").GetBoolean());
    }

    // Tests - Idempotency And Non Pending Targets

    [Fact]
    public void ApprovingAlreadyActiveMemberReturnsAlreadyMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("alreadyMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ApprovingIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);
        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("alreadyMember", root.GetProperty("status").GetString());
        Assert.Equal(1, CountMembershipRows(groupId, pendingUserAccountId));
    }

    [Fact]
    public void ApprovingNonPendingReturnsNotPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid strangerUserAccountId = SeedUser("Stranger", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, strangerUserAccountId);

        Assert.Equal("notPending", root.GetProperty("status").GetString());
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotApproveReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, strangerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void MemberCannotApproveAnotherPendingUser() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, memberAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void ApproveOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ApproveOnProvisionalGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentApproveAndCancelResolveConsistently() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);

        List<Exception> exceptions = RunConcurrently(
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MemberUserAccountId = requesterUserAccountId }).EnsureSuccessStatusCode(),
            () => testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.True(CountMembershipRows(groupId, requesterUserAccountId) <= 1);
        Assert.False(PendingMembershipExists(groupId, requesterUserAccountId));
    }

    // Tests - Response Shape

    [Fact]
    public void ApproveResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement ApproveMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static bool ActiveMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active);
    }

    private static bool PendingMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static int CountMembershipRows(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }
}
