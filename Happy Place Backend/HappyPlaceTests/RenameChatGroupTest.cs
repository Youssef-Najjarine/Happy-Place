using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RenameChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void RenameEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), Name = "New Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RenameInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), Name = "New Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RenameMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { ChatGroupId = Guid.NewGuid(), Name = "New Name" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Owner Renames

    [Fact]
    public void OwnerRenamesActiveGroupUpdatesName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);

        Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Fresh Name");

        Assert.Equal("Fresh Name", GetGroupName(groupId));
    }

    [Fact]
    public void RenameReturnsRenamedStatusAndTitle() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Fresh Name");

        Assert.Equal("renamed", root.GetProperty("status").GetString());
        Assert.Equal("Fresh Name", root.GetProperty("title").GetString());
    }

    [Fact]
    public void RenamingToSameNameSucceeds() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Same Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Same Name");

        Assert.Equal("renamed", root.GetProperty("status").GetString());
        Assert.Equal("Same Name", GetGroupName(groupId));
    }

    [Fact]
    public void NameTrimmedBeforeSave() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "   Padded Name   ");

        Assert.Equal("Padded Name", root.GetProperty("title").GetString());
        Assert.Equal("Padded Name", GetGroupName(groupId));
    }

    [Fact]
    public void NameTruncatedToOneHundredCharacters() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);
        string overlongName = new('a', 150);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, overlongName);

        Assert.Equal(100, root.GetProperty("title").GetString().Length);
        Assert.Equal(100, GetGroupName(groupId).Length);
    }

    // Tests - Invalid Names

    [Fact]
    public void EmptyNameReturnsInvalidNameAndNameUnchanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Original Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "");

        Assert.Equal("invalidName", root.GetProperty("status").GetString());
        Assert.Equal("Original Name", GetGroupName(groupId));
    }

    [Fact]
    public void WhitespaceNameReturnsInvalidName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Original Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "     ");

        Assert.Equal("invalidName", root.GetProperty("status").GetString());
        Assert.Equal("Original Name", GetGroupName(groupId));
    }

    [Fact]
    public void MissingNameFieldReturnsInvalidName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Original Name", true);

        string status = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();

        Assert.Equal("invalidName", status);
    }

    // Tests - Authorization And Existence

    [Fact]
    public void NonOwnerStrangerCannotRenameReturnsNoneAndNameUnchanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Original Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, strangerAuthToken, groupId, "Hijacked Name");

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.Equal("Original Name", GetGroupName(groupId));
    }

    [Fact]
    public void MemberWhoIsNotOwnerCannotRename() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Original Name", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Rename(testingMockProvidersContainer, memberAuthToken, groupId, "Hijacked Name");

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.Equal("Original Name", GetGroupName(groupId));
    }

    [Fact]
    public void UnknownGroupReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, Guid.NewGuid(), "New Name");

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ProvisionalGroupCannotBeRenamedReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "New Name");

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.Equal("Waiting For Help", GetGroupName(groupId));
    }

    // Tests - Response Shape

    [Fact]
    public void RenameResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Fresh Name");
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status", "title"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Boundary And Preservation

    [Fact]
    public void NameAtExactlyOneHundredCharactersIsKept() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);
        string exactName = new('a', 100);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, exactName);

        Assert.Equal("renamed", root.GetProperty("status").GetString());
        Assert.Equal(100, GetGroupName(groupId).Length);
    }

    [Fact]
    public void UnicodeNameSavedCorrectly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", true);

        JsonElement root = Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Calm Space 🌸");

        Assert.Equal("Calm Space 🌸", root.GetProperty("title").GetString());
        Assert.Equal("Calm Space 🌸", GetGroupName(groupId));
    }

    [Fact]
    public void RenameDoesNotAffectVisibilityOrMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Old Name", false);
        AddActiveMember(groupId, SeedUser("Member One", null));

        Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "Fresh Name");

        Assert.False(GetGroupIsPublic(groupId));
        Assert.Equal(2, CountMembers(groupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Rename(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string name) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = authToken, ChatGroupId = chatGroupId, Name = name }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static string GetGroupName(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).Name;
    }

    private static bool GetGroupIsPublic(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).IsPublic;
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }
}
