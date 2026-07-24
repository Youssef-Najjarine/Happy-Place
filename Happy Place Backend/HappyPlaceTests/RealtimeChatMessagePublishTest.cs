using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimeChatMessagePublishTest {
    // Tests - Send

    [Fact]
    public void SendPublishesMessagesKindToEveryActiveMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = "hello there" });

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        AssertChatGroupChangedForUsers(sentEvents, chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    [Fact]
    public void SendByNonMemberPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string outsiderAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Outsider");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = outsiderAuthToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = "should not land" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Reactions

    [Fact]
    public void ReactAndRemoveReactionEachPublishMessagesKind() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        Guid clientMessageId = Guid.NewGuid();
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = "react to me" });
        Guid messageId = LoadMessageId(clientMessageId);

        int reactBaselineCount = CountEvents(testingMockProvidersContainer);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/react", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, MessageId = messageId, Emoji = "❤️" });
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, reactBaselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);

        int removeBaselineCount = CountEvents(testingMockProvidersContainer);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/react", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, MessageId = messageId, Emoji = "" });
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, removeBaselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    // Tests - Deletion

    [Fact]
    public void DeleteOwnPublishesMessagesKind() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        Guid clientMessageId = Guid.NewGuid();
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = "delete me" });
        Guid messageId = LoadMessageId(clientMessageId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, MessageId = messageId });

        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    [Fact]
    public void DeleteOfAnotherMembersMessagePublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        Guid clientMessageId = Guid.NewGuid();
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = "not yours" });
        Guid messageId = LoadMessageId(clientMessageId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/deleteOwn", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, MessageId = messageId });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Read Pointers

    [Fact]
    public void MarkReadAdvancePublishesAndRepeatDoesNot() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = "read me" });

        int advanceBaselineCount = CountEvents(testingMockProvidersContainer);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/markRead", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, UpToSequence = 1 });
        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, advanceBaselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);

        int repeatBaselineCount = CountEvents(testingMockProvidersContainer);
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/markRead", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, UpToSequence = 1 });
        Assert.Empty(EventsAfter(testingMockProvidersContainer, repeatBaselineCount));
    }

    // Tests - Typing

    [Fact]
    public void TypingPublishesMessagesKind() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/typing", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId });

        AssertChatGroupChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), chatGroupId, RealtimePublisher.MessagesKind, memberUserAccountIds);
    }

    // Tests - Reports

    [Fact]
    public void ReportPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        Guid clientMessageId = Guid.NewGuid();
        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = "report me" });
        Guid messageId = LoadMessageId(clientMessageId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/chatMessage/report", new { AuthToken = memberAuthToken, ChatGroupId = chatGroupId, MessageId = messageId, Reason = "test report" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Helpers

    private static HttpResponseMessage PostJsonOrFail(TestingMockProvidersContainer testingMockProvidersContainer, string url, object jsonData) {
        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson(url, jsonData);
        Assert.True(response.IsSuccessStatusCode);
        return response;
    }

    private static Guid SeedActiveGroup(List<Guid> memberUserAccountIds, Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Realtime Chat Publish Group", OwnerUserAccountId = ownerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = memberUserAccountId, MemberRole = memberUserAccountId == ownerUserAccountId ? ChatGroupMemberRole.Owner : ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }

    private static Guid LoadMessageId(Guid clientMessageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Single(field => field.ClientMessageId == clientMessageId).Id;
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
}
