using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class LeaveChatGroupTest {
    // Tests - Authentication Failures

    [Fact]
    public void LeaveEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void LeaveInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void LeaveMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Members Leaving

    [Fact]
    public void ActiveMemberCanLeaveRemovesMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.False(MembershipExists(groupId, memberUserAccountId));
    }

    [Fact]
    public void LeaveReturnsLeftStatus() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("left", root.GetProperty("status").GetString());
    }

    [Fact]
    public void LeavingDoesNotAffectOtherMembersOrGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string leavingMemberAuthToken = CreateUser(testingMockProvidersContainer, "Leaving Member");
        Guid ownerUserAccountId = SeedUser("Owner", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid stayingMemberUserAccountId = SeedUser("Staying Member", null);
        AddActiveMember(groupId, ResolveUserAccountId(leavingMemberAuthToken));
        AddActiveMember(groupId, stayingMemberUserAccountId);

        Leave(testingMockProvidersContainer, leavingMemberAuthToken, groupId);

        Assert.True(GroupExists(groupId));
        Assert.True(MembershipExists(groupId, stayingMemberUserAccountId));
        Assert.True(MembershipExists(groupId, ownerUserAccountId));
        Assert.Equal(2, CountMembers(groupId));
    }

    [Fact]
    public void LeavingIsIdempotent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void MemberLeaveReleasesConnectedHelpOffer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Helper");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedConnectedOffer(groupId, memberUserAccountId);

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.Equal(HelpOfferStatus.Released, GetOfferStatus(groupId, memberUserAccountId));
    }

    [Fact]
    public void NonOwnerLeaveIgnoresDispositionAndReturnsLeft() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId, "delete");

        Assert.Equal("left", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
        Assert.False(MembershipExists(groupId, memberUserAccountId));
    }

    // Tests - Owner Leaving With Other Members (Transfer)

    [Fact]
    public void OwnerLeaveWithOtherMemberTransfersOwnershipAndReturnsTransferred() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid successorUserAccountId = SeedUser("Successor", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, successorUserAccountId);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("transferred", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
        Assert.False(MembershipExists(groupId, ownerUserAccountId));
        Assert.Equal(successorUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, successorUserAccountId));
        Assert.Equal(1, CountActiveMembers(groupId));
    }

    [Fact]
    public void OwnerLeaveTransfersToOldestActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid oldestUserAccountId = SeedUser("Oldest", null);
        Guid newestUserAccountId = SeedUser("Newest", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMemberJoinedAt(groupId, oldestUserAccountId, DateTime.UtcNow.AddMinutes(-10));
        AddActiveMemberJoinedAt(groupId, newestUserAccountId, DateTime.UtcNow.AddMinutes(-1));

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("transferred", root.GetProperty("status").GetString());
        Assert.Equal(oldestUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, oldestUserAccountId));
        Assert.False(IsActiveOwner(groupId, newestUserAccountId));
    }

    [Fact]
    public void OwnerLeaveWithMembersIgnoresDeleteDispositionAndTransfers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid successorUserAccountId = SeedUser("Successor", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, successorUserAccountId);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete");

        Assert.Equal("transferred", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
        Assert.Equal(successorUserAccountId, GetOwnerUserAccountId(groupId));
    }

    [Fact]
    public void OwnerLeaveReleasesOwnConnectedOfferWhenTransferring() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid successorUserAccountId = SeedUser("Successor", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, successorUserAccountId);
        SeedConnectedOffer(groupId, ownerUserAccountId);

        Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(HelpOfferStatus.Released, GetOfferStatus(groupId, ownerUserAccountId));
    }

    // Tests - Owner Leaving As Last Member (Disposition)

    [Fact]
    public void OwnerLeaveAsLastWithDeleteDeletesGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete");

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        Assert.False(GroupExists(groupId));
        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void OwnerLeaveAsLastWithMakePublicMakesGroupOwnerlessAndPublic() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "makePublic");

        Assert.Equal("madePublic", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
        Assert.True(IsPublic(groupId));
        Assert.Null(GetOwnerUserAccountId(groupId));
        Assert.False(MembershipExists(groupId, ownerUserAccountId));
        Assert.Equal(0, CountActiveMembers(groupId));
    }

    [Fact]
    public void OwnerLeaveMakePublicClearsPendingRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid pendingUserAccountId = SeedUser("Pending", null);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Private Group", false);
        AddPendingMember(groupId, pendingUserAccountId);

        Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "makePublic");

        Assert.False(MembershipExists(groupId, pendingUserAccountId));
        Assert.Equal(0, CountMembers(groupId));
    }

    [Fact]
    public void OwnerLeaveAsLastWithoutDispositionReturnsLastOwnerAndKeepsOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("lastOwner", root.GetProperty("status").GetString());
        Assert.True(GroupExists(groupId));
        Assert.True(IsActiveOwner(groupId, ownerUserAccountId));
        Assert.Equal(ownerUserAccountId, GetOwnerUserAccountId(groupId));
    }

    // Tests - Non Members

    [Fact]
    public void StrangerCannotLeaveReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Leave(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberCannotLeaveReturnsNotMemberAndKeepsRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, requesterUserAccountId);

        JsonElement root = Leave(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.True(MembershipExists(groupId, requesterUserAccountId));
    }

    [Fact]
    public void LeaveUnknownGroupReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid());

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    // Tests - Effect On Feed

    [Fact]
    public void LeftMemberStillSeesPrivateGroupInDirectoryNotJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Assert.True(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
        Assert.False(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void LeavingPublicGroupStillVisibleAsDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Public Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);

        Assert.True(ListContainsGroup(testingMockProvidersContainer, memberAuthToken, groupId));
        Assert.False(GetGroupFromList(testingMockProvidersContainer, memberAuthToken, groupId).GetProperty("joined").GetBoolean());
    }

    // Tests - Response Shape

    [Fact]
    public void LeaveResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement root = Leave(testingMockProvidersContainer, memberAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentOwnerLeaveDeleteAndJoinEndInConsistentState() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
            Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
            string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
            Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
            Guid groupId = CreateActiveGroup(ownerUserAccountId, "Public Group", true);

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [
                new Thread(() => { try { Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete"); } catch (Exception error) { errors.Add(error); } }),
                new Thread(() => { try { Join(testingMockProvidersContainer, joinerAuthToken, groupId); } catch (Exception error) { errors.Add(error); } })
            ];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.False(MembershipExists(groupId, ownerUserAccountId));
            if (GroupExists(groupId)) {
                Assert.Equal(1, CountActiveMembers(groupId));
                Assert.Equal(joinerUserAccountId, GetOwnerUserAccountId(groupId));
                Assert.True(IsActiveOwner(groupId, joinerUserAccountId));
            }
            else {
                Assert.Equal(0, CountMembers(groupId));
            }
        }
    }

    [Fact]
    public void ConcurrentOwnerLeaveMakePublicAndJoinEndInConsistentState() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
            Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
            string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
            Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
            Guid groupId = CreateActiveGroup(ownerUserAccountId, "Public Group", true);

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [
                new Thread(() => { try { Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "makePublic"); } catch (Exception error) { errors.Add(error); } }),
                new Thread(() => { try { Join(testingMockProvidersContainer, joinerAuthToken, groupId); } catch (Exception error) { errors.Add(error); } })
            ];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.True(GroupExists(groupId));
            Assert.True(IsPublic(groupId));
            Assert.False(MembershipExists(groupId, ownerUserAccountId));
            Assert.Equal(1, CountActiveMembers(groupId));
            Assert.Equal(joinerUserAccountId, GetOwnerUserAccountId(groupId));
            Assert.True(IsActiveOwner(groupId, joinerUserAccountId));
        }
    }

    [Fact]
    public void ConcurrentDuplicateOwnerLeaveDeleteIsSafe() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
            Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
            Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [
                new Thread(() => { try { Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete"); } catch (Exception error) { errors.Add(error); } }),
                new Thread(() => { try { Leave(testingMockProvidersContainer, ownerAuthToken, groupId, "delete"); } catch (Exception error) { errors.Add(error); } })
            ];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.False(GroupExists(groupId));
            Assert.Equal(0, CountMembers(groupId));
        }
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Leave(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string disposition = null) {
        if (disposition == null)
            return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId, Disposition = disposition }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Join(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
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
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
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

    private static void SeedConnectedOffer(Guid groupId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.HelpOffers.Add(new HelpOffer { Id = Guid.NewGuid(), ChatGroupId = groupId, HelperUserAccountId = helperUserAccountId, Status = HelpOfferStatus.Connected, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool MembershipExists(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId);
    }

    private static bool GroupExists(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == groupId);
    }

    private static bool IsPublic(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).IsPublic;
    }

    private static Guid? GetOwnerUserAccountId(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).OwnerUserAccountId;
    }

    private static bool IsActiveOwner(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active && field.MemberRole == ChatGroupMemberRole.Owner);
    }

    private static int CountMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId);
    }

    private static int CountActiveMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.Status == ChatGroupMemberStatus.Active);
    }

    private static HelpOfferStatus? GetOfferStatus(Guid groupId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        HelpOffer offer = dbContext.HelpOffers.SingleOrDefault(field => field.ChatGroupId == groupId && field.HelperUserAccountId == helperUserAccountId);
        if (offer == null)
            return null;
        return offer.Status;
    }
}
