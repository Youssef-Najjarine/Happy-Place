using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatGroupVisibilityTest {
    // Tests - Authentication Failures

    [Fact]
    public void SetVisibilityEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), IsPublic = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void SetVisibilityInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), IsPublic = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void SetVisibilityMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { ChatGroupId = Guid.NewGuid(), IsPublic = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Changes Visibility

    [Fact]
    public void OwnerMakesPublicGroupPrivate() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.Equal("updated", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("isPublic").GetBoolean());
        Assert.False(GetGroupIsPublic(groupId));
    }

    [Fact]
    public void OwnerMakesPrivateGroupPublic() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", false);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, true);

        Assert.Equal("updated", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("isPublic").GetBoolean());
        Assert.True(GetGroupIsPublic(groupId));
    }

    [Fact]
    public void SettingPublicWhenAlreadyPublicStaysPublic() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, true);

        Assert.Equal("updated", root.GetProperty("status").GetString());
        Assert.True(GetGroupIsPublic(groupId));
    }

    [Fact]
    public void SettingPrivateWhenAlreadyPrivateStaysPrivate() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", false);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.Equal("updated", root.GetProperty("status").GetString());
        Assert.False(GetGroupIsPublic(groupId));
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotChangeVisibilityReturnsNoneAndUnchanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = SetVisibility(testingMockProvidersContainer, strangerAuthToken, groupId, false);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GetGroupIsPublic(groupId));
    }

    [Fact]
    public void MemberWhoIsNotOwnerCannotChangeVisibility() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = SetVisibility(testingMockProvidersContainer, memberAuthToken, groupId, false);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GetGroupIsPublic(groupId));
    }

    [Fact]
    public void UnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid(), false);

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ProvisionalGroupVisibilityCannotBeChangedReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.True(GetGroupIsPublic(groupId));
    }

    // Tests - Effect On Discovery

    [Fact]
    public void MakingGroupPrivateRemovesItFromStrangerDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        Assert.True(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
    }

    [Fact]
    public void MakingGroupPublicAddsItToStrangerDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        Assert.False(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, true);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
    }

    // Tests - Response Shape

    [Fact]
    public void SetVisibilityResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["isPublic", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Visibility Does Not Over Hide Or Bleed

    [Fact]
    public void MakingGroupPrivateStillVisibleToOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, ownerAuthToken, groupId));
    }

    [Fact]
    public void MakingGroupPrivateStillVisibleToActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
    }

    [Fact]
    public void SetVisibilityDoesNotAffectNameOrMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Keep This Name", true);
        AddActiveMember(groupId, SeedUser("Member One", null));

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);

        Assert.Equal("Keep This Name", GetGroupName(groupId));
        Assert.Equal(2, CountMembers(groupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement SetVisibility(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, bool isPublic) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = authToken, ChatGroupId = chatGroupId, IsPublic = isPublic }).ReadContentAsJsonDocument().RootElement.Clone();
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

    // Helpers - Reading

    private static bool GetGroupIsPublic(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).IsPublic;
    }

    private static string GetGroupName(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).Name;
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }
}
