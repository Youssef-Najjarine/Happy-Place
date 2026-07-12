using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class PollMessagesTest {
    // Tests - Authentication Failures

    [Fact]
    public void PollEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), SinceChangeSequence = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), SinceChangeSequence = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void PollMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { ChatGroupId = Guid.NewGuid(), SinceChangeSequence = 0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Poll(testingMockProvidersContainer, strangerAuthToken, groupId, 0);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = Poll(testingMockProvidersContainer, requesterAuthToken, groupId, 0);

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

        JsonElement root = Poll(testingMockProvidersContainer, memberAuthToken, groupId, 0);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = Poll(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), 0);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Deltas

    [Fact]
    public void PollFromZeroReturnsAllMessagesAscending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 3);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        List<long> sequences = [.. root.GetProperty("changes").EnumerateArray().Select(change => change.GetProperty("sequence").GetInt64())];
        Assert.Equal([1, 2, 3], sequences);
        Assert.Equal(3, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void PollSinceWatermarkReturnsOnlyNewer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 3);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "fourth");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "fifth");

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 3);

        List<long> sequences = [.. root.GetProperty("changes").EnumerateArray().Select(change => change.GetProperty("sequence").GetInt64())];
        Assert.Equal([4, 5], sequences);
        Assert.Equal(5, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void EmptyDeltaEchoesSinceWatermark() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 3);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 3);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("changes").GetArrayLength());
        Assert.Equal(3, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void PollPicksUpMessagesSentAfterListPage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        SeedMessages(groupId, ownerUserAccountId, 3);
        JsonElement listRoot = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();
        long watermark = listRoot.GetProperty("changeSequence").GetInt64();
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "sent after the page load");

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, watermark);

        Assert.Equal(1, root.GetProperty("changes").GetArrayLength());
        Assert.Equal("sent after the page load", root.GetProperty("changes")[0].GetProperty("body").GetString());
        Assert.Equal(watermark + 1, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void CapLimitsChangesAndWatermarkAllowsResume() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 250);

        JsonElement firstRoot = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);
        long firstWatermark = firstRoot.GetProperty("changeSequence").GetInt64();
        JsonElement secondRoot = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, firstWatermark);

        Assert.Equal(200, firstRoot.GetProperty("changes").GetArrayLength());
        Assert.Equal(200, firstWatermark);
        Assert.Equal(50, secondRoot.GetProperty("changes").GetArrayLength());
        Assert.Equal(250, secondRoot.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void DeletedMessageRowsSurfaceInChangesWithFlag() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedDeletedMessage(groupId, ownerUserAccountId, 1);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        JsonElement change = root.GetProperty("changes")[0];
        Assert.True(change.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, change.GetProperty("body").ValueKind);
    }

    [Fact]
    public void SendersAccompanyChanges() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "hello");

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        Assert.Equal(1, root.GetProperty("senders").GetArrayLength());
        Assert.Equal(memberUserAccountId.ToString(), root.GetProperty("senders")[0].GetProperty("id").GetString());
    }

    // Tests - Response Shape

    [Fact]
    public void PollResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["changeSequence", "changes", "readPointers", "senders", "status", "typing"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long sinceChangeSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = sinceChangeSequence }).ReadContentAsJsonDocument().RootElement.Clone();
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
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
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
