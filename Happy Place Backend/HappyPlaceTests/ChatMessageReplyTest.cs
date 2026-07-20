using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatMessageReplyTest {
    // Tests - Storing The Link

    [Fact]
    public void ReplyStoresParentReferenceOnTheRowAndPlainSendsStoreNone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "the reply", parentMessageId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        List<ChatMessage> messages = LoadMessages(groupId);
        ChatMessage parentRow = messages.Single(message => message.Id == parentMessageId);
        ChatMessage replyRow = messages.Single(message => message.Id != parentMessageId);
        Assert.Null(parentRow.ReplyToChatMessageId);
        Assert.Equal(parentMessageId, replyRow.ReplyToChatMessageId);
    }

    [Fact]
    public void ReplyToAnotherMembersMessageIsAllowed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "from the owner");

        JsonElement root = Reply(testingMockProvidersContainer, memberAuthToken, groupId, Guid.NewGuid(), "from the member", parentMessageId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(parentMessageId.ToString(), root.GetProperty("message").GetProperty("replyTo").GetProperty("messageId").GetString());
    }

    [Fact]
    public void ReplyToYourOwnMessageIsAllowed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "first thought");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "second thought", parentMessageId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(parentMessageId.ToString(), root.GetProperty("message").GetProperty("replyTo").GetProperty("messageId").GetString());
    }

    // Tests - Refusals

    [Fact]
    public void ReplyToUnknownMessageReturnsInvalidReplyAndStoresNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello", Guid.NewGuid());

        Assert.Equal("invalidReply", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void ReplyToMessageInAnotherGroupReturnsInvalidReply() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);
        Guid foreignParentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, firstGroupId, "lives elsewhere");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, secondGroupId, Guid.NewGuid(), "hello", foreignParentMessageId);

        Assert.Equal("invalidReply", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(secondGroupId));
    }

    [Fact]
    public void RefusedReplyClaimsNoSequence() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "refused", Guid.NewGuid());
        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "first real message");

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(1, root.GetProperty("message").GetProperty("sequence").GetInt64());
    }

    [Fact]
    public void EmptyBodyOnReplyStillReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "   ", parentMessageId);

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    // Tests - Embedded Context

    [Fact]
    public void SendResponseEmbedsFullReplyContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, memberAuthToken, groupId, "help me think this through");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "of course", parentMessageId);

        JsonElement replyTo = root.GetProperty("message").GetProperty("replyTo");
        Assert.Equal(parentMessageId.ToString(), replyTo.GetProperty("messageId").GetString());
        Assert.Equal(memberUserAccountId.ToString(), replyTo.GetProperty("senderUserAccountId").GetString());
        Assert.False(string.IsNullOrEmpty(replyTo.GetProperty("senderDisplayName").GetString()));
        Assert.Equal(1, replyTo.GetProperty("kind").GetInt32());
        Assert.Equal("help me think this through", replyTo.GetProperty("preview").GetString());
        Assert.False(replyTo.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("guestMessagesRemaining").ValueKind);
    }

    [Fact]
    public void NonReplyEntriesCarryNullReplyTo() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "just a message");

        Assert.Equal(JsonValueKind.Null, root.GetProperty("message").GetProperty("replyTo").ValueKind);
    }

    [Fact]
    public void PollEntriesEmbedReplyContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");
        Guid replyMessageId = ReplyAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the reply", parentMessageId);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        JsonElement replyEntry = FindEntryById(root.GetProperty("changes"), replyMessageId);
        Assert.Equal(parentMessageId.ToString(), replyEntry.GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal("the original", replyEntry.GetProperty("replyTo").GetProperty("preview").GetString());
        JsonElement parentEntry = FindEntryById(root.GetProperty("changes"), parentMessageId);
        Assert.Equal(JsonValueKind.Null, parentEntry.GetProperty("replyTo").ValueKind);
    }

    [Fact]
    public void ListPageEntriesEmbedReplyContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");
        Guid replyMessageId = ReplyAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the reply", parentMessageId);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement replyEntry = FindEntryById(root.GetProperty("items"), replyMessageId);
        Assert.Equal(parentMessageId.ToString(), replyEntry.GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal("the original", replyEntry.GetProperty("replyTo").GetProperty("preview").GetString());
    }

    [Fact]
    public void ReplyChainsCarryEachLevelsOwnParent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid firstMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "level one");
        Guid secondMessageId = ReplyAndGetMessageId(testingMockProvidersContainer, memberAuthToken, groupId, "level two", firstMessageId);
        Guid thirdMessageId = ReplyAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "level three", secondMessageId);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement secondEntry = FindEntryById(root.GetProperty("items"), secondMessageId);
        JsonElement thirdEntry = FindEntryById(root.GetProperty("items"), thirdMessageId);
        Assert.Equal(firstMessageId.ToString(), secondEntry.GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal("level one", secondEntry.GetProperty("replyTo").GetProperty("preview").GetString());
        Assert.Equal(secondMessageId.ToString(), thirdEntry.GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal("level two", thirdEntry.GetProperty("replyTo").GetProperty("preview").GetString());
    }

    [Fact]
    public void PreviewTruncatesLongParentBodies() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, new string('a', 300));

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "short answer", parentMessageId);

        string preview = root.GetProperty("message").GetProperty("replyTo").GetProperty("preview").GetString();
        Assert.Equal(new string('a', 140), preview);
    }

    [Fact]
    public void ReplyContextForMediaParentHasKindAndNullPreview() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid mediaParentMessageId = SeedMediaKindMessage(groupId, ownerUserAccountId, 1);

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "nice photo", mediaParentMessageId);

        JsonElement replyTo = root.GetProperty("message").GetProperty("replyTo");
        Assert.Equal(2, replyTo.GetProperty("kind").GetInt32());
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("preview").ValueKind);
        Assert.False(replyTo.GetProperty("isDeleted").GetBoolean());
    }

    // Tests - Direct Chats And Media Replies

    [Fact]
    public void ReplyWorksInDirectChatsBetweenFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair friendshipPair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(friendshipPair.RequesterAuthToken);
        JsonElement openRoot = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/openDirect", new { AuthToken = friendshipPair.RequesterAuthToken, Username = friendshipPair.AddresseeUsername }).ReadContentAsJsonDocument().RootElement.Clone();
        Guid directChatGroupId = Guid.Parse(openRoot.GetProperty("chatGroupId").GetString());
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, friendshipPair.RequesterAuthToken, directChatGroupId, "hey, how are you holding up");

        JsonElement root = Reply(testingMockProvidersContainer, friendshipPair.AddresseeAuthToken, directChatGroupId, Guid.NewGuid(), "hanging in there, thanks for asking", parentMessageId);

        Assert.Equal("opened", openRoot.GetProperty("status").GetString());
        Assert.Equal("sent", root.GetProperty("status").GetString());
        JsonElement replyTo = root.GetProperty("message").GetProperty("replyTo");
        Assert.Equal(parentMessageId.ToString(), replyTo.GetProperty("messageId").GetString());
        Assert.Equal(requesterUserAccountId.ToString(), replyTo.GetProperty("senderUserAccountId").GetString());
        Assert.Equal("hey, how are you holding up", replyTo.GetProperty("preview").GetString());
    }

    [Fact]
    public void MediaReplyCarriesTheLink() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "look at this");
        Guid mediaId = SeedImageAsset(groupId, ownerUserAccountId);

        JsonElement root = SendMediaReply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), mediaId, parentMessageId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(2, root.GetProperty("message").GetProperty("kind").GetInt32());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("message").GetProperty("mediaUrl").GetString()));
        Assert.Equal(parentMessageId.ToString(), root.GetProperty("message").GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal(1, root.GetProperty("message").GetProperty("replyTo").GetProperty("kind").GetInt32());
        ChatMessage mediaReplyRow = LoadMessages(groupId).Single(message => message.Kind == ChatMessageKind.Image);
        Assert.Equal(parentMessageId, mediaReplyRow.ReplyToChatMessageId);
    }

    // Tests - Tombstones And Edge Parents

    [Fact]
    public void ReplyToDeletedParentSucceedsWithTombstoneContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "regretted");
        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, parentMessageId);

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "still replying", parentMessageId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        JsonElement replyTo = root.GetProperty("message").GetProperty("replyTo");
        Assert.Equal(parentMessageId.ToString(), replyTo.GetProperty("messageId").GetString());
        Assert.True(replyTo.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("preview").ValueKind);
    }

    [Fact]
    public void ParentDeletedAfterReplyRebuildsTombstoneOnFreshReads() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "soon to vanish");
        Guid replyMessageId = ReplyAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the reply", parentMessageId);
        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, parentMessageId);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement replyEntry = FindEntryById(root.GetProperty("items"), replyMessageId);
        Assert.True(replyEntry.GetProperty("replyTo").GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, replyEntry.GetProperty("replyTo").GetProperty("preview").ValueKind);
        JsonElement parentEntry = FindEntryById(root.GetProperty("items"), parentMessageId);
        Assert.True(parentEntry.GetProperty("isDeleted").GetBoolean());
    }

    [Fact]
    public void ReplyContextForDeletedAccountParentHasNullSender() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, memberAuthToken, groupId, "a message that outlives me");
        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/deleteAccount", new { AuthToken = memberAuthToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "replying anyway", parentMessageId);

        JsonElement replyTo = root.GetProperty("message").GetProperty("replyTo");
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("senderUserAccountId").ValueKind);
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("senderDisplayName").ValueKind);
        Assert.Equal("a message that outlives me", replyTo.GetProperty("preview").GetString());
        Assert.False(replyTo.GetProperty("isDeleted").GetBoolean());
    }

    [Fact]
    public void DanglingParentReferenceRendersAsDeletedContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid danglingParentId = Guid.NewGuid();
        Guid orphanReplyMessageId = SeedReplyMessage(groupId, ownerUserAccountId, 1, danglingParentId);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement replyTo = FindEntryById(root.GetProperty("items"), orphanReplyMessageId).GetProperty("replyTo");
        Assert.Equal(danglingParentId.ToString(), replyTo.GetProperty("messageId").GetString());
        Assert.True(replyTo.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("preview").ValueKind);
        Assert.Equal(JsonValueKind.Null, replyTo.GetProperty("senderDisplayName").ValueKind);
    }

    // Tests - Idempotency And Sequencing

    [Fact]
    public void DuplicateRetryEchoesTheOriginalReplyContext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");
        Guid clientMessageId = Guid.NewGuid();
        JsonElement firstRoot = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "the reply", parentMessageId);

        JsonElement secondRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "the reply");

        Assert.Equal("sent", firstRoot.GetProperty("status").GetString());
        Assert.Equal("duplicate", secondRoot.GetProperty("status").GetString());
        Assert.Equal(parentMessageId.ToString(), secondRoot.GetProperty("message").GetProperty("replyTo").GetProperty("messageId").GetString());
        Assert.Equal(2, CountMessages(groupId));
    }

    [Fact]
    public void RepliesKeepSequencesGapFree() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "one");

        Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "two", parentMessageId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "three");

        List<long> sequences = [.. LoadMessages(groupId).Select(message => message.Sequence).OrderBy(sequence => sequence)];
        Assert.Equal([1, 2, 3], sequences);
    }

    // Tests - Response Shape

    [Fact]
    public void ReplyContextContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid parentMessageId = SendAndGetMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, "the original");

        JsonElement root = Reply(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "the reply", parentMessageId);
        List<string> actualProperties = [.. root.GetProperty("message").GetProperty("replyTo").EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["isDeleted", "kind", "messageId", "preview", "senderDisplayName", "senderUserAccountId"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Reply(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body, Guid replyToMessageId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body, ReplyToMessageId = replyToMessageId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement SendMediaReply(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, Guid mediaId, Guid replyToMessageId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, MediaId = mediaId, ReplyToMessageId = replyToMessageId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static Guid SendAndGetMessageId(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        return Guid.Parse(Send(testingMockProvidersContainer, authToken, chatGroupId, Guid.NewGuid(), body).GetProperty("message").GetProperty("id").GetString());
    }

    private static Guid ReplyAndGetMessageId(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body, Guid replyToMessageId) {
        return Guid.Parse(Reply(testingMockProvidersContainer, authToken, chatGroupId, Guid.NewGuid(), body, replyToMessageId).GetProperty("message").GetProperty("id").GetString());
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long sinceChangeSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = sinceChangeSequence }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement ListPage(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string cursor) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = authToken, ChatGroupId = chatGroupId, Cursor = cursor }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void DeleteOwn(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId }).EnsureSuccessStatusCode();
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

    private static Guid SeedMediaKindMessage(Guid groupId, Guid senderUserAccountId, long sequence) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid messageId = Guid.NewGuid();
        dbContext.ChatMessages.Add(new ChatMessage { Id = messageId, ChatGroupId = groupId, SenderUserAccountId = senderUserAccountId, ClientMessageId = Guid.NewGuid(), Kind = ChatMessageKind.Image, BodyCipher = null, CipherVersion = MessageCipher.CurrentVersion, Sequence = sequence, ChangeSequence = sequence, IsDeleted = false, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastMessageSequence, sequence).SetProperty(field => field.LastChangeSequence, sequence));
        return messageId;
    }

    private static Guid SeedReplyMessage(Guid groupId, Guid senderUserAccountId, long sequence, Guid replyToChatMessageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid messageId = Guid.NewGuid();
        dbContext.ChatMessages.Add(new ChatMessage { Id = messageId, ChatGroupId = groupId, SenderUserAccountId = senderUserAccountId, ClientMessageId = Guid.NewGuid(), ReplyToChatMessageId = replyToChatMessageId, Kind = ChatMessageKind.Text, BodyCipher = MessageCipher.Encrypt("orphaned reply"), CipherVersion = MessageCipher.CurrentVersion, Sequence = sequence, ChangeSequence = sequence, IsDeleted = false, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastMessageSequence, sequence).SetProperty(field => field.LastChangeSequence, sequence));
        return messageId;
    }

    private static Guid SeedImageAsset(Guid groupId, Guid uploaderUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid mediaId = Guid.NewGuid();
        byte[] contentBytes = [1, 2, 3, 4];
        dbContext.ChatMediaAssets.Add(new ChatMediaAsset { Id = mediaId, ChatGroupId = groupId, UploaderUserAccountId = uploaderUserAccountId, Kind = ChatMessageKind.Image, StorageMode = (ChatMediaStorageMode)1, ContentBytes = contentBytes, ContentType = "image/jpeg", ByteCount = contentBytes.Length, Width = 800, Height = 600, CipherVersion = 0, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        return mediaId;
    }

    // Helpers - Reading

    private static int CountMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == groupId);
    }

    private static List<ChatMessage> LoadMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return [.. dbContext.ChatMessages.Where(field => field.ChatGroupId == groupId)];
    }

    private static JsonElement FindEntryById(JsonElement arrayElement, Guid messageId) {
        string target = messageId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("id").GetString() == target)
                return entry;
        throw new InvalidOperationException("Message was not present in the response.");
    }
}
