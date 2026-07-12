using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatMessageNotificationTest {
    // Tests - Channel Creation

    [Fact]
    public void SendCreatesChannelsForOtherActiveMembersOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string firstMemberAuthToken = CreateUser(testingMockProvidersContainer, "First Member");
        string secondMemberAuthToken = CreateUser(testingMockProvidersContainer, "Second Member");
        string pendingAuthToken = CreateUser(testingMockProvidersContainer, "Pending");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstMemberUserAccountId = ResolveUserAccountId(firstMemberAuthToken);
        Guid secondMemberUserAccountId = ResolveUserAccountId(secondMemberAuthToken);
        Guid pendingUserAccountId = ResolveUserAccountId(pendingAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, firstMemberUserAccountId);
        AddActiveMember(groupId, secondMemberUserAccountId);
        AddPendingMember(groupId, pendingUserAccountId);

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        Assert.NotNull(LoadMessagesChannel(groupId, firstMemberUserAccountId));
        Assert.NotNull(LoadMessagesChannel(groupId, secondMemberUserAccountId));
        Assert.Null(LoadMessagesChannel(groupId, ownerUserAccountId));
        Assert.Null(LoadMessagesChannel(groupId, pendingUserAccountId));
        Assert.Equal(2, CountMessagesChannels(groupId));
    }

    [Fact]
    public void NewJoinerGetsChannelOnNextSendOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "before joiner");
        AddActiveMember(groupId, joinerUserAccountId);
        Assert.Null(LoadMessagesChannel(groupId, joinerUserAccountId));

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "after joiner");

        Assert.NotNull(LoadMessagesChannel(groupId, joinerUserAccountId));
    }

    // Tests - Count Pushes

    [Fact]
    public void SweepDeliversNewMessagePushWithGroupTitleAndCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello there");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        PushMessage push = Assert.Single(ChatPushes(testingMockProvidersContainer, groupId));
        Assert.Equal("member-device-token", push.Token);
        Assert.Equal("My Group", push.Title);
        Assert.Equal("1 new message.", push.Body);
        Assert.True(push.Alerting);
        Assert.Equal("chatMessages", push.Data["type"]);
        Assert.Equal("1", push.Data["count"]);
        Assert.Equal(groupId.ToString(), push.Data["chatGroupId"]);
        NotificationChannel channel = LoadMessagesChannel(groupId, memberUserAccountId);
        Assert.Equal(1, channel.LastSentCount);
        Assert.True(channel.IsLive);
        Assert.Null(channel.DueAtUtc);
    }

    [Fact]
    public void SecondMessageUpdatesCountToTwo() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "first");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "second");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        List<PushMessage> pushes = ChatPushes(testingMockProvidersContainer, groupId);
        Assert.Equal(2, pushes.Count);
        Assert.Equal("2 new messages.", pushes[^1].Body);
        Assert.Equal("2", pushes[^1].Data["count"]);
        Assert.Equal(2, LoadMessagesChannel(groupId, memberUserAccountId).LastSentCount);
    }

    [Fact]
    public void OwnMessagesExcludedFromRecipientCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "from owner");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "from member");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        PushMessage push = Assert.Single(ChatPushes(testingMockProvidersContainer, groupId));
        Assert.Equal("member-device-token", push.Token);
        Assert.Equal("1", push.Data["count"]);
    }

    [Fact]
    public void DeletedMessageExcludedFromCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "kept");
        Guid deletedMessageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "removed");

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MessageId = deletedMessageId }).EnsureSuccessStatusCode();
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        PushMessage push = Assert.Single(ChatPushes(testingMockProvidersContainer, groupId));
        Assert.Equal("1", push.Data["count"]);
        Assert.Equal("1 new message.", push.Body);
    }

    // Tests - Read Semantics

    [Fact]
    public void MarkReadTriggersDismissalAndClearsLive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();
        Assert.True(LoadMessagesChannel(groupId, memberUserAccountId).IsLive);

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = memberAuthToken, ChatGroupId = groupId, UpToSequence = 1 }).EnsureSuccessStatusCode();
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        PushMessage dismissal = Assert.Single(DismissalPushes(testingMockProvidersContainer, groupId));
        Assert.Equal("member-device-token", dismissal.Token);
        NotificationChannel channel = LoadMessagesChannel(groupId, memberUserAccountId);
        Assert.False(channel.IsLive);
        Assert.Equal(0, channel.LastSentCount);
    }

    [Fact]
    public void PartialReadLowersCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "two");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "three");

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = memberAuthToken, ChatGroupId = groupId, UpToSequence = 1 }).EnsureSuccessStatusCode();
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();

        PushMessage push = Assert.Single(ChatPushes(testingMockProvidersContainer, groupId));
        Assert.Equal("2 new messages.", push.Body);
        Assert.Equal("2", push.Data["count"]);
    }

    // Tests - Mutation Silence

    [Fact]
    public void ReactionDoesNotDirtyMessageChannels() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();
        Assert.Null(LoadMessagesChannel(groupId, memberUserAccountId).DueAtUtc);

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = memberAuthToken, ChatGroupId = groupId, MessageId = messageId, Kind = 1 }).EnsureSuccessStatusCode();

        Assert.Null(LoadMessagesChannel(groupId, memberUserAccountId).DueAtUtc);
    }

    [Fact]
    public void DeleteOwnDoesNotDirtyMessageChannels() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();
        Assert.Null(LoadMessagesChannel(groupId, memberUserAccountId).DueAtUtc);

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MessageId = messageId }).EnsureSuccessStatusCode();

        Assert.Null(LoadMessagesChannel(groupId, memberUserAccountId).DueAtUtc);
    }

    // Tests - Teardown

    [Fact]
    public void MemberLeaveRemovesTheirChannelOnly() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string leaverAuthToken = CreateUser(testingMockProvidersContainer, "Leaver");
        string stayerAuthToken = CreateUser(testingMockProvidersContainer, "Stayer");
        Guid leaverUserAccountId = ResolveUserAccountId(leaverAuthToken);
        Guid stayerUserAccountId = ResolveUserAccountId(stayerAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, leaverUserAccountId);
        AddActiveMember(groupId, stayerUserAccountId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = leaverAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        Assert.Null(LoadMessagesChannel(groupId, leaverUserAccountId));
        Assert.NotNull(LoadMessagesChannel(groupId, stayerUserAccountId));
    }

    [Fact]
    public void RemovedMemberChannelTornDown() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string removedAuthToken = CreateUser(testingMockProvidersContainer, "Removed");
        Guid removedUserAccountId = ResolveUserAccountId(removedAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, removedUserAccountId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        Assert.NotNull(LoadMessagesChannel(groupId, removedUserAccountId));

        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MemberUserAccountId = removedUserAccountId }).EnsureSuccessStatusCode();

        Assert.Null(LoadMessagesChannel(groupId, removedUserAccountId));
    }

    [Fact]
    public void GroupDeleteRemovesAllMessageChannelsWithDismissal() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedDeviceToken(memberUserAccountId, "member-device-token");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        ForceMessageChannelsDue(groupId);
        NotificationDispatchManager.Sweep();
        Assert.True(LoadMessagesChannel(groupId, memberUserAccountId).IsLive);

        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        Assert.Equal(0, CountMessagesChannels(groupId));
        Assert.Single(DismissalPushes(testingMockProvidersContainer, groupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static Guid Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).ReadContentAsJsonDocument().RootElement;
        return Guid.Parse(root.GetProperty("message").GetProperty("id").GetString());
    }

    private static void ForceMessageChannelsDue(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<NotificationChannel> dirtyChannels = [.. dbContext.NotificationChannels
            .Where(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == groupId && field.DueAtUtc != null)];
        foreach (NotificationChannel dirtyChannel in dirtyChannels) {
            dirtyChannel.DueAtUtc = DateTime.UtcNow.AddSeconds(-1);
            dirtyChannel.LastSentAtUtc = DateTime.UtcNow.AddSeconds(-2);
        }
        dbContext.SaveChanges();
    }

    private static List<PushMessage> ChatPushes(TestingMockProvidersContainer testingMockProvidersContainer, Guid groupId) {
        return [.. testingMockProvidersContainer.PushProvider.SentMessages.Where(field => field.CollapseId == $"chat-messages-{groupId}" && !field.IsDismiss)];
    }

    private static List<PushMessage> DismissalPushes(TestingMockProvidersContainer testingMockProvidersContainer, Guid groupId) {
        return [.. testingMockProvidersContainer.PushProvider.SentMessages.Where(field => field.CollapseId == $"chat-messages-{groupId}" && field.IsDismiss)];
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
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
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void AddPendingMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Pending, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void SeedDeviceToken(Guid userAccountId, string token) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.DeviceTokens.Add(new DeviceToken { Id = Guid.NewGuid(), UserAccountId = userAccountId, Token = token, Platform = "ios", CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static NotificationChannel LoadMessagesChannel(Guid groupId, Guid recipientUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.NotificationChannels.SingleOrDefault(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == groupId && field.RecipientUserAccountId == recipientUserAccountId);
    }

    private static int CountMessagesChannels(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.NotificationChannels.Count(field => field.Kind == NotificationChannelKind.Messages && field.ScopeChatGroupId == groupId);
    }
}
