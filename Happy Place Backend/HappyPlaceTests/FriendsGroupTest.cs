using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class FriendsGroupTest {
    // Fields

    private static readonly string[] PlaceholderUsernames = ["someone"];

    // Tests - Authentication Failures

    [Fact]
    public void CreateWithFriendsEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/createWithFriends", new { AuthToken = "", Name = "Friends", Usernames = PlaceholderUsernames });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void CreateWithFriendsInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/createWithFriends", new { AuthToken = "not-a-real-token", Name = "Friends", Usernames = PlaceholderUsernames });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Creation

    [Fact]
    public void CreateWithFriendsCreatesAPrivateActiveGroupOwnedByTheCaller() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");
        string firstFriendAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "First Friend");
        string secondFriendAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Second Friend");
        FriendshipTestActions.MakeFriends(testingMockProvidersContainer, callerAuthToken, firstFriendAuthToken);
        FriendshipTestActions.MakeFriends(testingMockProvidersContainer, callerAuthToken, secondFriendAuthToken);
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        Guid firstFriendUserAccountId = FriendshipTestActions.ResolveUserAccountId(firstFriendAuthToken);
        Guid secondFriendUserAccountId = FriendshipTestActions.ResolveUserAccountId(secondFriendAuthToken);
        string groupName = "Weekend Plans " + Guid.NewGuid();

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, callerAuthToken, groupName, [FriendshipTestActions.ResolveUsername(firstFriendAuthToken), FriendshipTestActions.ResolveUsername(secondFriendAuthToken)]);

        Assert.Equal("created", root.GetProperty("status").GetString());
        Guid chatGroupId = Guid.Parse(root.GetProperty("chatGroupId").GetString());
        ChatGroup chatGroup = LoadGroup(chatGroupId);
        Assert.Equal(groupName, chatGroup.Name);
        Assert.False(chatGroup.IsPublic);
        Assert.Equal(callerUserAccountId, chatGroup.OwnerUserAccountId);
        Assert.Equal(ChatGroupStatus.Active, chatGroup.Status);
        Assert.Null(chatGroup.DirectPairLowId);
        List<ChatGroupMember> memberRows = LoadMemberRows(chatGroupId);
        Assert.Equal(3, memberRows.Count);
        Assert.All(memberRows, member => Assert.Equal(ChatGroupMemberStatus.Active, member.Status));
        Assert.Equal(ChatGroupMemberRole.Owner, memberRows.Single(member => member.UserAccountId == callerUserAccountId).MemberRole);
        Assert.Equal(ChatGroupMemberRole.Member, memberRows.Single(member => member.UserAccountId == firstFriendUserAccountId).MemberRole);
        Assert.Equal(ChatGroupMemberRole.Member, memberRows.Single(member => member.UserAccountId == secondFriendUserAccountId).MemberRole);
    }

    [Fact]
    public void CreateWithFriendsResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Shape Pin Group", [pair.AddresseeUsername]);

        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["chatGroupId", "status"];
        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void EveryMemberSeesTheNewGroupInTheirFeed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");
        string friendAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Friend");
        FriendshipTestActions.MakeFriends(testingMockProvidersContainer, callerAuthToken, friendAuthToken);
        string groupName = "Feed Group " + Guid.NewGuid();
        Guid chatGroupId = Guid.Parse(CreateWithFriends(testingMockProvidersContainer, callerAuthToken, groupName, [FriendshipTestActions.ResolveUsername(friendAuthToken)]).GetProperty("chatGroupId").GetString());

        JsonElement callerRow = GetGroup(List(testingMockProvidersContainer, callerAuthToken), chatGroupId);
        JsonElement friendRow = GetGroup(List(testingMockProvidersContainer, friendAuthToken), chatGroupId);

        Assert.Equal(groupName, callerRow.GetProperty("title").GetString());
        Assert.False(callerRow.GetProperty("isDirect").GetBoolean());
        Assert.True(callerRow.GetProperty("owner").GetBoolean());
        Assert.True(friendRow.GetProperty("joined").GetBoolean());
        Assert.False(friendRow.GetProperty("owner").GetBoolean());
        Assert.Equal(2, friendRow.GetProperty("memberCount").GetInt32());
    }

    [Fact]
    public void MessagesFlowInTheNewGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        Guid chatGroupId = Guid.Parse(CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Chatty Group", [pair.AddresseeUsername]).GetProperty("chatGroupId").GetString());

        JsonElement sentRoot = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = pair.AddresseeAuthToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = "first group message" }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("sent", sentRoot.GetProperty("status").GetString());
        Assert.Equal(1, GetGroup(List(testingMockProvidersContainer, pair.RequesterAuthToken), chatGroupId).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public void NameIsTruncatedAtTheCap() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        string longName = new('x', 150);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, longName, [pair.AddresseeUsername]);

        Assert.Equal("created", root.GetProperty("status").GetString());
        Assert.Equal(100, LoadGroup(Guid.Parse(root.GetProperty("chatGroupId").GetString())).Name.Length);
    }

    [Fact]
    public void DuplicateUsernamesCollapseToOneMembership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Duplicate Group", [pair.AddresseeUsername, pair.AddresseeUsername]);

        Assert.Equal("created", root.GetProperty("status").GetString());
        Assert.Equal(2, LoadMemberRows(Guid.Parse(root.GetProperty("chatGroupId").GetString())).Count);
    }

    // Tests - Refusals

    [Fact]
    public void AnonymousCallerReturnsAccountRequired() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        FriendshipTestActions.MakeAnonymous(pair.RequesterAuthToken);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Guest Group", [pair.AddresseeUsername]);

        Assert.Equal("accountRequired", root.GetProperty("status").GetString());
    }

    [Fact]
    public void EmptyNameReturnsInvalidName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "   ", [pair.AddresseeUsername]);

        Assert.Equal("invalidName", root.GetProperty("status").GetString());
    }

    [Fact]
    public void NonFriendMemberReturnsNotFriendsWithoutCreating() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        string strangerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Stranger");
        string groupName = "Never Created " + Guid.NewGuid();

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, groupName, [pair.AddresseeUsername, FriendshipTestActions.ResolveUsername(strangerAuthToken)]);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("chatGroupId").ValueKind);
        Assert.Equal(0, CountGroupsNamed(groupName));
    }

    [Fact]
    public void PendingRequestMemberReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pendingPair.RequesterAuthToken, "Pending Group", [pendingPair.AddresseeUsername]);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void BlockedMemberReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);
        FriendshipTestActions.Block(testingMockProvidersContainer, pair.RequesterAuthToken, pair.AddresseeUsername).EnsureSuccessStatusCode();

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Blocked Group", [pair.AddresseeUsername]);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void SelfInTheListReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Self Group", [pair.RequesterUsername, pair.AddresseeUsername]);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownUsernameReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        FriendshipPair pair = FriendshipTestActions.CreateFriends(testingMockProvidersContainer);

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, pair.RequesterAuthToken, "Ghost Group", ["no-such-user-" + Guid.NewGuid()]);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void EmptyUsernamesReturnsNotFriends() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, callerAuthToken, "Empty Group", []);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    [Fact]
    public void MemberCapIsEnforced() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, "Caller");
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        DateTime respondedAtUtc = DateTime.UtcNow;
        List<string> friendUsernames = [];
        for (int friendIndex = 0; friendIndex < 21; friendIndex++) {
            string friendAuthToken = FriendshipTestActions.CreateUser(testingMockProvidersContainer, $"Friend {friendIndex}");
            FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(friendAuthToken), respondedAtUtc);
            friendUsernames.Add(FriendshipTestActions.ResolveUsername(friendAuthToken));
        }

        JsonElement root = CreateWithFriends(testingMockProvidersContainer, callerAuthToken, "Oversized Group", friendUsernames);

        Assert.Equal("notFriends", root.GetProperty("status").GetString());
    }

    // Helpers - Acting

    private static JsonElement CreateWithFriends(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, string name, List<string> usernames) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/createWithFriends", new { AuthToken = authToken, Name = name, Usernames = usernames }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement List(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    // Helpers - Reading

    private static ChatGroup LoadGroup(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId);
    }

    private static List<ChatGroupMember> LoadMemberRows(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return [.. dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroupId)];
    }

    private static int CountGroupsNamed(string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Count(field => field.Name == name);
    }

    private static JsonElement GetGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Chat group was not present in the response.");
    }
}
