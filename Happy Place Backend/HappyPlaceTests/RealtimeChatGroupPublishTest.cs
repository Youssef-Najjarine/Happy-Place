using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimeChatGroupPublishTest {
    // Tests - Creation

    [Fact]
    public void CreateWithFriendsPublishesMembershipToOwnerAndFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedAcceptedFriendship(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        List<string> friendUsernames = ["bob"];
        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/createWithFriends", new { AuthToken = aliceAuthToken, Name = "Weekend Crew", Usernames = friendUsernames });

        Guid chatGroupId = LoadGroupIdByName("Weekend Crew");
        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void OpenDirectFirstTimePublishesMembershipToBothUsers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedAcceptedFriendship(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/openDirect", new { AuthToken = aliceAuthToken, Username = "bob" });

        Guid chatGroupId = LoadDirectGroupId(aliceUserAccountId, bobUserAccountId);
        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    // Tests - Owner Controls

    [Fact]
    public void RenamePublishesMessagesKindToActiveMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/rename", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, Name = "Renamed Group" });

        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    [Fact]
    public void RenameByNonOwnerPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/rename", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, Name = "Hijacked Name" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void SetVisibilityPublishesMessagesKindToActiveMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/setVisibility", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, IsPublic = false });

        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    [Fact]
    public void DeletePublishesMembershipToEveryFormerMemberIncludingPending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        string pendingAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        Guid pendingUserAccountId = HelpParticipant.ResolveUserAccountId(pendingAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, pendingUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, memberUserAccountId, pendingUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    // Tests - Join Requests

    [Fact]
    public void RequestToJoinPublishesMembershipToOwnerAndRequester() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid requesterUserAccountId = HelpParticipant.ResolveUserAccountId(requesterAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = chatGroupId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, requesterUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void CancelJoinRequestPublishesMembershipToOwnerAndRequester() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid requesterUserAccountId = HelpParticipant.ResolveUserAccountId(requesterAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, requesterUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/cancelJoinRequest", new { AuthToken = requesterAuthToken, ChatGroupId = chatGroupId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, requesterUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void ApproveMemberPublishesMembershipToOwnerAndNewMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid requesterUserAccountId = HelpParticipant.ResolveUserAccountId(requesterAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, requesterUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/approveMember", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, MemberUserAccountId = requesterUserAccountId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, requesterUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void RejectMemberPublishesMembershipToOwnerAndRejectedUser() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string requesterAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid requesterUserAccountId = HelpParticipant.ResolveUserAccountId(requesterAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, requesterUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/rejectMember", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, MemberUserAccountId = requesterUserAccountId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, requesterUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    // Tests - Membership Changes

    [Fact]
    public void RemoveMemberPublishesMembershipToRemainingAndRemoved() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/removeMember", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void LeaveByMemberPublishesMembershipToRemainingAndLeaver() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void OwnerLeaveWithSuccessorPublishesMembershipToSuccessorAndLeaver() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/leave", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void OwnerLeaveDeleteDispositionPublishesToOwnerAndPendingRequesters() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string pendingAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid pendingUserAccountId = HelpParticipant.ResolveUserAccountId(pendingAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, pendingUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/leave", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, Disposition = "delete" });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, pendingUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    [Fact]
    public void OwnerLeaveMakePublicDispositionPublishesToOwnerAndPendingRequesters() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string pendingAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid pendingUserAccountId = HelpParticipant.ResolveUserAccountId(pendingAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedPendingMember(chatGroupId, pendingUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/leave", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, Disposition = "makePublic" });

        List<Guid> expectedUserAccountIds = [ownerUserAccountId, pendingUserAccountId];
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MembershipKind, expectedUserAccountIds);
    }

    // Tests - Preferences

    [Fact]
    public void SetMutedPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/setMuted", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, IsMuted = true });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void HideDirectGroupPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedAcceptedFriendship(aliceUserAccountId, bobUserAccountId);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/openDirect", new { AuthToken = aliceAuthToken, Username = "bob" });
        Guid chatGroupId = LoadDirectGroupId(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatGroup/hide", new { AuthToken = aliceAuthToken, ChatGroupId = chatGroupId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Account Deletion

    [Fact]
    public void AccountDeletionUntanglePublishesForEveryAffectedGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string danaAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Dana");
        string pendingOwnerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending Owner");
        string directPartnerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Direct Partner");
        string membershipOwnerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Membership Owner");
        string successorAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Successor");
        Guid danaUserAccountId = HelpParticipant.ResolveUserAccountId(danaAuthToken).Value;
        Guid pendingOwnerUserAccountId = HelpParticipant.ResolveUserAccountId(pendingOwnerAuthToken).Value;
        Guid directPartnerUserAccountId = HelpParticipant.ResolveUserAccountId(directPartnerAuthToken).Value;
        Guid membershipOwnerUserAccountId = HelpParticipant.ResolveUserAccountId(membershipOwnerAuthToken).Value;
        Guid successorUserAccountId = HelpParticipant.ResolveUserAccountId(successorAuthToken).Value;
        List<Guid> pendingGroupMemberIds = [pendingOwnerUserAccountId];
        Guid pendingGroupId = SeedGroup(pendingGroupMemberIds, pendingOwnerUserAccountId, false);
        SeedPendingMember(pendingGroupId, danaUserAccountId);
        Guid directGroupId = SeedDirectGroup(danaUserAccountId, directPartnerUserAccountId);
        List<Guid> membershipGroupMemberIds = [membershipOwnerUserAccountId, danaUserAccountId];
        Guid membershipGroupId = SeedGroup(membershipGroupMemberIds, membershipOwnerUserAccountId, false);
        List<Guid> ownedGroupMemberIds = [danaUserAccountId, successorUserAccountId];
        Guid ownedGroupId = SeedGroup(ownedGroupMemberIds, danaUserAccountId, false);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        ChatGroupManager.UntangleUserForAccountDeletion(danaUserAccountId);

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(4, sentEvents.Count);
        AssertSingleMembershipEventForGroup(sentEvents, pendingGroupId, pendingOwnerUserAccountId);
        AssertSingleMembershipEventForGroup(sentEvents, directGroupId, directPartnerUserAccountId);
        AssertSingleMembershipEventForGroup(sentEvents, membershipGroupId, membershipOwnerUserAccountId);
        AssertSingleMembershipEventForGroup(sentEvents, ownedGroupId, successorUserAccountId);
    }

    // Helpers

    private static HttpResponseMessage PostJsonOrFail(TestingMockProvidersContainer testingMockProvidersContainer, string url, object jsonData) {
        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson(url, jsonData);
        Assert.True(response.IsSuccessStatusCode);
        return response;
    }

    private static string CreateUserWithUsername(TestingMockProvidersContainer testingMockProvidersContainer, string displayName, string username) {
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, displayName);
        Guid userAccountId = HelpParticipant.ResolveUserAccountId(authToken).Value;
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts
            .Where(field => field.Id == userAccountId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Username, username));
        return authToken;
    }

    private static void SeedAcceptedFriendship(Guid requesterUserAccountId, Guid addresseeUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.Friendships.Add(new Friendship { Id = Guid.NewGuid(), RequesterUserAccountId = requesterUserAccountId, AddresseeUserAccountId = addresseeUserAccountId, Status = FriendshipStatus.Accepted, CreatedAtUtc = now, RespondedAtUtc = now });
        dbContext.SaveChanges();
    }

    private static Guid SeedGroup(List<Guid> memberUserAccountIds, Guid ownerUserAccountId, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Realtime Group Publish Group", OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = memberUserAccountId, MemberRole = memberUserAccountId == ownerUserAccountId ? ChatGroupMemberRole.Owner : ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }

    private static void SeedPendingMember(Guid chatGroupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Pending, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static Guid SeedDirectGroup(Guid firstUserAccountId, Guid secondUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(firstUserAccountId, secondUserAccountId);
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "", OwnerUserAccountId = null, IsPublic = false, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now, DirectPairLowId = pairLowId, DirectPairHighId = pairHighId });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = firstUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = secondUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }

    private static Guid LoadGroupIdByName(string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Name == name).Id;
    }

    private static Guid LoadDirectGroupId(Guid firstUserAccountId, Guid secondUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        (Guid pairLowId, Guid pairHighId) = ChatGroupManager.ComputeDirectPair(firstUserAccountId, secondUserAccountId);
        return dbContext.ChatGroups.Single(field => field.DirectPairLowId == pairLowId && field.DirectPairHighId == pairHighId).Id;
    }

    private static int CountEvents(TestingMockProvidersContainer testingMockProvidersContainer) {
        return testingMockProvidersContainer.RealtimeProvider.SentEvents.Count();
    }

    private static List<RealtimeSentEvent> EventsAfter(TestingMockProvidersContainer testingMockProvidersContainer, int baselineCount) {
        return [.. testingMockProvidersContainer.RealtimeProvider.SentEvents.Skip(baselineCount)];
    }

    private static void AssertChatGroupChangedForUsers(List<RealtimeSentEvent> sentEvents, Guid chatGroupId, string kind, List<Guid> expectedUserAccountIds) {
        Assert.Equal(expectedUserAccountIds.Count, sentEvents.Count);
        foreach (Guid expectedUserAccountId in expectedUserAccountIds)
            Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(expectedUserAccountId));
        foreach (RealtimeSentEvent sentEvent in sentEvents) {
            Assert.Equal(RealtimePublisher.ChatGroupChangedEventName, sentEvent.EventName);
            Assert.Equal(chatGroupId.ToString(), sentEvent.Payload["chatGroupId"]);
            Assert.Equal(kind, sentEvent.Payload["kind"]);
        }
    }

    private static void AssertSingleMembershipEventForGroup(List<RealtimeSentEvent> sentEvents, Guid chatGroupId, Guid expectedUserAccountId) {
        List<RealtimeSentEvent> groupEvents = [.. sentEvents.Where(field => field.Payload["chatGroupId"] == chatGroupId.ToString())];
        RealtimeSentEvent groupEvent = Assert.Single(groupEvents);
        Assert.Equal(RealtimePublisher.BuildUserGroupName(expectedUserAccountId), groupEvent.GroupName);
        Assert.Equal(RealtimePublisher.ChatGroupChangedEventName, groupEvent.EventName);
        Assert.Equal(RealtimePublisher.MembershipKind, groupEvent.Payload["kind"]);
    }
}
