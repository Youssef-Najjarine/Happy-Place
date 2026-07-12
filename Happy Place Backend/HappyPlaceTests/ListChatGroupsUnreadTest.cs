using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListChatGroupsUnreadTest {
    // Tests - Counting

    [Fact]
    public void NoMessagesShowsZeroUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(0, group.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void OthersMessagesCountAsUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "two");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "three");

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(3, group.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void OwnMessagesAreExcluded() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "two");

        JsonElement ownerGroup = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);
        JsonElement memberGroup = GetGroup(List(testingMockProvidersContainer, memberAuthToken), groupId);

        Assert.Equal(0, ownerGroup.GetProperty("unreadCount").GetInt32());
        Assert.Equal(2, memberGroup.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void DeletedMessagesAreExcluded() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        (Guid firstMessageId, long _) = Send(testingMockProvidersContainer, memberAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "two");
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = memberAuthToken, ChatGroupId = groupId, MessageId = firstMessageId }).EnsureSuccessStatusCode();

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(1, group.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void NewMemberBacklogCountsAsUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "two");
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));

        JsonElement group = GetGroup(List(testingMockProvidersContainer, memberAuthToken), groupId);

        Assert.Equal(2, group.GetProperty("unreadCount").GetInt32());
    }

    // Tests - Read Resets

    [Fact]
    public void MarkReadResetsUnreadToZero() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "one");
        (Guid _, long newestSequence) = Send(testingMockProvidersContainer, memberAuthToken, groupId, "two");
        MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, newestSequence);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(0, group.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void PartialMarkReadCountsRemainder() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "one");
        (Guid _, long secondSequence) = Send(testingMockProvidersContainer, memberAuthToken, groupId, "two");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "three");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "four");
        MarkRead(testingMockProvidersContainer, ownerAuthToken, groupId, secondSequence);

        JsonElement group = GetGroup(List(testingMockProvidersContainer, ownerAuthToken), groupId);

        Assert.Equal(2, group.GetProperty("unreadCount").GetInt32());
    }

    // Tests - Isolation

    [Fact]
    public void GroupsCountIndependently() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid firstGroupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Second Group", true);
        AddActiveMember(firstGroupId, memberUserAccountId);
        AddActiveMember(secondGroupId, memberUserAccountId);
        Send(testingMockProvidersContainer, memberAuthToken, firstGroupId, "one");
        Send(testingMockProvidersContainer, memberAuthToken, secondGroupId, "one");
        Send(testingMockProvidersContainer, memberAuthToken, secondGroupId, "two");

        JsonElement list = List(testingMockProvidersContainer, ownerAuthToken);

        Assert.Equal(1, GetGroup(list, firstGroupId).GetProperty("unreadCount").GetInt32());
        Assert.Equal(2, GetGroup(list, secondGroupId).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void NonMemberSeesZeroOnPublicGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "two");

        JsonElement group = GetGroup(List(testingMockProvidersContainer, strangerAuthToken), groupId);

        Assert.Equal(0, group.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void PendingMemberSeesZeroUnread() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string pendingAuthToken = CreateUser(testingMockProvidersContainer, "Pending");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddMember(groupId, ResolveUserAccountId(pendingAuthToken), ChatGroupMemberStatus.Pending);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "one");

        JsonElement group = GetGroup(List(testingMockProvidersContainer, pendingAuthToken), groupId);

        Assert.Equal(0, group.GetProperty("unreadCount").GetInt32());
    }

    // Tests - Paged Endpoint

    [Fact]
    public void ListPageCarriesUnreadCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "one");
        Send(testingMockProvidersContainer, memberAuthToken, groupId, "two");

        JsonElement page = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/listPage", new { AuthToken = ownerAuthToken }).ReadContentAsJsonDocument().RootElement.Clone();
        JsonElement group = page.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("id").GetString() == groupId.ToString());

        Assert.Equal(2, group.GetProperty("unreadCount").GetInt32());
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static (Guid MessageId, long Sequence) Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).ReadContentAsJsonDocument().RootElement;
        JsonElement message = root.GetProperty("message");
        return (Guid.Parse(message.GetProperty("id").GetString()), message.GetProperty("sequence").GetInt64());
    }

    private static void MarkRead(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long upToSequence) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = authToken, ChatGroupId = chatGroupId, UpToSequence = upToSequence }).EnsureSuccessStatusCode();
    }

    private static JsonElement List(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement GetGroup(JsonElement list, Guid groupId) {
        return list.EnumerateArray().Single(group => group.GetProperty("id").GetString() == groupId.ToString());
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
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Active);
    }

    private static void AddMember(Guid groupId, Guid userAccountId, ChatGroupMemberStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }
}
