using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class TypingTest {
    // Tests - Authentication Failures

    [Fact]
    public void TypingEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { AuthToken = "", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TypingInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void TypingMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { ChatGroupId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Typing(testingMockProvidersContainer, strangerAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void PendingMemberReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = Typing(testingMockProvidersContainer, requesterAuthToken, groupId);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = Typing(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Typing Visibility

    [Fact]
    public void TypingMemberAppearsInOthersPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Typing(testingMockProvidersContainer, memberAuthToken, groupId);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId);

        List<string> typing = [.. root.GetProperty("typing").EnumerateArray().Select(entry => entry.GetString())];
        Assert.Equal([memberUserAccountId.ToString()], typing);
    }

    [Fact]
    public void CallerNeverSeesOwnTyping() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Typing(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, root.GetProperty("typing").GetArrayLength());
    }

    [Fact]
    public void TypingExpiresAfterWindow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SetTypingStamp(groupId, memberUserAccountId, DateTime.UtcNow.AddSeconds(-10));

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(0, root.GetProperty("typing").GetArrayLength());
    }

    [Fact]
    public void RecentStampWithinWindowStillShows() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        SetTypingStamp(groupId, memberUserAccountId, DateTime.UtcNow.AddSeconds(-3));

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId);

        List<string> typing = [.. root.GetProperty("typing").EnumerateArray().Select(entry => entry.GetString())];
        Assert.Equal([memberUserAccountId.ToString()], typing);
    }

    [Fact]
    public void MultipleTypersAllAppearSorted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string firstMemberAuthToken = CreateUser(testingMockProvidersContainer, "First Member");
        string secondMemberAuthToken = CreateUser(testingMockProvidersContainer, "Second Member");
        Guid firstMemberUserAccountId = ResolveUserAccountId(firstMemberAuthToken);
        Guid secondMemberUserAccountId = ResolveUserAccountId(secondMemberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, firstMemberUserAccountId);
        AddActiveMember(groupId, secondMemberUserAccountId);
        Typing(testingMockProvidersContainer, firstMemberAuthToken, groupId);
        Typing(testingMockProvidersContainer, secondMemberAuthToken, groupId);

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId);

        List<string> typing = [.. root.GetProperty("typing").EnumerateArray().Select(entry => entry.GetString())];
        List<string> expectedTyping = [.. new[] { firstMemberUserAccountId, secondMemberUserAccountId }.OrderBy(id => id).Select(id => id.ToString())];
        Assert.Equal(expectedTyping, typing);
    }

    [Fact]
    public void ListPageIncludesReadPointersAndTyping() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = memberAuthToken, ChatGroupId = groupId, UpToSequence = 1 }).EnsureSuccessStatusCode();
        Typing(testingMockProvidersContainer, memberAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        Dictionary<string, long> pointers = root.GetProperty("readPointers").EnumerateArray().ToDictionary(pointer => pointer.GetProperty("userAccountId").GetString(), pointer => pointer.GetProperty("lastReadSequence").GetInt64());
        Assert.Equal(1, pointers[memberUserAccountId.ToString()]);
        Assert.Equal(0, pointers[ownerUserAccountId.ToString()]);
        List<string> typing = [.. root.GetProperty("typing").EnumerateArray().Select(entry => entry.GetString())];
        Assert.Equal([memberUserAccountId.ToString()], typing);
    }

    // Tests - Response Shape

    [Fact]
    public void TypingResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Typing(testingMockProvidersContainer, ownerAuthToken, groupId);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Typing(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = 0 }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static void SetTypingStamp(Guid groupId, Guid userAccountId, DateTime lastTypingAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.LastTypingAtUtc, (DateTime?)lastTypingAtUtc));
    }
}
