using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListMembersTest {
    // Tests - Authentication Failures

    [Fact]
    public void ListMembersEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListMembersInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListMembersMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Authorization And Visibility

    [Fact]
    public void OwnerSeesActiveMembersAndPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid activeMemberUserAccountId = SeedUser("Active Member", null);
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);
        AddActiveMember(groupId, activeMemberUserAccountId);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.True(ContainsUser(root.GetProperty("members"), ownerUserAccountId));
        Assert.True(ContainsUser(root.GetProperty("members"), activeMemberUserAccountId));
        Assert.True(ContainsUser(root.GetProperty("pendingMembers"), pendingUserAccountId));
    }

    [Fact]
    public void ActiveMemberSeesActiveMembersButNotPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);
        AddPendingMember(groupId, SeedUser("Pending", null));

        JsonElement root = ListMembers(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.True(ContainsUser(root.GetProperty("members"), memberUserAccountId));
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void StrangerGetsEmptyListsForPrivateGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);

        JsonElement root = ListMembers(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal(0, root.GetProperty("members").GetArrayLength());
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void StrangerSeesRosterButNoPendingForPublicGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Public Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = ListMembers(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.True(ContainsUser(root.GetProperty("members"), ownerUserAccountId));
        Assert.True(ContainsUser(root.GetProperty("members"), memberUserAccountId));
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void PendingRequesterGetsEmptyLists() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = ListMembers(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal(0, root.GetProperty("members").GetArrayLength());
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void ListMembersOnUnknownGroupReturnsEmptyLists() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(testingMockProvidersContainer, "Caller");

        JsonElement root = ListMembers(testingMockProvidersContainer, callerAuthToken, Guid.NewGuid());

        Assert.Equal(0, root.GetProperty("members").GetArrayLength());
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void ListMembersOnProvisionalGroupReturnsEmptyLists() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", false);

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, root.GetProperty("members").GetArrayLength());
        Assert.Equal(0, root.GetProperty("pendingMembers").GetArrayLength());
    }

    // Tests - Content Correctness

    [Fact]
    public void OwnerAppearsInMembersWithIsOwnerTrue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);

        JsonElement ownerEntry = GetEntry(ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members"), ownerUserAccountId);

        Assert.True(ownerEntry.GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public void NonOwnerMemberHasIsOwnerFalse() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUser("Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement memberEntry = GetEntry(ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members"), memberUserAccountId);

        Assert.False(memberEntry.GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public void MemberEntryReflectsUserProfile() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberPhotoUrl = "/api/photo/" + Guid.NewGuid();
        Guid memberUserAccountId = SeedUser("Zoe", memberPhotoUrl);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement memberEntry = GetEntry(ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members"), memberUserAccountId);

        Assert.Equal("Zoe", memberEntry.GetProperty("name").GetString());
        Assert.Equal(memberPhotoUrl, memberEntry.GetProperty("profilePhotoUrl").GetString());
        Assert.False(string.IsNullOrEmpty(memberEntry.GetProperty("avatarColor").GetString()));
    }

    [Fact]
    public void MemberEntryReflectsUsername() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid memberUserAccountId = SeedUserWithUsername("Zoe", "zoe_helps");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement memberEntry = GetEntry(ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members"), memberUserAccountId);

        Assert.Equal("zoe_helps", memberEntry.GetProperty("username").GetString());
    }

    [Fact]
    public void MembersListMatchesActiveMemberCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, SeedUser("Member One", null));
        AddActiveMember(groupId, SeedUser("Member Two", null));
        AddPendingMember(groupId, SeedUser("Pending", null));

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(3, root.GetProperty("members").GetArrayLength());
    }

    [Fact]
    public void PendingListMatchesPendingCountForOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, SeedUser("Pending One", null));
        AddPendingMember(groupId, SeedUser("Pending Two", null));

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(2, root.GetProperty("pendingMembers").GetArrayLength());
    }

    [Fact]
    public void ActiveMemberNotInPendingList() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid activeMemberUserAccountId = SeedUser("Active Member", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, activeMemberUserAccountId);

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ContainsUser(root.GetProperty("pendingMembers"), activeMemberUserAccountId));
    }

    [Fact]
    public void PendingRequesterNotInMembersList() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ContainsUser(root.GetProperty("members"), pendingUserAccountId));
    }

    // Tests - Ordering

    [Fact]
    public void MembersOrderedByJoinTimeOwnerFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMemberAt(groupId, SeedUser("Member One", null), DateTime.UtcNow.AddMinutes(1));
        AddActiveMemberAt(groupId, SeedUser("Member Two", null), DateTime.UtcNow.AddMinutes(2));

        JsonElement members = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members");

        Assert.True(members[0].GetProperty("isOwner").GetBoolean());
    }

    // Tests - Transitions With Approve And Reject

    [Fact]
    public void AfterApprovalMemberMovesFromPendingToActive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.True(ContainsUser(root.GetProperty("members"), pendingUserAccountId));
        Assert.False(ContainsUser(root.GetProperty("pendingMembers"), pendingUserAccountId));
    }

    [Fact]
    public void AfterRejectionRequesterDisappearsFromPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        RejectMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ContainsUser(root.GetProperty("pendingMembers"), pendingUserAccountId));
        Assert.False(ContainsUser(root.GetProperty("members"), pendingUserAccountId));
    }

    // Tests - Response Shape

    [Fact]
    public void MembersResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement root = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["members", "pendingMembers"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void MemberEntryContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement memberEntry = ListMembers(testingMockProvidersContainer, ownerAuthToken, groupId).GetProperty("members")[0];
        List<string> actualProperties = [.. memberEntry.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["avatarColor", "isOwner", "name", "profilePhotoUrl", "userAccountId", "username"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement ListMembers(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void ApproveMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void RejectMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
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

    private static Guid SeedUserWithUsername(string displayName, string username) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName, Username = username, IsAnonymous = false, CreatedAtUtc = DateTime.UtcNow });
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
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = now });
        dbContext.SaveChanges();
    }

    private static void AddActiveMemberAt(Guid groupId, Guid userAccountId, DateTime joinedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = joinedAtUtc });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool ContainsUser(JsonElement arrayElement, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("userAccountId").GetString() == target)
                return true;
        return false;
    }

    private static JsonElement GetEntry(JsonElement arrayElement, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("userAccountId").GetString() == target)
                return entry;
        throw new InvalidOperationException("Member was not present in the response.");
    }
}
