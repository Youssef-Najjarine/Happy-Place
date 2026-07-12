using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class MarkReadTest {
    // Tests - Authentication Failures

    [Fact]
    public void MarkReadEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), UpToSequence = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MarkReadInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), UpToSequence = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MarkReadMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { ChatGroupId = Guid.NewGuid(), UpToSequence = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = MarkRead(testingMockProvidersContainer, strangerAuthToken, groupId, 1);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = MarkRead(testingMockProvidersContainer, requesterAuthToken, groupId, 1);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 1);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = MarkRead(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), 1);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Pointer Semantics

    [Fact]
    public void MarkReadAdvancesPointer() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 3);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(3, root.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(3, GetPointer(groupId, ownerUserAccountId));
    }

    [Fact]
    public void MarkReadIsMonotonicNeverRegresses() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);
        MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 4);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 2);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(4, root.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(4, GetPointer(groupId, ownerUserAccountId));
    }

    [Fact]
    public void MarkReadClampsToLatestMessage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 999999);

        Assert.Equal(5, root.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(5, GetPointer(groupId, ownerUserAccountId));
    }

    [Fact]
    public void MarkReadNegativeSequenceIsSafeNoop() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, -5);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(0, GetPointer(groupId, ownerUserAccountId));
    }

    [Fact]
    public void MarkReadIdempotentAtSameValue() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 5);
        MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 3);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 3);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal(3, root.GetProperty("lastReadSequence").GetInt64());
    }

    [Fact]
    public void ReadPointersVisibleToOtherMembersViaPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SeedMessages(groupId, ownerUserAccountId, 5);
        MarkRead(testingMockProvidersContainer, memberAuthToken, groupId, 3);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, SinceChangeSequence = 5 }).ReadContentAsJsonDocument().RootElement.Clone();

        Dictionary<string, long> pointers = root.GetProperty("readPointers").EnumerateArray().ToDictionary(pointer => pointer.GetProperty("userAccountId").GetString(), pointer => pointer.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(2, pointers.Count);
        Assert.Equal(3, pointers[memberUserAccountId.ToString()]);
        Assert.Equal(0, pointers[ownerUserAccountId.ToString()]);
    }

    // Tests - Response Shape

    [Fact]
    public void MarkReadResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        SeedMessages(groupId, ownerUserAccountId, 1);

        JsonElement root = MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, 1);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["lastReadSequence", "status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement MarkRead(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long upToSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = authToken, ChatGroupId = chatGroupId, UpToSequence = upToSequence }).ReadContentAsJsonDocument().RootElement.Clone();
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

    // Helpers - Reading

    private static long GetPointer(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Single(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId).LastReadSequence;
    }
}
