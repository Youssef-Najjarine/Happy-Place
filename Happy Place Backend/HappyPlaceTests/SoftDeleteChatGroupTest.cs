using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SoftDeleteChatGroupTest {
    // Tests - Soft Delete Semantics

    [Fact]
    public void DeleteSetsStatusDeletedAndKeepsGroupRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(ChatGroupStatus.Deleted, GetGroupStatus(groupId));
    }

    [Fact]
    public void OwnerLeaveDeleteDispositionSoftDeletesGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete");

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        Assert.Equal(ChatGroupStatus.Deleted, GetGroupStatus(groupId));
    }

    // Tests - Teardown Invariants

    [Fact]
    public void DeleteClearsActiveAndPendingMembershipRows() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddActiveMember(groupId, SeedUser("Active Member", null));
        AddPendingMember(groupId, SeedUser("Pending Member", null));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void DeleteRemovesHelpOfferRows() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        SeedOffer(groupId, SeedUser("Helper", null), HelpOfferStatus.Connected, DateTime.UtcNow);

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, CountOffers(groupId));
    }

    [Fact]
    public void DeleteTearsDownJoinRequestsChannel() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);
        SeedJoinRequestsChannel(groupId, ownerUserAccountId);

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(JoinRequestsChannelExists(groupId));
    }

    // Tests - Deleted Groups Are Gone From The Product

    [Fact]
    public void DeletedGroupAbsentFromListAndListPage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        Assert.True(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
        Assert.True(ListPageContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));

        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.False(ListContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
        Assert.False(ListPageContainsGroup(testingMockProvidersContainer, strangerAuthToken, groupId));
    }

    [Fact]
    public void DeletedGroupListMembersReturnsEmptyCollections() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, SeedUser("Member", null));
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listMembers", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        foreach (JsonProperty property in root.EnumerateObject())
            if (property.Value.ValueKind == JsonValueKind.Array)
                Assert.Equal(0, property.Value.GetArrayLength());
    }

    [Fact]
    public void DeletedGroupRenameReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, Name = "New Name" }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void DeletedGroupSetVisibilityReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, IsPublic = false }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("none", root.GetProperty("status").GetString());
    }

    [Fact]
    public void DeletedGroupRequestToJoinReturnsNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("none", root.GetProperty("status").GetString());
        Assert.False(MembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void DeletedGroupJoinReturnsUnavailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        Delete(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = joinerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("unavailable", root.GetProperty("status").GetString());
        Assert.False(MembershipExists(groupId, joinerUserAccountId));
    }

    // Tests - Provisional Groups Keep Hard Delete

    [Fact]
    public void ProvisionalExpirySweepStillHardDeletesGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(testingMockProvidersContainer, "Caller");
        Guid seekerUserAccountId = SeedUser("Seeker", null);
        Guid groupId = CreateProvisionalGroup(seekerUserAccountId, "Waiting For Help", true);
        SeedOffer(groupId, SeedUser("Helper", null), HelpOfferStatus.Offered, DateTime.UtcNow);
        SetGroupLastSeen(groupId, DateTime.UtcNow.AddDays(-8));

        testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = callerAuthToken }).EnsureSuccessStatusCode();

        Assert.False(GroupRowExists(groupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Delete(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Leave(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string disposition) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId, Disposition = disposition }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static bool ListContainsGroup(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement;
        string target = chatGroupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
    }

    private static bool ListPageContainsGroup(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        string target = chatGroupId.ToString();
        string cursor = null;
        for (int page = 0; page < 50; page++) {
            JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listPage", new { AuthToken = authToken, Cursor = cursor }).ReadContentAsJsonDocument().RootElement.Clone();
            foreach (JsonElement element in root.GetProperty("items").EnumerateArray())
                if (element.GetProperty("id").GetString() == target)
                    return true;
            JsonElement nextCursorElement = root.GetProperty("nextCursor");
            if (nextCursorElement.ValueKind == JsonValueKind.Null)
                return false;
            cursor = nextCursorElement.GetString();
        }
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

    private static void SeedOffer(Guid groupId, Guid helperUserAccountId, HelpOfferStatus status, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.HelpOffers.Add(new HelpOffer { Id = Guid.NewGuid(), ChatGroupId = groupId, HelperUserAccountId = helperUserAccountId, Status = status, CreatedAtUtc = createdAtUtc, LastSeenAtUtc = createdAtUtc });
        dbContext.SaveChanges();
    }

    private static void SetGroupLastSeen(Guid groupId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == groupId);
        chatGroup.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }

    private static void SeedJoinRequestsChannel(Guid groupId, Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.NotificationChannels.Add(new NotificationChannel { Id = Guid.NewGuid(), RecipientUserAccountId = ownerUserAccountId, Kind = NotificationChannelKind.JoinRequests, ScopeChatGroupId = groupId, LastSentCount = 0, IsLive = false });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static ChatGroupStatus? GetGroupStatus(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == groupId);
        if (chatGroup == null)
            return null;
        return chatGroup.Status;
    }

    private static bool GroupRowExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static bool MembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }

    private static int CountOffers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == groupId);
    }

    private static bool JoinRequestsChannelExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == groupId);
    }
}
