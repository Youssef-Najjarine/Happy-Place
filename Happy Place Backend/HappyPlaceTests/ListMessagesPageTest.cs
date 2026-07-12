using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListMessagesPageTest {
    // Tests - Authentication Failures

    [Fact]
    public void ListPageEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListPageInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListPageMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = ListPage(testingMockProvidersContainer, strangerAuthToken, groupId, null);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = ListPage(testingMockProvidersContainer, requesterAuthToken, groupId, null);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = ListPage(testingMockProvidersContainer, memberAuthToken, groupId, null);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ProvisionalGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = ListPage(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), null);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Paging

    [Fact]
    public void EmptyGroupReturnsOkWithNoItems() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
        Assert.Equal(0, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void FirstPageReturnsNewestMessagesDescending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(ownerUserAccountId.ToString(), root.GetProperty("callerUserAccountId").GetString());
        List<long> sequences = [.. root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("sequence").GetInt64())];
        Assert.Equal([5, 4, 3, 2, 1], sequences);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public void PagesWalkBackwardThroughFullHistory() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 85);

        List<long> allSequences = [];
        string cursor = null;
        int pageCount = 0;
        while (true) {
            JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, cursor);
            Assert.Equal("ok", root.GetProperty("status").GetString());
            allSequences.AddRange(root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("sequence").GetInt64()));
            pageCount++;
            JsonElement nextCursorElement = root.GetProperty("nextCursor");
            if (nextCursorElement.ValueKind == JsonValueKind.Null)
                break;
            cursor = nextCursorElement.GetString();
        }

        Assert.Equal(3, pageCount);
        Assert.Equal(85, allSequences.Count);
        Assert.Equal([.. Enumerable.Range(1, 85).Select(value => (long)value).Reverse()], allSequences);
    }

    [Fact]
    public void ExactPageBoundaryEndsWithNullCursor() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 41);

        JsonElement firstRoot = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);
        string cursor = firstRoot.GetProperty("nextCursor").GetString();
        JsonElement secondRoot = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, cursor);

        Assert.Equal(40, firstRoot.GetProperty("items").GetArrayLength());
        Assert.NotNull(cursor);
        Assert.Equal(1, secondRoot.GetProperty("items").GetArrayLength());
        Assert.Equal(1, secondRoot.GetProperty("items")[0].GetProperty("sequence").GetInt64());
        Assert.Equal(JsonValueKind.Null, secondRoot.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public void InvalidCursorFallsBackToFirstPage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 3);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, "not-a-real-cursor");

        Assert.Equal("ok", root.GetProperty("status").GetString());
        List<long> sequences = [.. root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("sequence").GetInt64())];
        Assert.Equal([3, 2, 1], sequences);
    }

    [Fact]
    public void CursorFromDifferentGroupFallsBackToFirstPage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);
        SeedMessages(firstGroupId, ownerUserAccountId, 41);
        SeedMessages(secondGroupId, ownerUserAccountId, 2);
        string foreignCursor = ListPage(testingMockProvidersContainer, ownerAuthToken, firstGroupId, null).GetProperty("nextCursor").GetString();

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, secondGroupId, foreignCursor);

        List<long> sequences = [.. root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("sequence").GetInt64())];
        Assert.Equal([2, 1], sequences);
    }

    // Tests - Content

    [Fact]
    public void DeletedMessagesAppearWithNullBodyAndFlag() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedDeletedMessage(groupId, ownerUserAccountId, 1);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement item = root.GetProperty("items")[0];
        Assert.True(item.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("body").ValueKind);
    }

    [Fact]
    public void SendersIncludeDistinctAuthorsWithDisplayData() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "from owner");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "from member");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "owner again");

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        List<string> senderIds = [.. root.GetProperty("senders").EnumerateArray().Select(sender => sender.GetProperty("id").GetString()).OrderBy(id => id, StringComparer.Ordinal)];
        List<string> expectedSenderIds = [.. new[] { ownerUserAccountId.ToString(), memberUserAccountId.ToString() }.OrderBy(id => id, StringComparer.Ordinal)];
        Assert.Equal(expectedSenderIds, senderIds);
        foreach (JsonElement sender in root.GetProperty("senders").EnumerateArray())
            Assert.False(string.IsNullOrEmpty(sender.GetProperty("displayName").GetString()));
    }

    [Fact]
    public void MessagesFromDeletedAccountsReturnNullSenderAndNoSenderEntry() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "a message that outlives me");
        testingMockProvidersContainer.WebClient.PostJson("api/userProfile/deleteAccount", new { AuthToken = memberAuthToken, Password = "Seven74!" }).EnsureSuccessStatusCode();

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        JsonElement item = root.GetProperty("items")[0];
        Assert.Equal(JsonValueKind.Null, item.GetProperty("senderUserAccountId").ValueKind);
        Assert.Equal("a message that outlives me", item.GetProperty("body").GetString());
        Assert.Equal(0, root.GetProperty("senders").GetArrayLength());
    }

    [Fact]
    public void ChangeSequenceWatermarkMatchesNewestMessage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 7);

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);

        Assert.Equal(7, root.GetProperty("changeSequence").GetInt64());
    }

    // Tests - Response Shape

    [Fact]
    public void ListPageResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId, null);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["callerUserAccountId", "changeSequence", "items", "nextCursor", "readPointers", "senders", "status", "typing"];
        List<string> actualItemProperties = [.. root.GetProperty("items")[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedItemProperties = ["body", "createdAtUtc", "id", "isDeleted", "kind", "mediaDurationSeconds", "mediaHeight", "mediaUrl", "mediaWidth", "reactions", "senderUserAccountId", "sequence"];
        List<string> actualSenderProperties = [.. root.GetProperty("senders")[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedSenderProperties = ["displayName", "id", "profilePhotoUrl"];

        Assert.Equal(expectedProperties, actualProperties);
        Assert.Equal(expectedItemProperties, actualItemProperties);
        Assert.Equal(expectedSenderProperties, actualSenderProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement ListPage(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string cursor) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = authToken, ChatGroupId = chatGroupId, Cursor = cursor }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).EnsureSuccessStatusCode();
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

    private static void SeedMessages(Guid groupId, Guid senderUserAccountId, int count) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        for (int sequence = 1; sequence <= count; sequence++)
            dbContext.ChatMessages.Add(new ChatMessage { Id = Guid.NewGuid(), ChatGroupId = groupId, SenderUserAccountId = senderUserAccountId, ClientMessageId = Guid.NewGuid(), Kind = ChatMessageKind.Text, BodyCipher = MessageCipher.Encrypt("seeded message " + sequence), CipherVersion = MessageCipher.CurrentVersion, Sequence = sequence, ChangeSequence = sequence, IsDeleted = false, CreatedAtUtc = now });
        dbContext.SaveChanges();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastMessageSequence, (long)count).SetProperty(field => field.LastChangeSequence, (long)count));
    }

    private static void SeedDeletedMessage(Guid groupId, Guid senderUserAccountId, long sequence) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatMessages.Add(new ChatMessage { Id = Guid.NewGuid(), ChatGroupId = groupId, SenderUserAccountId = senderUserAccountId, ClientMessageId = Guid.NewGuid(), Kind = ChatMessageKind.Text, BodyCipher = MessageCipher.Encrypt("deleted message"), CipherVersion = MessageCipher.CurrentVersion, Sequence = sequence, ChangeSequence = sequence, IsDeleted = true, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastMessageSequence, sequence).SetProperty(field => field.LastChangeSequence, sequence));
    }
}
