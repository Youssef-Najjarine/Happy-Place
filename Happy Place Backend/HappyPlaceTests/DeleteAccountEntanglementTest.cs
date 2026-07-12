using System.Net;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeleteAccountEntanglementTest {
    // Tests - Memberships

    [Fact]
    public void MemberCanDeleteAccountAndMembershipRowIsRemoved() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid ownerUserAccountId = SeedUser("Owner", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        HttpResponseMessage response = DeleteAccount(testingMockProvidersContainer, memberAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(UserExists(memberUserAccountId));
        Assert.False(MembershipExists(groupId, memberUserAccountId));
        Assert.Equal(ChatGroupStatus.Active, GetGroupStatus(groupId));
        Assert.True(MembershipExists(groupId, ownerUserAccountId));
    }

    [Fact]
    public void MembershipsAcrossMultipleGroupsAllRemoved() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid firstGroupId = CreateActiveGroup(SeedUser("First Owner", null), "First Group", true);
        Guid secondGroupId = CreateActiveGroup(SeedUser("Second Owner", null), "Second Group", true);
        AddActiveMember(firstGroupId, memberUserAccountId);
        AddActiveMember(secondGroupId, memberUserAccountId);

        DeleteAccount(testingMockProvidersContainer, memberAuthToken);

        Assert.False(MembershipExists(firstGroupId, memberUserAccountId));
        Assert.False(MembershipExists(secondGroupId, memberUserAccountId));
        Assert.Equal(ChatGroupStatus.Active, GetGroupStatus(firstGroupId));
        Assert.Equal(ChatGroupStatus.Active, GetGroupStatus(secondGroupId));
    }

    [Fact]
    public void PendingJoinRequestRemovedOnDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);

        DeleteAccount(testingMockProvidersContainer, requesterAuthToken);

        Assert.False(MembershipExists(groupId, requesterUserAccountId));
        Assert.Equal(ChatGroupStatus.Active, GetGroupStatus(groupId));
    }

    // Tests - Ownership

    [Fact]
    public void OwnerWithMembersTransfersToOldestActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid oldestUserAccountId = SeedUser("Oldest", null);
        Guid newestUserAccountId = SeedUser("Newest", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMemberJoinedAt(groupId, oldestUserAccountId, DateTime.UtcNow.AddMinutes(-10));
        AddActiveMemberJoinedAt(groupId, newestUserAccountId, DateTime.UtcNow.AddMinutes(-1));

        DeleteAccount(testingMockProvidersContainer, ownerAuthToken);

        Assert.Equal(ChatGroupStatus.Active, GetGroupStatus(groupId));
        Assert.Equal(oldestUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, oldestUserAccountId));
        Assert.False(MembershipExists(groupId, ownerUserAccountId));
    }

    [Fact]
    public void LastOwnerActiveGroupIsSoftDeletedOnAccountDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        DeleteAccount(testingMockProvidersContainer, ownerAuthToken);

        Assert.Equal(ChatGroupStatus.Deleted, GetGroupStatus(groupId));
        Assert.Equal(0, CountMembers(groupId));
        Assert.Null(GetOwnerUserAccountId(groupId));
    }

    [Fact]
    public void LastOwnerSoftDeletedGroupClearsPendingRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        DeleteAccount(testingMockProvidersContainer, ownerAuthToken);

        Assert.Equal(ChatGroupStatus.Deleted, GetGroupStatus(groupId));
        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void OwnedProvisionalGroupIsHardDeletedOnAccountDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(testingMockProvidersContainer, "Seeker");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(seekerAuthToken), "Waiting For Help", true);

        DeleteAccount(testingMockProvidersContainer, seekerAuthToken);

        Assert.False(GroupRowExists(groupId));
        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void PreviouslyDeletedGroupOwnerReferenceIsCleared() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        HttpResponseMessage response = DeleteAccount(testingMockProvidersContainer, ownerAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(UserExists(ownerUserAccountId));
        Assert.Equal(ChatGroupStatus.Deleted, GetGroupStatus(groupId));
        Assert.Null(GetOwnerUserAccountId(groupId));
    }

    // Tests - Help Offers

    [Fact]
    public void HelpOfferRowsDeletedForEveryStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(testingMockProvidersContainer, "Helper");
        Guid helperUserAccountId = ResolveUserAccountId(helperAuthToken);
        Guid offeredGroupId = CreateProvisionalGroup(SeedUser("First Seeker", null), "First Request", true);
        Guid connectedGroupId = CreateActiveGroup(SeedUser("Second Seeker", null), "Second Group", true);
        Guid releasedGroupId = CreateActiveGroup(SeedUser("Third Seeker", null), "Third Group", true);
        Guid declinedGroupId = CreateProvisionalGroup(SeedUser("Fourth Seeker", null), "Fourth Request", true);
        SeedOffer(offeredGroupId, helperUserAccountId, HelpOfferStatus.Offered);
        SeedOffer(connectedGroupId, helperUserAccountId, HelpOfferStatus.Connected);
        SeedOffer(releasedGroupId, helperUserAccountId, HelpOfferStatus.Released);
        SeedOffer(declinedGroupId, helperUserAccountId, HelpOfferStatus.Declined);

        DeleteAccount(testingMockProvidersContainer, helperAuthToken);

        Assert.Equal(0, CountOffersByHelper(helperUserAccountId));
    }

    [Fact]
    public void ReactionRowsRemovedOnDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string reactorAuthToken = CreateUser(testingMockProvidersContainer, "Reactor");
        Guid reactorUserAccountId = ResolveUserAccountId(reactorAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, reactorUserAccountId);
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), Body = "react to me" }).EnsureSuccessStatusCode();
        Guid messageId = LoadSingleMessage(groupId).Id;
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = reactorAuthToken, ChatGroupId = groupId, MessageId = messageId, Kind = 1 }).EnsureSuccessStatusCode();

        DeleteAccount(testingMockProvidersContainer, reactorAuthToken);

        Assert.Equal(0, CountReactionsByUser(reactorUserAccountId));
        Assert.Equal(1, CountMessages(groupId));
    }

    [Fact]
    public void ReportSnapshotSurvivesReporterAccountDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string reporterAuthToken = CreateUser(testingMockProvidersContainer, "Reporter");
        Guid reporterUserAccountId = ResolveUserAccountId(reporterAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, reporterUserAccountId);
        string body = "reported content " + Guid.NewGuid();
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), Body = body }).EnsureSuccessStatusCode();
        Guid messageId = LoadSingleMessage(groupId).Id;
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { AuthToken = reporterAuthToken, ChatGroupId = groupId, MessageId = messageId, Reason = "harmful" }).EnsureSuccessStatusCode();

        DeleteAccount(testingMockProvidersContainer, reporterAuthToken);

        ChatMessageReport report = LoadSingleReport(messageId);
        Assert.Equal(reporterUserAccountId, report.ReporterUserAccountId);
        Assert.Equal(body, MessageCipher.Decrypt(report.BodySnapshotCipher));
    }

    // Tests - Cascading Rows

    [Fact]
    public void DeviceTokensRemovedOnDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string userAuthToken = CreateUser(testingMockProvidersContainer, "Device Owner");
        Guid userAccountId = ResolveUserAccountId(userAuthToken);
        SeedDeviceToken(userAccountId);

        DeleteAccount(testingMockProvidersContainer, userAuthToken);

        Assert.Equal(0, CountDeviceTokens(userAccountId));
    }

    [Fact]
    public void HelpAvailabilityRemovedOnDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string userAuthToken = CreateUser(testingMockProvidersContainer, "Available Helper");
        Guid userAccountId = ResolveUserAccountId(userAuthToken);
        SeedAvailability(userAccountId);

        DeleteAccount(testingMockProvidersContainer, userAuthToken);

        Assert.Equal(0, CountAvailabilities(userAccountId));
    }

    // Tests - Messages Survive

    [Fact]
    public void MessagesSurviveSenderAccountDeletion() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        string body = "a message that must outlive its author " + Guid.NewGuid();
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = memberAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), Body = body }).EnsureSuccessStatusCode();

        DeleteAccount(testingMockProvidersContainer, memberAuthToken);

        ChatMessage message = LoadSingleMessage(groupId);
        Assert.Null(message.SenderUserAccountId);
        Assert.False(message.IsDeleted);
        Assert.Equal(body, MessageCipher.Decrypt(message.BodyCipher));
    }

    // Tests - Kitchen Sink

    [Fact]
    public void EntangledUserDeletionReturnsOkAndUserRowGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string userAuthToken = CreateUser(testingMockProvidersContainer, "Entangled User");
        Guid userAccountId = ResolveUserAccountId(userAuthToken);
        Guid successorUserAccountId = SeedUser("Successor", null);
        Guid ownedGroupId = CreateActiveGroup(userAccountId, "Owned Group", true);
        AddActiveMember(ownedGroupId, successorUserAccountId);
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Joined Group", true);
        AddActiveMember(joinedGroupId, userAccountId);
        Guid provisionalGroupId = CreateProvisionalGroup(SeedUser("Seeker", null), "Open Request", true);
        SeedOffer(provisionalGroupId, userAccountId, HelpOfferStatus.Offered);
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = userAuthToken, ChatGroupId = joinedGroupId, ClientMessageId = Guid.NewGuid(), Body = "hello" }).EnsureSuccessStatusCode();

        HttpResponseMessage response = DeleteAccount(testingMockProvidersContainer, userAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(UserExists(userAccountId));
        Assert.Equal(successorUserAccountId, GetOwnerUserAccountId(ownedGroupId));
        Assert.False(MembershipExists(joinedGroupId, userAccountId));
        Assert.Equal(0, CountOffersByHelper(userAccountId));
        Assert.Equal(1, CountMessages(joinedGroupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static HttpResponseMessage DeleteAccount(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/userProfile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" });
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
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Active, DateTime.UtcNow);
    }

    private static void AddActiveMemberJoinedAt(Guid groupId, Guid userAccountId, DateTime joinedAtUtc) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Active, joinedAtUtc);
    }

    private static void AddPendingMember(Guid groupId, Guid userAccountId) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Pending, DateTime.UtcNow);
    }

    private static void AddMember(Guid groupId, Guid userAccountId, ChatGroupMemberStatus status, DateTime joinedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = joinedAtUtc });
        dbContext.SaveChanges();
    }

    private static void SeedOffer(Guid groupId, Guid helperUserAccountId, HelpOfferStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.HelpOffers.Add(new HelpOffer { Id = Guid.NewGuid(), ChatGroupId = groupId, HelperUserAccountId = helperUserAccountId, Status = status, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    private static void SeedDeviceToken(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.DeviceTokens.Add(new DeviceToken { Id = Guid.NewGuid(), UserAccountId = userAccountId, Token = "token-" + Guid.NewGuid(), Platform = "ios", CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    private static void SeedAvailability(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.HelpAvailabilities.Add(new HelpAvailability { Id = Guid.NewGuid(), HelperUserAccountId = userAccountId, IsAvailable = true, LastSeenAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool UserExists(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Any(field => field.Id == userAccountId);
    }

    private static bool GroupRowExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static ChatGroupStatus? GetGroupStatus(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == groupId);
        if (chatGroup == null)
            return null;
        return chatGroup.Status;
    }

    private static Guid? GetOwnerUserAccountId(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).OwnerUserAccountId;
    }

    private static bool IsActiveOwner(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active && field.MemberRole == ChatGroupMemberRole.Owner);
    }

    private static bool MembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }

    private static int CountOffersByHelper(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.HelperUserAccountId == helperUserAccountId);
    }

    private static int CountDeviceTokens(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.DeviceTokens.Count(field => field.UserAccountId == userAccountId);
    }

    private static int CountAvailabilities(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpAvailabilities.Count(field => field.HelperUserAccountId == userAccountId);
    }

    private static int CountMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == groupId);
    }

    private static ChatMessageReport LoadSingleReport(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReports.Single(field => field.ChatMessageId == messageId);
    }

    private static int CountReactionsByUser(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReactions.Count(field => field.UserAccountId == userAccountId);
    }

    private static ChatMessage LoadSingleMessage(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Single(field => field.ChatGroupId == groupId);
    }
}
