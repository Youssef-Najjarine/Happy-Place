using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeleteChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void DeleteEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeleteInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeleteMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Deletes

    [Fact]
    public void OwnerDeletesActiveGroupRemovesIt() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        Assert.False(GroupExists(groupId));
    }

    [Fact]
    public void DeleteAlsoRemovesMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, SeedUser("Member One", null));
        AddActiveMember(groupId, SeedUser("Member Two", null));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void DeleteReturnsDeletedStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("deleted", root.GetProperty("status").GetString());
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotDeleteReturnsNoneAndGroupSurvives() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Delete(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
    }

    [Fact]
    public void MemberWhoIsNotOwnerCannotDelete() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Delete(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
    }

    [Fact]
    public void UnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid());

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ProvisionalGroupCannotBeDeletedViaThisEndpointReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
    }

    // Tests - Idempotency And Isolation

    [Fact]
    public void DeletingAlreadyDeletedGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void DeletingMyGroupDoesNotAffectOtherGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid deletedGroupId = CreateActiveGroup(ownerUserAccountId, "Doomed Group", true);
        Guid survivingGroupId = CreateActiveGroup(ownerUserAccountId, "Surviving Group", true);

        Delete(testingMockProvidersContainer, ownerAuthToken, deletedGroupId);

        Assert.False(GroupExists(deletedGroupId));
        Assert.True(GroupExists(survivingGroupId));
    }

    // Tests - Effect On Discovery

    [Fact]
    public void DeletedGroupNoLongerAppearsInStrangerDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        Assert.True(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
    }

    // Tests - Response Shape

    [Fact]
    public void DeleteResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Delete(testingMockProvidersContainer, ownerAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Cascade And Former Member Visibility

    [Fact]
    public void DeleteRemovesPendingMembersToo() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", false);
        AddActiveMember(groupId, SeedUser("Active Member", null));
        AddPendingMember(groupId, SeedUser("Pending Member", null));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void DeletedGroupNoLongerVisibleToFormerMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
    }

    // Tests - Real Active Group Teardown

    [Fact]
    public void DeleteActiveGroupWithConnectedOffersSucceedsAndCascades() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(testingMockProvidersContainer, "Seeker");
        string chatGroupIdText = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "I need help" }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        string helperAuthToken = CreateUser(testingMockProvidersContainer, "Helper");
        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupIdText }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupIdText }).EnsureSuccessStatusCode();
        Guid chatGroupId = Guid.Parse(chatGroupIdText);

        JsonElement root = Delete(testingMockProvidersContainer, seekerAuthToken, chatGroupId);

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        Assert.False(GroupExists(chatGroupId));
        Assert.Equal(0, CountMembers(chatGroupId));
        Assert.Equal(0, CountOffers(chatGroupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Delete(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static void AddPendingMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Pending, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool GroupExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }

    private static int CountOffers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == groupId);
    }
}
