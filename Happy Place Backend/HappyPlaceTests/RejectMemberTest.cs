using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RejectMemberTest {
    // Tests - Authentication Failures

    [Fact]
    public void RejectEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RejectInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RejectMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rejectMember", new { ChatGroupId = Guid.NewGuid(), MemberUserAccountId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Rejects A Pending Member

    [Fact]
    public void OwnerRejectsPendingMemberRemovesRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.False(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void RejectReturnsRejectedStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("rejected", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RejectedMemberNoLongerSeesPrivateGroupInFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);
        Assert.True(ListContainsGroup(testingMockProvidersContainer, requesterAuthToken, groupId));

        RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, requesterUserAccountId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, requesterAuthToken, groupId));
    }

    [Fact]
    public void OwnerNoLongerSeesPendingAfterRejection() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        JsonElement group = GetGroupFromList(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(group.GetProperty("pendingMembers").GetBoolean());
    }

    // Tests - Non Pending Targets

    [Fact]
    public void RejectingNonPendingReturnsNotPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid strangerUserAccountId = SeedUser("Stranger", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, strangerUserAccountId);

        Assert.Equal("notPending", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RejectingActiveMemberReturnsNotPendingAndKeepsMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);

        Assert.Equal("notPending", root.GetProperty("status").GetString());
        Assert.True(ActiveMembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void RejectIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);
        RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("notPending", root.GetProperty("status").GetString());
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotRejectReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, strangerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    [Fact]
    public void RejectOnUnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RejectOnProvisionalGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(PendingMembershipExists(groupId, pendingUserAccountId));
    }

    // Tests - Response Shape

    [Fact]
    public void RejectResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement RejectMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static bool PendingMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static bool ActiveMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active);
    }
}
