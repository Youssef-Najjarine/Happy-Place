using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CancelJoinRequestTest {
    // Tests - Authentication Failures

    [Fact]
    public void CancelEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CancelInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CancelMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Cancelling A Pending Request

    [Fact]
    public void CancelRemovesPendingMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);

        CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.False(PendingMembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void CancelReturnsCancelledStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("cancelled", root.GetProperty("status").GetString());
    }

    [Fact]
    public void CancelIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));
        CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("notRequested", root.GetProperty("status").GetString());
    }

    // Tests - Nothing To Cancel

    [Fact]
    public void CancelWhenNotRequestedReturnsNotRequested() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal("notRequested", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ActiveMemberCancelReturnsNotRequestedAndKeepsMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("notRequested", root.GetProperty("status").GetString());
        Assert.True(ActiveMembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void CancelUnknownGroupReturnsNotRequested() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, Guid.NewGuid());

        Assert.Equal("notRequested", root.GetProperty("status").GetString());
    }

    // Tests - Effect On Feed

    [Fact]
    public void CancelledRequesterNoLongerSeesPrivateGroupInFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));
        Assert.True(ListContainsGroup(testingMockProvidersContainer, requesterAuthToken, groupId));

        CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, requesterAuthToken, groupId));
    }

    // Tests - Response Shape

    [Fact]
    public void CancelResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = CancelJoinRequest(testingMockProvidersContainer, requesterAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Isolation

    [Fact]
    public void CancellingOneRequestLeavesOthersPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string cancellingRequesterAuthToken = CreateUser(testingMockProvidersContainer, "Cancelling Requester");
        Guid otherRequesterUserAccountId = SeedUser("Other Requester", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(cancellingRequesterAuthToken));
        AddPendingMember(groupId, otherRequesterUserAccountId);

        CancelJoinRequest(testingMockProvidersContainer, cancellingRequesterAuthToken, groupId);

        Assert.False(PendingMembershipExists(groupId, ResolveUserAccountId(cancellingRequesterAuthToken)));
        Assert.True(PendingMembershipExists(groupId, otherRequesterUserAccountId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement CancelJoinRequest(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static bool ListContainsGroup(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement;
        string target = chatGroupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
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

    private static bool PendingMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending);
    }

    private static bool ActiveMembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active);
    }
}
