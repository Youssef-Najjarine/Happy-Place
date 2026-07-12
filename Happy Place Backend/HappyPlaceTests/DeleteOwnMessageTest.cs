using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeleteOwnMessageTest {
    // Tests - Authentication Failures

    [Fact]
    public void DeleteOwnEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeleteOwnInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void DeleteOwnMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = DeleteOwn(testingMockProvidersContainer, strangerAuthToken, groupId, messageId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.False(IsMessageDeleted(messageId));
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = DeleteOwn(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Message Gates

    [Fact]
    public void UnknownMessageReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid());

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void MessageFromOtherGroupReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);
        Guid foreignMessageId = Send(testingMockProvidersContainer, ownerAuthToken, secondGroupId, "hello");

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, firstGroupId, foreignMessageId);

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
        Assert.False(IsMessageDeleted(foreignMessageId));
    }

    [Fact]
    public void OthersMessageReturnsNotYours() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = DeleteOwn(testingMockProvidersContainer, memberAuthToken, groupId, messageId);

        Assert.Equal("notYours", root.GetProperty("status").GetString());
        Assert.False(IsMessageDeleted(messageId));
    }

    // Tests - Soft Delete Semantics

    [Fact]
    public void SenderDeletesOwnFlipsFlagAndRetainsContent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        string body = "a message the record keeps " + Guid.NewGuid();
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, body);

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        ChatMessage message = LoadMessage(messageId);
        Assert.True(message.IsDeleted);
        Assert.NotNull(message.BodyCipher);
        Assert.Equal(body, MessageCipher.Decrypt(message.BodyCipher));
    }

    [Fact]
    public void DeleteIsIdempotentWithoutRestamping() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);
        long stampedChangeSequence = LoadMessage(messageId).ChangeSequence;

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);

        Assert.Equal("deleted", root.GetProperty("status").GetString());
        Assert.Equal(stampedChangeSequence, LoadMessage(messageId).ChangeSequence);
    }

    [Fact]
    public void DeleteBumpsChangeSequenceAndSurfacesAsTombstoneInPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "soon to vanish");
        JsonElement quietPoll = Poll(testingMockProvidersContainer, memberAuthToken, groupId, 1);
        Assert.Equal(0, quietPoll.GetProperty("changes").GetArrayLength());

        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);
        JsonElement root = Poll(testingMockProvidersContainer, memberAuthToken, groupId, 1);

        Assert.Equal(1, root.GetProperty("changes").GetArrayLength());
        JsonElement change = root.GetProperty("changes")[0];
        Assert.Equal(messageId.ToString(), change.GetProperty("id").GetString());
        Assert.True(change.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, change.GetProperty("body").ValueKind);
        Assert.Equal(2, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void DeleteClearsReactions() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = memberAuthToken, ChatGroupId = groupId, MessageId = messageId, Emoji = "\u2764\uFE0F" }).EnsureSuccessStatusCode();
        Assert.Equal(1, CountReactions(messageId));

        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);

        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void DeletedMessageStillOccupiesItsSequenceInListPage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "first");
        Guid middleMessageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "middle");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "last");
        DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, middleMessageId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal(3, root.GetProperty("items").GetArrayLength());
        JsonElement middleItem = root.GetProperty("items")[1];
        Assert.Equal(2, middleItem.GetProperty("sequence").GetInt64());
        Assert.True(middleItem.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, middleItem.GetProperty("body").ValueKind);
        Assert.Equal("last", root.GetProperty("items")[0].GetProperty("body").GetString());
        Assert.Equal("first", root.GetProperty("items")[2].GetProperty("body").GetString());
    }

    // Tests - Response Shape

    [Fact]
    public void DeleteOwnResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = DeleteOwn(testingMockProvidersContainer, ownerAuthToken, groupId, messageId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static Guid Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).ReadContentAsJsonDocument().RootElement;
        return Guid.Parse(root.GetProperty("message").GetProperty("id").GetString());
    }

    private static JsonElement DeleteOwn(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long sinceChangeSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = sinceChangeSequence }).ReadContentAsJsonDocument().RootElement.Clone();
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

    // Helpers - Reading

    private static ChatMessage LoadMessage(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Single(field => field.Id == messageId);
    }

    private static bool IsMessageDeleted(Guid messageId) {
        return LoadMessage(messageId).IsDeleted;
    }

    private static int CountReactions(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReactions.Count(field => field.ChatMessageId == messageId);
    }
}
