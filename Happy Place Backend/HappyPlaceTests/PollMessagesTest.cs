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

    [Fact]
    public void PollChangeEntriesEchoClientMessageId() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid clientMessageId = Guid.NewGuid();
        SendWithClientMessageId(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");

        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        Assert.Equal(clientMessageId.ToString(), root.GetProperty("changes")[0].GetProperty("clientMessageId").GetString());
    }

    // Tests - Group State

    [Fact]
    public void PollReturnsGroupStateWithTitleVisibilityAndMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Support After Loss", false);

        JsonElement group = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group");

        Assert.Equal("Support After Loss", group.GetProperty("title").GetString());
        Assert.False(group.GetProperty("isPublic").GetBoolean());
        Assert.True(ContainsUser(group.GetProperty("members"), ownerUserAccountId));
    }

    [Fact]
    public void GroupStateContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement group = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group");
        List<string> actualGroupProperties = [.. group.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedGroupProperties = ["directContact", "isDirect", "isPublic", "members", "title"];
        JsonElement memberEntry = group.GetProperty("members")[0];
        List<string> actualMemberProperties = [.. memberEntry.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedMemberProperties = ["avatarColor", "isOwner", "name", "profilePhotoUrl", "userAccountId", "username"];

        Assert.Equal(expectedGroupProperties, actualGroupProperties);
        Assert.Equal(expectedMemberProperties, actualMemberProperties);
    }

    [Fact]
    public void GroupStateMembersOrderedByJoinTimeOwnerFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMemberAt(groupId, SeedUser("Member One", null), DateTime.UtcNow.AddMinutes(1));
        AddActiveMemberAt(groupId, SeedUser("Member Two", null), DateTime.UtcNow.AddMinutes(2));

        JsonElement members = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.True(members[0].GetProperty("isOwner").GetBoolean());
        Assert.Equal("Member One", members[1].GetProperty("name").GetString());
        Assert.Equal("Member Two", members[2].GetProperty("name").GetString());
    }

    [Fact]
    public void MemberJoiningAfterEarlierPollAppearsInNextPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        JsonElement firstGroupState = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group");
        Guid joinerUserAccountId = SeedUser("Joiner", null);
        AddActiveMember(groupId, joinerUserAccountId);

        JsonElement secondRoot = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0);

        Assert.Equal(1, firstGroupState.GetProperty("members").GetArrayLength());
        Assert.Equal(2, secondRoot.GetProperty("group").GetProperty("members").GetArrayLength());
        Assert.True(ContainsUser(secondRoot.GetProperty("group").GetProperty("members"), joinerUserAccountId));
        Assert.Equal(0, secondRoot.GetProperty("changes").GetArrayLength());
    }

    [Fact]
    public void PendingRequesterNeverAppearsInGroupState() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        Guid pendingUserAccountId = SeedUser("Pending", null);
        AddPendingMember(groupId, pendingUserAccountId);

        JsonElement members = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.Equal(1, members.GetArrayLength());
        Assert.False(ContainsUser(members, pendingUserAccountId));
    }

    [Fact]
    public void ApprovedRequesterAppearsInGroupStateAfterApproval() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        Guid pendingUserAccountId = SeedUser("Pending", null);
        AddPendingMember(groupId, pendingUserAccountId);

        ApproveMember(testingMockProvidersContainer, ownerAuthToken, groupId, pendingUserAccountId);
        JsonElement members = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.Equal(2, members.GetArrayLength());
        Assert.True(ContainsUser(members, pendingUserAccountId));
    }

    [Fact]
    public void RemovedMemberDisappearsFromGroupState() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid memberUserAccountId = SeedUser("Member", null);
        AddActiveMember(groupId, memberUserAccountId);

        RemoveMember(testingMockProvidersContainer, ownerAuthToken, groupId, memberUserAccountId);
        JsonElement members = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.Equal(1, members.GetArrayLength());
        Assert.False(ContainsUser(members, memberUserAccountId));
    }

    [Fact]
    public void LeftMemberDisappearsFromGroupState() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);

        Leave(testingMockProvidersContainer, memberAuthToken, groupId);
        JsonElement members = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.Equal(1, members.GetArrayLength());
        Assert.False(ContainsUser(members, memberUserAccountId));
    }

    [Fact]
    public void RenameReflectsInGroupStateForOtherMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Original Title", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        Rename(testingMockProvidersContainer, ownerAuthToken, groupId, "A Better Title");
        JsonElement group = Poll(testingMockProvidersContainer, memberAuthToken, groupId, 0).GetProperty("group");

        Assert.Equal("A Better Title", group.GetProperty("title").GetString());
    }

    [Fact]
    public void VisibilityChangeReflectsInGroupState() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        SetVisibility(testingMockProvidersContainer, ownerAuthToken, groupId, false);
        JsonElement group = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("group");

        Assert.False(group.GetProperty("isPublic").GetBoolean());
    }

    [Fact]
    public void OwnershipTransferReflectsInGroupState() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMemberAt(groupId, memberUserAccountId, DateTime.UtcNow.AddMinutes(1));

        Leave(testingMockProvidersContainer, ownerAuthToken, groupId);
        JsonElement members = Poll(testingMockProvidersContainer, memberAuthToken, groupId, 0).GetProperty("group").GetProperty("members");

        Assert.False(ContainsUser(members, ownerUserAccountId));
        Assert.True(GetEntry(members, memberUserAccountId).GetProperty("isOwner").GetBoolean());
    }

    [Fact]
    public void GroupIsNullWhenCallerIsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Poll(testingMockProvidersContainer, strangerAuthToken, groupId, 0);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("group").ValueKind);
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
        List<string> expectedProperties = ["changeSequence", "changes", "group", "readPointers", "senders", "status", "typing"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void PollChangeEntryContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement entry = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("changes")[0];
        List<string> actualEntryProperties = [.. entry.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedEntryProperties = ["body", "clientMessageId", "createdAtUtc", "id", "isDeleted", "kind", "mediaDurationSeconds", "mediaHeight", "mediaUrl", "mediaWidth", "reactions", "replyTo", "senderUserAccountId", "sequence"];

        Assert.Equal(expectedEntryProperties, actualEntryProperties);
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

    private static void SendWithClientMessageId(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).EnsureSuccessStatusCode();
    }

    private static void Rename(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string name) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/rename", new { AuthToken = authToken, ChatGroupId = chatGroupId, Name = name }).EnsureSuccessStatusCode();
    }

    private static void SetVisibility(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, bool isPublic) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = authToken, ChatGroupId = chatGroupId, IsPublic = isPublic }).EnsureSuccessStatusCode();
    }

    private static void ApproveMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void RemoveMember(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/removeMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void Leave(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
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

    private static void AddActiveMemberAt(Guid groupId, Guid userAccountId, DateTime joinedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = joinedAtUtc });
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

    // Helpers - Reading

    private static bool ContainsUser(JsonElement arrayElement, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("userAccountId").GetString() == target)
                return true;
        return false;
    }

    private static JsonElement GetEntry(JsonElement arrayElement, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement entry in arrayElement.EnumerateArray())
            if (entry.GetProperty("userAccountId").GetString() == target)
                return entry;
        throw new InvalidOperationException("Member was not present in the response.");
    }
}
