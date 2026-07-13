using System.Net;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SearchUsersTest {
    // Tests - Matching And Ranking

    [Fact]
    public void UsernamePrefixMatchesRankAboveDisplayNameMatches() {
        using var container = new TestingMockProvidersContainer();
        string marker = "q" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string usernameMatchAuthToken = FriendshipTestActions.CreateUser(container, "Plain Person");
        string usernameMatch = marker + "a1";
        SetProfile(container, usernameMatchAuthToken, usernameMatch, "Plain Person");
        FriendshipTestActions.CreateUser(container, "Display " + marker);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        List<string> usernames = ReadUsernames(response);
        Assert.Equal(2, usernames.Count);
        Assert.Equal(usernameMatch, usernames[0]);
    }

    [Fact]
    public void DisplayNameContainsMatchesMidWord() {
        using var container = new TestingMockProvidersContainer();
        string marker = "m" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Middle" + marker + "Name");

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(targetAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    [Fact]
    public void QueryMatchingIsCaseInsensitive() {
        using var container = new TestingMockProvidersContainer();
        string marker = "c" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Plain Person");
        string targetUsername = marker + "a1";
        SetProfile(container, targetAuthToken, targetUsername, "Plain Person");

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker.ToUpperInvariant());

        Assert.Contains(targetUsername, ReadUsernames(response));
    }

    // Tests - Exclusions

    [Fact]
    public void SelfIsExcludedFromResults() {
        using var container = new TestingMockProvidersContainer();
        string marker = "s" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Self " + marker);
        string otherAuthToken = FriendshipTestActions.CreateUser(container, "Other " + marker);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        List<string> usernames = ReadUsernames(response);
        Assert.Contains(FriendshipTestActions.ResolveUsername(otherAuthToken), usernames);
        Assert.DoesNotContain(FriendshipTestActions.ResolveUsername(callerAuthToken), usernames);
    }

    [Fact]
    public void GuestsAreExcluded() {
        using var container = new TestingMockProvidersContainer();
        string marker = "g" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string visibleAuthToken = FriendshipTestActions.CreateUser(container, "Visible " + marker);
        string guestAuthToken = FriendshipTestActions.CreateUser(container, "Ghost " + marker);
        string guestUsername = FriendshipTestActions.ResolveUsername(guestAuthToken);
        FriendshipTestActions.MakeAnonymous(guestAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        List<string> usernames = ReadUsernames(response);
        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(visibleAuthToken)];
        Assert.Equal(expectedUsernames, usernames);
        Assert.DoesNotContain(guestUsername, usernames);
    }

    [Fact]
    public void BlockRelationsAreExcludedInBothDirections() {
        using var container = new TestingMockProvidersContainer();
        string callerMarker = "a" + Guid.NewGuid().ToString("N")[..8];
        string targetMarker = "b" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller " + callerMarker);
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked " + targetMarker);
        FriendshipTestActions.Block(container, callerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage callerResponse = FriendshipTestActions.SearchUsers(container, callerAuthToken, targetMarker);
        HttpResponseMessage blockedResponse = FriendshipTestActions.SearchUsers(container, blockedAuthToken, callerMarker);

        Assert.Empty(ReadUsernames(callerResponse));
        Assert.Empty(ReadUsernames(blockedResponse));
    }

    [Fact]
    public void UsersWithoutUsernamesAreExcluded() {
        using var container = new TestingMockProvidersContainer();
        string marker = "n" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string completeAuthToken = FriendshipTestActions.CreateUser(container, "Complete " + marker);
        string incompleteAuthToken = FriendshipTestActions.CreateUser(container, "Incomplete " + marker);
        ClearUsername(incompleteAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(completeAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    // Tests - Row Content

    [Fact]
    public void RowsCarryFriendshipStatusPerRow() {
        using var container = new TestingMockProvidersContainer();
        string marker = "r" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string friendAuthToken = FriendshipTestActions.CreateUser(container, "Friend " + marker);
        string sentToAuthToken = FriendshipTestActions.CreateUser(container, "Sent " + marker);
        string receivedFromAuthToken = FriendshipTestActions.CreateUser(container, "Received " + marker);
        FriendshipTestActions.MakeFriends(container, callerAuthToken, friendAuthToken);
        FriendshipTestActions.SendRequest(container, callerAuthToken, FriendshipTestActions.ResolveUsername(sentToAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, receivedFromAuthToken, FriendshipTestActions.ResolveUsername(callerAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        var rows = ReadRows(response);
        Assert.Equal(3, rows.Count);
        Assert.Equal("friends", rows[FriendshipTestActions.ResolveUsername(friendAuthToken)]);
        Assert.Equal("requestSent", rows[FriendshipTestActions.ResolveUsername(sentToAuthToken)]);
        Assert.Equal("requestReceived", rows[FriendshipTestActions.ResolveUsername(receivedFromAuthToken)]);
    }

    [Fact]
    public void ResultsAreCappedAtTwenty() {
        using var container = new TestingMockProvidersContainer();
        string marker = "k" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        for (int index = 0; index < 25; index++)
            FriendshipTestActions.CreateUser(container, "Cap " + marker);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        Assert.Equal(20, ReadUsernames(response).Count);
    }

    [Fact]
    public void ResponseContainsExactlyExpectedProperties() {
        using var container = new TestingMockProvidersContainer();
        string marker = "p" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.CreateUser(container, "Person " + marker);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, marker);

        var root = response.ReadContentAsJsonDocument().RootElement;
        List<string> rootProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRootProperties = ["users"];
        Assert.Equal(expectedRootProperties, rootProperties);
        var firstRow = root.GetProperty("users").EnumerateArray().Single();
        List<string> rowProperties = [.. firstRow.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRowProperties = ["avatarColor", "displayName", "friendshipStatus", "profilePhotoUrl", "username"];
        Assert.Equal(expectedRowProperties, rowProperties);
    }

    // Tests - Suggestions

    [Fact]
    public void EmptyQueryReturnsActiveGroupCoMembers() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string coMemberAuthToken = FriendshipTestActions.CreateUser(container, "Co Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(coMemberAuthToken));

        HttpResponseMessage emptyQueryResponse = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");
        HttpResponseMessage whitespaceQueryResponse = FriendshipTestActions.SearchUsers(container, callerAuthToken, "   ");

        var emptyQueryRows = ReadRows(emptyQueryResponse);
        Assert.Equal("none", emptyQueryRows[FriendshipTestActions.ResolveUsername(coMemberAuthToken)]);
        Assert.Equal(ReadUsernames(emptyQueryResponse), ReadUsernames(whitespaceQueryResponse));
    }

    [Fact]
    public void SuggestionsExcludeExistingFriendsAndPendings() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string friendAuthToken = FriendshipTestActions.CreateUser(container, "Friend Member");
        string sentToAuthToken = FriendshipTestActions.CreateUser(container, "Sent Member");
        string receivedFromAuthToken = FriendshipTestActions.CreateUser(container, "Received Member");
        string plainAuthToken = FriendshipTestActions.CreateUser(container, "Plain Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(friendAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(sentToAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(receivedFromAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(plainAuthToken));
        FriendshipTestActions.MakeFriends(container, callerAuthToken, friendAuthToken);
        FriendshipTestActions.SendRequest(container, callerAuthToken, FriendshipTestActions.ResolveUsername(sentToAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, receivedFromAuthToken, FriendshipTestActions.ResolveUsername(callerAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(plainAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    [Fact]
    public void SuggestionsExcludeBlockRelations() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked Member");
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker Member");
        string plainAuthToken = FriendshipTestActions.CreateUser(container, "Plain Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(blockedAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(blockerAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(plainAuthToken));
        FriendshipTestActions.Block(container, callerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.Block(container, blockerAuthToken, FriendshipTestActions.ResolveUsername(callerAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(plainAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    [Fact]
    public void SuggestionsExcludeGuestCoMembers() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string guestAuthToken = FriendshipTestActions.CreateUser(container, "Guest Member");
        string plainAuthToken = FriendshipTestActions.CreateUser(container, "Plain Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(guestAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(plainAuthToken));
        FriendshipTestActions.MakeAnonymous(guestAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(plainAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    [Fact]
    public void SuggestionsExcludeUsersWithoutUsernames() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string incompleteAuthToken = FriendshipTestActions.CreateUser(container, "Incomplete Member");
        string plainAuthToken = FriendshipTestActions.CreateUser(container, "Plain Member");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(incompleteAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(plainAuthToken));
        ClearUsername(incompleteAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(plainAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    [Fact]
    public void SuggestionsExcludeUsersWithoutSharedActiveGroup() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string coMemberAuthToken = FriendshipTestActions.CreateUser(container, "Co Member");
        string strangerAuthToken = FriendshipTestActions.CreateUser(container, "Stranger");
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(coMemberAuthToken));

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> usernames = ReadUsernames(response);
        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(coMemberAuthToken)];
        Assert.Equal(expectedUsernames, usernames);
        Assert.DoesNotContain(FriendshipTestActions.ResolveUsername(strangerAuthToken), usernames);
    }

    [Fact]
    public void SuggestionsOrderedByDisplayNameAscending() {
        using var container = new TestingMockProvidersContainer();
        string marker = Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string thirdAuthToken = FriendshipTestActions.CreateUser(container, "Cc " + marker);
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "Aa " + marker);
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Bb " + marker);
        Guid groupId = CreateActiveGroup(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), "Suggestion Group", false);
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(thirdAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(firstAuthToken));
        AddActiveMember(groupId, FriendshipTestActions.ResolveUserAccountId(secondAuthToken));

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, callerAuthToken, "");

        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(firstAuthToken), FriendshipTestActions.ResolveUsername(secondAuthToken), FriendshipTestActions.ResolveUsername(thirdAuthToken)];
        Assert.Equal(expectedUsernames, ReadUsernames(response));
    }

    // Tests - Authentication

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, "", "anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.SearchUsers(container, "invalid-token", "anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/searchUsers", new { Query = "anything" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Methods - Helpers

    private static void SetProfile(TestingMockProvidersContainer container, string authToken, string username, string displayName) {
        container.WebClient.PostJson("api/userProfile/updateProfile", new { AuthToken = authToken, Username = username, DisplayName = displayName, Bio = "" }).EnsureSuccessStatusCode();
    }

    private static void ClearUsername(string authToken) {
        Guid userAccountId = FriendshipTestActions.ResolveUserAccountId(authToken);
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts.Where(field => field.Id == userAccountId).ExecuteUpdate(setters => setters.SetProperty(field => field.Username, (string)null));
    }

    private static List<string> ReadUsernames(HttpResponseMessage response) {
        return [.. response.ReadContentAsJsonDocument().RootElement.GetProperty("users").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
    }

    private static Dictionary<string, string> ReadRows(HttpResponseMessage response) {
        return response.ReadContentAsJsonDocument().RootElement.GetProperty("users").EnumerateArray().ToDictionary(row => row.GetProperty("username").GetString(), row => row.GetProperty("friendshipStatus").GetString());
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
}
