using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListChatGroupsTest {
    // Tests - Authentication Failures

    [Fact]
    public void ListEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Empty And Membership Basics

    [Fact]
    public void ListWithNoGroupsReturnsEmptyArray() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");

        JsonElement root = List(testingMockProvidersContainer, authToken);

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public void OwnedActiveGroupAppearsAsOwnerAndJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, authToken), groupId);

        Assert.True(group.GetProperty("owner").GetBoolean());
        Assert.True(group.GetProperty("joined").GetBoolean());
        Assert.False(group.GetProperty("joinRequest").GetBoolean());
    }

    [Fact]
    public void JoinedActiveGroupAppearsAsJoinedNotOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Shared Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, memberAuthToken), groupId);

        Assert.False(group.GetProperty("owner").GetBoolean());
        Assert.True(group.GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void PublicGroupNotJoinedAppearsForDiscoveryNotJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, strangerAuthToken), groupId);

        Assert.False(group.GetProperty("owner").GetBoolean());
        Assert.False(group.GetProperty("joined").GetBoolean());
        Assert.True(group.GetProperty("isPublic").GetBoolean());
    }

    [Fact]
    public void PrivateGroupWithNoRelationshipAppearsForDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, strangerAuthToken), groupId);

        Assert.False(group.GetProperty("owner").GetBoolean());
        Assert.False(group.GetProperty("joined").GetBoolean());
        Assert.False(group.GetProperty("joinRequest").GetBoolean());
        Assert.False(group.GetProperty("isPublic").GetBoolean());
    }

    [Fact]
    public void PrivateGroupOwnedByMeAppears() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Private Group", false);

        JsonElement root = List(testingMockProvidersContainer, ownerAuthToken);

        Assert.True(ContainsGroup(root, groupId));
        Assert.False(GetGroup(root, groupId).GetProperty("isPublic").GetBoolean());
    }

    [Fact]
    public void PrivateGroupJoinedByMeAppears() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Shared", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = List(testingMockProvidersContainer, memberAuthToken);

        Assert.True(ContainsGroup(root, groupId));
        Assert.True(GetGroup(root, groupId).GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void PendingRequestOnPrivateGroupAppearsWithJoinRequestTrue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, requesterAuthToken), groupId);

        Assert.True(group.GetProperty("joinRequest").GetBoolean());
        Assert.False(group.GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void ProvisionalGroupIsExcluded() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = List(testingMockProvidersContainer, ownerAuthToken);

        Assert.False(ContainsGroup(root, groupId));
    }

    [Fact]
    public void PublicGroupJoinedByMeAppearsOnceWithJoinedTrue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Shared", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = List(testingMockProvidersContainer, memberAuthToken);

        Assert.Equal(1, CountGroup(root, groupId));
        Assert.True(GetGroup(root, groupId).GetProperty("joined").GetBoolean());
    }

    // Tests - Pending Members Flag

    [Fact]
    public void OwnerSeesPendingMembersTrueWhenPendingMemberExists() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, SeedUser("Requester", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.True(group.GetProperty("pendingMembers").GetBoolean());
    }

    [Fact]
    public void OwnerWithoutPendingMembersSeesPendingMembersFalse() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.False(group.GetProperty("pendingMembers").GetBoolean());
    }

    [Fact]
    public void NonOwnerMemberNeverSeesPendingMembersTrue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        AddPendingMember(groupId, SeedUser("Requester", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, memberAuthToken), groupId);

        Assert.False(group.GetProperty("pendingMembers").GetBoolean());
    }

    // Tests - Member Count And Helper Avatars

    [Fact]
    public void MemberCountCountsActiveMembersOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", false);
        AddActiveMember(groupId, SeedUser("Active One", null));
        AddActiveMember(groupId, SeedUser("Active Two", null));
        AddPendingMember(groupId, SeedUser("Pending One", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(3, group.GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public void HelpersReturnedForGroupIAmActiveMemberOf() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, SeedUser("Member One", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(2, group.GetProperty("helpers").GetArrayLength());
    }

    [Fact]
    public void HelpersReturnedForPublicGroupIAmNotMemberOf() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Public Group", true);
        AddActiveMember(groupId, SeedUser("Member One", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, strangerAuthToken), groupId);

        Assert.Equal(2, group.GetProperty("helpers").GetArrayLength());
        Assert.Equal(2, group.GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public void HelpersEmptyForPendingOnlyGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, requesterAuthToken), groupId);

        Assert.Equal(0, group.GetProperty("helpers").GetArrayLength());
    }

    [Fact]
    public void HelpersCappedAtFiveButMemberCountIsFull() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Busy Group", true);
        for (int index = 0; index < 9; index++)
            AddActiveMember(groupId, SeedUser("Member " + index, null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(5, group.GetProperty("helpers").GetArrayLength());
        Assert.Equal(10, group.GetProperty("memberCount").GetInt32());
    }

    // Tests - Response Shape

    [Fact]
    public void ListItemContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);
        List<string> actualProperties = [.. group.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["directContact", "helpers", "id", "isDirect", "isMuted", "isPublic", "joinRequest", "joined", "lastMessageAtUtc", "lastMessagePreview", "memberCount", "owner", "pendingMembers", "title", "unreadCount"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void HelperItemContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement helper = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId).GetProperty("helpers")[0];
        List<string> actualProperties = [.. helper.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["avatarColor", "initial", "profilePhotoUrl"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void TitleReflectsGroupName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Paddleboarding Support", true);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal("Paddleboarding Support", group.GetProperty("title").GetString());
    }

    // Tests - Isolation And Ordering

    [Fact]
    public void OtherUsersPrivateGroupAppearsInDirectory() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid otherOwnerUserAccountId = SeedUser("Other Owner", null);
        Guid privateGroupId = CreateActiveGroup(otherOwnerUserAccountId, "Other Private Group", false);
        AddActiveMember(privateGroupId, SeedUser("Other Member", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, requesterAuthToken), privateGroupId);

        Assert.False(group.GetProperty("joined").GetBoolean());
        Assert.Equal(2, group.GetProperty("memberCount").GetInt32());
        Assert.Equal(0, group.GetProperty("helpers").GetArrayLength());
    }

    [Fact]
    public void OwnedGroupAppearsBeforeOtherGroupEvenIfOlder() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid myOldGroupId = CreateActiveGroup(ResolveUserAccountId(meAuthToken), "My Old Group", true);
        Guid otherNewGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Other New Group", true);
        SetGroupCreatedAt(myOldGroupId, DateTime.UtcNow.AddDays(-10));
        SetGroupCreatedAt(otherNewGroupId, DateTime.UtcNow);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, myOldGroupId) < IndexOfGroup(root, otherNewGroupId));
    }

    [Fact]
    public void OwnedGroupsOrderedByMostRecentlyCreatedFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid olderGroupId = CreateActiveGroup(meUserAccountId, "Older Owned", true);
        Guid newerGroupId = CreateActiveGroup(meUserAccountId, "Newer Owned", true);
        SetGroupCreatedAt(olderGroupId, DateTime.UtcNow.AddMinutes(-30));
        SetGroupCreatedAt(newerGroupId, DateTime.UtcNow);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, newerGroupId) < IndexOfGroup(root, olderGroupId));
    }

    [Fact]
    public void JoinedGroupAppearsBeforePendingGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Joined Group", true);
        AddActiveMember(joinedGroupId, meUserAccountId);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Pending Group", false);
        AddPendingMember(pendingGroupId, meUserAccountId);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, joinedGroupId) < IndexOfGroup(root, pendingGroupId));
    }

    [Fact]
    public void PendingGroupAppearsBeforeUnrelatedGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Pending Group", false);
        AddPendingMember(pendingGroupId, meUserAccountId);
        Guid unrelatedGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Unrelated Group", true);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, pendingGroupId) < IndexOfGroup(root, unrelatedGroupId));
    }

    [Fact]
    public void MoreRecentlyJoinedGroupAppearsFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid joinedEarlierGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Joined Earlier", true);
        Guid joinedLaterGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Joined Later", true);
        AddActiveMemberAt(joinedEarlierGroupId, meUserAccountId, DateTime.UtcNow.AddMinutes(-30));
        AddActiveMemberAt(joinedLaterGroupId, meUserAccountId, DateTime.UtcNow);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, joinedLaterGroupId) < IndexOfGroup(root, joinedEarlierGroupId));
    }

    [Fact]
    public void MoreRecentlyRequestedPendingGroupAppearsFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid requestedYesterdayGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Requested Yesterday", false);
        Guid requestedTodayGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Requested Today", false);
        AddPendingMemberAt(requestedYesterdayGroupId, meUserAccountId, DateTime.UtcNow.AddDays(-1));
        AddPendingMemberAt(requestedTodayGroupId, meUserAccountId, DateTime.UtcNow);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, requestedTodayGroupId) < IndexOfGroup(root, requestedYesterdayGroupId));
    }

    [Fact]
    public void DefaultOrderIsCreatedThenJoinedThenPendingThenOthers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string meAuthToken = CreateUser(testingMockProvidersContainer, "Me");
        Guid meUserAccountId = ResolveUserAccountId(meAuthToken);
        Guid ownedGroupId = CreateActiveGroup(meUserAccountId, "Owned", true);
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Joined", true);
        AddActiveMember(joinedGroupId, meUserAccountId);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner C", null), "Pending", false);
        AddPendingMember(pendingGroupId, meUserAccountId);
        Guid otherGroupId = CreateActiveGroup(SeedUser("Owner D", null), "Other", true);

        JsonElement root = List(testingMockProvidersContainer, meAuthToken);

        Assert.True(IndexOfGroup(root, ownedGroupId) < IndexOfGroup(root, joinedGroupId));
        Assert.True(IndexOfGroup(root, joinedGroupId) < IndexOfGroup(root, pendingGroupId));
        Assert.True(IndexOfGroup(root, pendingGroupId) < IndexOfGroup(root, otherGroupId));
    }

    // Tests - Avatar Correctness And Cross Contamination

    [Fact]
    public void HelperAvatarReflectsMemberProfile() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        string ownerPhotoUrl = "/api/photo/" + Guid.NewGuid();
        Guid ownerUserAccountId = SeedUser("zoe", ownerPhotoUrl);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement helpers = GetGroup(List(testingMockProvidersContainer, memberAuthToken), groupId).GetProperty("helpers");
        JsonElement ownerAvatar = default;
        foreach (JsonElement helper in helpers.EnumerateArray())
            if (helper.GetProperty("profilePhotoUrl").GetString() == ownerPhotoUrl)
                ownerAvatar = helper;

        Assert.Equal("Z", ownerAvatar.GetProperty("initial").GetString());
        Assert.False(string.IsNullOrEmpty(ownerAvatar.GetProperty("avatarColor").GetString()));
    }

    [Fact]
    public void OwnerHelpersExcludePendingMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", false);
        AddActiveMember(groupId, SeedUser("Active Member", null));
        AddPendingMember(groupId, SeedUser("Pending Member", null));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(2, group.GetProperty("helpers").GetArrayLength());
        Assert.Equal(2, group.GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public void MixedFeedReportsIndependentFlagsPerGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Joined Group", true);
        AddActiveMember(joinedGroupId, requesterUserAccountId);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner C", null), "Pending Group", false);
        AddPendingMember(pendingGroupId, requesterUserAccountId);
        Guid discoveryGroupId = CreateActiveGroup(SeedUser("Owner D", null), "Discovery Group", true);

        JsonElement root = List(testingMockProvidersContainer, requesterAuthToken);
        JsonElement ownedGroup = GetGroup(root, ownedGroupId);
        JsonElement joinedGroup = GetGroup(root, joinedGroupId);
        JsonElement pendingGroup = GetGroup(root, pendingGroupId);
        JsonElement discoveryGroup = GetGroup(root, discoveryGroupId);

        Assert.True(ownedGroup.GetProperty("owner").GetBoolean());
        Assert.True(ownedGroup.GetProperty("joined").GetBoolean());
        Assert.False(joinedGroup.GetProperty("owner").GetBoolean());
        Assert.True(joinedGroup.GetProperty("joined").GetBoolean());
        Assert.False(pendingGroup.GetProperty("joined").GetBoolean());
        Assert.True(pendingGroup.GetProperty("joinRequest").GetBoolean());
        Assert.False(discoveryGroup.GetProperty("joined").GetBoolean());
        Assert.False(discoveryGroup.GetProperty("joinRequest").GetBoolean());
        Assert.True(discoveryGroup.GetProperty("isPublic").GetBoolean());
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement List(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static void SetGroupCreatedAt(Guid groupId, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == groupId);
        chatGroup.CreatedAtUtc = createdAtUtc;
        dbContext.SaveChanges();
    }

    private static void AddActiveMemberAt(Guid groupId, Guid userAccountId, DateTime joinedAtUtc) {
        AddMemberAt(groupId, userAccountId, ChatGroupMemberStatus.Active, joinedAtUtc);
    }

    private static void AddPendingMemberAt(Guid groupId, Guid userAccountId, DateTime joinedAtUtc) {
        AddMemberAt(groupId, userAccountId, ChatGroupMemberStatus.Pending, joinedAtUtc);
    }

    private static void AddMemberAt(Guid groupId, Guid userAccountId, ChatGroupMemberStatus status, DateTime joinedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = joinedAtUtc });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool ContainsGroup(JsonElement root, Guid groupId) {
        return CountGroup(root, groupId) > 0;
    }

    private static int CountGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        int count = 0;
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                count++;
        return count;
    }

    private static JsonElement GetGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Chat group was not present in the response.");
    }

    private static int IndexOfGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        int index = 0;
        foreach (JsonElement element in root.EnumerateArray()) {
            if (element.GetProperty("id").GetString() == target)
                return index;
            index++;
        }
        throw new InvalidOperationException("Chat group was not present in the response.");
    }
}
