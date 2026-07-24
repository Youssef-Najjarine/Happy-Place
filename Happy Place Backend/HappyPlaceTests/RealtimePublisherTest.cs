using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimePublisherTest {
    // Tests - Chat Group Publishing

    [Fact]
    public void PublishChatGroupChangedSendsToEveryActiveMemberUserGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid ownerUserAccountId = CreateUser(testingMockProvidersContainer, "Owner");
        Guid firstMemberUserAccountId = CreateUser(testingMockProvidersContainer, "First Member");
        Guid secondMemberUserAccountId = CreateUser(testingMockProvidersContainer, "Second Member");
        List<Guid> memberUserAccountIds = [ownerUserAccountId, firstMemberUserAccountId, secondMemberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Equal(3, sentEvents.Count);
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(memberUserAccountId));
        foreach (RealtimeSentEvent sentEvent in sentEvents) {
            Assert.Equal(RealtimePublisher.ChatGroupChangedEventName, sentEvent.EventName);
            Assert.Equal(chatGroupId.ToString(), sentEvent.Payload["chatGroupId"]);
            Assert.Equal(RealtimePublisher.MessagesKind, sentEvent.Payload["kind"]);
        }
    }

    [Fact]
    public void PublishChatGroupChangedExcludesPendingMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid ownerUserAccountId = CreateUser(testingMockProvidersContainer, "Owner");
        Guid activeMemberUserAccountId = CreateUser(testingMockProvidersContainer, "Active Member");
        Guid pendingMemberUserAccountId = CreateUser(testingMockProvidersContainer, "Pending Member");
        List<Guid> memberUserAccountIds = [ownerUserAccountId, activeMemberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        SeedPendingMember(chatGroupId, pendingMemberUserAccountId);

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Equal(2, sentEvents.Count);
        Assert.DoesNotContain(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(pendingMemberUserAccountId));
    }

    [Fact]
    public void PublishChatGroupChangedIncludesExtraRecipientsWithoutDuplicates() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid ownerUserAccountId = CreateUser(testingMockProvidersContainer, "Owner");
        Guid activeMemberUserAccountId = CreateUser(testingMockProvidersContainer, "Active Member");
        Guid removedUserAccountId = CreateUser(testingMockProvidersContainer, "Removed User");
        List<Guid> memberUserAccountIds = [ownerUserAccountId, activeMemberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        List<Guid> extraRecipientUserAccountIds = [activeMemberUserAccountId, removedUserAccountId];

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Equal(3, sentEvents.Count);
        Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(removedUserAccountId));
        List<RealtimeSentEvent> activeMemberEvents = [.. sentEvents.Where(field => field.GroupName == RealtimePublisher.BuildUserGroupName(activeMemberUserAccountId))];
        Assert.Single(activeMemberEvents);
    }

    [Fact]
    public void PublishChatGroupChangedForUnknownGroupSendsNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        RealtimePublisher.PublishChatGroupChanged(Guid.NewGuid(), RealtimePublisher.MessagesKind);

        Assert.Empty(testingMockProvidersContainer.RealtimeProvider.SentEvents);
    }

    [Fact]
    public void PublishChatGroupChangedSurvivesSenderFailureAndRecovers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid ownerUserAccountId = CreateUser(testingMockProvidersContainer, "Owner");
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        testingMockProvidersContainer.RealtimeProvider.FailNextSend();

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        Assert.Empty(testingMockProvidersContainer.RealtimeProvider.SentEvents);

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Single(sentEvents);
    }

    // Tests - Friends Publishing

    [Fact]
    public void PublishFriendsChangedSendsToUserGroupWithEmptyPayload() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid recipientUserAccountId = CreateUser(testingMockProvidersContainer, "Recipient");

        RealtimePublisher.PublishFriendsChanged(recipientUserAccountId);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Single(sentEvents);
        Assert.Equal(RealtimePublisher.BuildUserGroupName(recipientUserAccountId), sentEvents[0].GroupName);
        Assert.Equal(RealtimePublisher.FriendsChangedEventName, sentEvents[0].EventName);
        Assert.Empty(sentEvents[0].Payload);
    }

    // Tests - Help Publishing

    [Fact]
    public void PublishHelpChangedSendsToUserGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid recipientUserAccountId = CreateUser(testingMockProvidersContainer, "Recipient");

        RealtimePublisher.PublishHelpChanged(recipientUserAccountId);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Single(sentEvents);
        Assert.Equal(RealtimePublisher.BuildUserGroupName(recipientUserAccountId), sentEvents[0].GroupName);
        Assert.Equal(RealtimePublisher.HelpChangedEventName, sentEvents[0].EventName);
    }

    [Fact]
    public void PublishHelpChangedListSendsOncePerDistinctUser() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        Guid firstRecipientUserAccountId = CreateUser(testingMockProvidersContainer, "First Recipient");
        Guid secondRecipientUserAccountId = CreateUser(testingMockProvidersContainer, "Second Recipient");
        List<Guid> recipientUserAccountIds = [firstRecipientUserAccountId, secondRecipientUserAccountId, firstRecipientUserAccountId];

        RealtimePublisher.PublishHelpChanged(recipientUserAccountIds);

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Equal(2, sentEvents.Count);
        Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(firstRecipientUserAccountId));
        Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(secondRecipientUserAccountId));
    }

    [Fact]
    public void PublishHelpOpenRequestsChangedSendsToHelpersListeningGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        RealtimePublisher.PublishHelpOpenRequestsChanged();

        List<RealtimeSentEvent> sentEvents = [.. testingMockProvidersContainer.RealtimeProvider.SentEvents];
        Assert.Single(sentEvents);
        Assert.Equal(RealtimePublisher.HelpersListeningGroupName, sentEvents[0].GroupName);
        Assert.Equal(RealtimePublisher.HelpChangedEventName, sentEvents[0].EventName);
    }

    // Helpers

    private static Guid CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string displayName) {
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, displayName);
        return HelpParticipant.ResolveUserAccountId(authToken).Value;
    }

    private static Guid SeedActiveGroup(List<Guid> memberUserAccountIds, Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Realtime Publisher Group", OwnerUserAccountId = ownerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = memberUserAccountId, MemberRole = memberUserAccountId == ownerUserAccountId ? ChatGroupMemberRole.Owner : ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }

    private static void SeedPendingMember(Guid chatGroupId, Guid pendingMemberUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = pendingMemberUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Pending, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }
}
