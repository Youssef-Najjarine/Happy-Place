using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimeHelpPublishTest {
    // Tests - Requests

    [Fact]
    public void CreateRequestPublishesBroadcastAndSeekerHelpChanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        AssertSingleOpenRequestsBroadcast(OpenRequestsBroadcastEvents(sentEvents));
        List<Guid> expectedUserAccountIds = [seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    [Fact]
    public void CreateRequestWhenAlreadyOpenPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void CancelRequestPublishesBroadcastAndSeekerHelpChanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        AssertSingleOpenRequestsBroadcast(OpenRequestsBroadcastEvents(sentEvents));
        List<Guid> expectedUserAccountIds = [seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    [Fact]
    public void CancelOfActiveGroupPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/cancel", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Connect

    [Fact]
    public void ConnectWithOffersPublishesInvitesSeekerAndMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string firstHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "First Helper");
        string secondHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Second Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        Guid firstHelperUserAccountId = HelpParticipant.ResolveUserAccountId(firstHelperAuthToken).Value;
        Guid secondHelperUserAccountId = HelpParticipant.ResolveUserAccountId(secondHelperAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/createOffer", new { AuthToken = firstHelperAuthToken, ChatGroupId = chatGroupId });
        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/createOffer", new { AuthToken = secondHelperAuthToken, ChatGroupId = chatGroupId });
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(5, sentEvents.Count);
        AssertSingleOpenRequestsBroadcast(OpenRequestsBroadcastEvents(sentEvents));
        List<Guid> expectedHelpChangedUserAccountIds = [firstHelperUserAccountId, secondHelperUserAccountId, seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedHelpChangedUserAccountIds);
        List<Guid> expectedMembershipUserAccountIds = [seekerUserAccountId];
        AssertChatGroupChangedForUsers(ChatGroupChangedEvents(sentEvents), chatGroupId, RealtimePublisher.MembershipKind, expectedMembershipUserAccountIds);
    }

    [Fact]
    public void ConnectWhenAlreadyActivePublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId });
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void ConnectWithoutOffersPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Offers

    [Fact]
    public void CreateOfferPublishesHelpChangedToHelperAndSeeker() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        Guid helperUserAccountId = HelpParticipant.ResolveUserAccountId(helperAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        List<Guid> expectedUserAccountIds = [helperUserAccountId, seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    [Fact]
    public void DeclineOfferPublishesHelpChangedToHelperAndSeeker() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        Guid helperUserAccountId = HelpParticipant.ResolveUserAccountId(helperAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        List<Guid> expectedUserAccountIds = [helperUserAccountId, seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    [Fact]
    public void WithdrawOfferPublishesHelpChangedToHelperAndSeeker() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        Guid helperUserAccountId = HelpParticipant.ResolveUserAccountId(helperAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        List<Guid> expectedUserAccountIds = [helperUserAccountId, seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    [Fact]
    public void WithdrawWithoutOfferPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        PostJsonOrFail(testingMockProvidersContainer, "api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Feeling low" });
        Guid chatGroupId = LoadProvisionalGroupId(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Joining

    [Fact]
    public void JoinPublicGroupPublishesMembershipAndJoinerHelpChanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string joinerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Joiner");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid joinerUserAccountId = HelpParticipant.ResolveUserAccountId(joinerAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId, true);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/join", new { AuthToken = joinerAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(3, sentEvents.Count);
        List<Guid> expectedMembershipUserAccountIds = [ownerUserAccountId, joinerUserAccountId];
        AssertChatGroupChangedForUsers(ChatGroupChangedEvents(sentEvents), chatGroupId, RealtimePublisher.MembershipKind, expectedMembershipUserAccountIds);
        List<Guid> expectedHelpChangedUserAccountIds = [joinerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedHelpChangedUserAccountIds);
    }

    [Fact]
    public void JoinByInvitedHelperOnPrivateGroupPublishesMembershipAndHelpChanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid helperUserAccountId = HelpParticipant.ResolveUserAccountId(helperAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId, false);
        SeedConnectedOffer(chatGroupId, helperUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(3, sentEvents.Count);
        List<Guid> expectedMembershipUserAccountIds = [ownerUserAccountId, helperUserAccountId];
        AssertChatGroupChangedForUsers(ChatGroupChangedEvents(sentEvents), chatGroupId, RealtimePublisher.MembershipKind, expectedMembershipUserAccountIds);
        List<Guid> expectedHelpChangedUserAccountIds = [helperUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedHelpChangedUserAccountIds);
    }

    [Fact]
    public void DeclineInvitePublishesHelpChangedToHelper() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid helperUserAccountId = HelpParticipant.ResolveUserAccountId(helperAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId, true);
        SeedConnectedOffer(chatGroupId, helperUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/declineInvite", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Single(sentEvents);
        List<Guid> expectedUserAccountIds = [helperUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    // Tests - Sweep

    [Fact]
    public void OpenRequestsSweepPublishesBroadcastAndExpiredSeekerHelpChanged() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Seeker");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        Guid seekerUserAccountId = HelpParticipant.ResolveUserAccountId(seekerAuthToken).Value;
        SeedStaleProvisionalGroup(seekerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/helpOffer/openRequests", new { AuthToken = helperAuthToken });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        Assert.Equal(2, sentEvents.Count);
        AssertSingleOpenRequestsBroadcast(OpenRequestsBroadcastEvents(sentEvents));
        List<Guid> expectedUserAccountIds = [seekerUserAccountId];
        AssertHelpChangedForUsers(UserHelpChangedEvents(sentEvents), expectedUserAccountIds);
    }

    // Helpers

    private static HttpResponseMessage PostJsonOrFail(TestingMockProvidersContainer testingMockProvidersContainer, string url, object jsonData) {
        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson(url, jsonData);
        Assert.True(response.IsSuccessStatusCode);
        return response;
    }

    private static Guid SeedActiveGroup(List<Guid> memberUserAccountIds, Guid ownerUserAccountId, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Realtime Help Publish Group", OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = memberUserAccountId, MemberRole = memberUserAccountId == ownerUserAccountId ? ChatGroupMemberRole.Owner : ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }

    private static void SeedStaleProvisionalGroup(Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime staleTimestamp = DateTime.UtcNow.AddDays(-8);
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Stale Request", OwnerUserAccountId = ownerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Provisional, CreatedAtUtc = staleTimestamp, LastSeenAtUtc = staleTimestamp });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = staleTimestamp });
        dbContext.SaveChanges();
    }

    private static void SeedConnectedOffer(Guid chatGroupId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.HelpOffers.Add(new HelpOffer { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, HelperUserAccountId = helperUserAccountId, Status = HelpOfferStatus.Connected, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    private static Guid LoadProvisionalGroupId(Guid seekerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.OwnerUserAccountId == seekerUserAccountId && field.Status == ChatGroupStatus.Provisional).Id;
    }

    private static int CountEvents(TestingMockProvidersContainer testingMockProvidersContainer) {
        return testingMockProvidersContainer.RealtimeProvider.SentEvents.Count();
    }

    private static List<RealtimeSentEvent> EventsAfter(TestingMockProvidersContainer testingMockProvidersContainer, int baselineCount) {
        return [.. testingMockProvidersContainer.RealtimeProvider.SentEvents.Skip(baselineCount)];
    }

    private static List<RealtimeSentEvent> OpenRequestsBroadcastEvents(List<RealtimeSentEvent> sentEvents) {
        return [.. sentEvents.Where(field => field.GroupName == RealtimePublisher.HelpersListeningGroupName)];
    }

    private static List<RealtimeSentEvent> UserHelpChangedEvents(List<RealtimeSentEvent> sentEvents) {
        return [.. sentEvents.Where(field => field.EventName == RealtimePublisher.HelpChangedEventName && field.GroupName != RealtimePublisher.HelpersListeningGroupName)];
    }

    private static List<RealtimeSentEvent> ChatGroupChangedEvents(List<RealtimeSentEvent> sentEvents) {
        return [.. sentEvents.Where(field => field.EventName == RealtimePublisher.ChatGroupChangedEventName)];
    }

    private static void AssertSingleOpenRequestsBroadcast(List<RealtimeSentEvent> broadcastEvents) {
        RealtimeSentEvent broadcastEvent = Assert.Single(broadcastEvents);
        Assert.Equal(RealtimePublisher.HelpChangedEventName, broadcastEvent.EventName);
        Assert.Empty(broadcastEvent.Payload);
    }

    private static void AssertHelpChangedForUsers(List<RealtimeSentEvent> helpChangedEvents, List<Guid> expectedUserAccountIds) {
        Assert.Equal(expectedUserAccountIds.Count, helpChangedEvents.Count);
        foreach (Guid expectedUserAccountId in expectedUserAccountIds)
            Assert.Contains(helpChangedEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(expectedUserAccountId));
        foreach (RealtimeSentEvent helpChangedEvent in helpChangedEvents) {
            Assert.Equal(RealtimePublisher.HelpChangedEventName, helpChangedEvent.EventName);
            Assert.Empty(helpChangedEvent.Payload);
        }
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
}
