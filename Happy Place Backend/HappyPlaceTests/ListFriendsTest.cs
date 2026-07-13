using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListFriendsTest {
    // Tests - Own List

    [Fact]
    public void OwnListReturnsFriendsNewestFirst() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First Friend");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second Friend");
        string thirdAuthToken = FriendshipTestActions.CreateUser(container, "Third Friend");
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        DateTime baseTime = DateTime.UtcNow.AddHours(-1);
        FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(firstAuthToken), baseTime.AddMinutes(-1));
        FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(secondAuthToken), baseTime.AddMinutes(-2));
        FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(thirdAuthToken), baseTime.AddMinutes(-3));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = response.ReadContentAsJsonDocument().RootElement;
        Assert.Equal("ok", root.GetProperty("status").GetString());
        List<string> usernames = [.. root.GetProperty("friends").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(firstAuthToken), FriendshipTestActions.ResolveUsername(secondAuthToken), FriendshipTestActions.ResolveUsername(thirdAuthToken)];
        Assert.Equal(expectedUsernames, usernames);
    }

    [Fact]
    public void TotalCountMatchesProfileFriendCount() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "First Friend"));
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Second Friend"));

        HttpResponseMessage listResponse = FriendshipTestActions.ListFriends(container, callerAuthToken);
        HttpResponseMessage profileResponse = ProfileTestActions.GetMyProfile(container, callerAuthToken);

        int totalCount = listResponse.ReadContentAsJsonDocument().RootElement.GetProperty("totalCount").GetInt32();
        int profileFriendCount = profileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("friendCount").GetInt32();
        Assert.Equal(2, totalCount);
        Assert.Equal(profileFriendCount, totalCount);
    }

    [Fact]
    public void ViewingOwnListByUsernameMatchesDefaultMode() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Only Friend"));

        HttpResponseMessage defaultResponse = FriendshipTestActions.ListFriends(container, callerAuthToken);
        HttpResponseMessage byUsernameResponse = FriendshipTestActions.ListFriends(container, callerAuthToken, FriendshipTestActions.ResolveUsername(callerAuthToken));

        Assert.Equal(HttpStatusCode.OK, byUsernameResponse.StatusCode);
        Assert.Equal(FriendshipTestActions.ReadBody(defaultResponse), FriendshipTestActions.ReadBody(byUsernameResponse));
    }

    // Tests - Row Shape

    [Fact]
    public void ResponseContainsExactlyExpectedProperties() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Only Friend"));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken);

        var root = response.ReadContentAsJsonDocument().RootElement;
        List<string> rootProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRootProperties = ["friends", "nextCursor", "status", "totalCount"];
        Assert.Equal(expectedRootProperties, rootProperties);
        var firstRow = root.GetProperty("friends").EnumerateArray().Single();
        List<string> rowProperties = [.. firstRow.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRowProperties = ["avatarColor", "displayName", "friendshipStatus", "profilePhotoUrl", "username"];
        Assert.Equal(expectedRowProperties, rowProperties);
    }

    // Tests - Pagination

    [Fact]
    public void PaginationWalksAllFriendsWithoutDuplicatesOrGaps() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        DateTime baseTime = DateTime.UtcNow.AddDays(-1);
        List<string> expectedUsernames = [];
        for (int index = 0; index < 32; index++) {
            string friendAuthToken = FriendshipTestActions.CreateUser(container, "Walk Friend");
            FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(friendAuthToken), baseTime.AddMinutes(-index));
            expectedUsernames.Add(FriendshipTestActions.ResolveUsername(friendAuthToken));
        }

        var firstPage = FriendshipTestActions.ListFriends(container, callerAuthToken).ReadContentAsJsonDocument().RootElement;
        string nextCursor = firstPage.GetProperty("nextCursor").GetString();
        var secondPage = FriendshipTestActions.ListFriends(container, callerAuthToken, cursor: nextCursor).ReadContentAsJsonDocument().RootElement;

        Assert.Equal(32, firstPage.GetProperty("totalCount").GetInt32());
        List<string> firstPageUsernames = [.. firstPage.GetProperty("friends").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        List<string> secondPageUsernames = [.. secondPage.GetProperty("friends").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        Assert.Equal(30, firstPageUsernames.Count);
        Assert.NotNull(nextCursor);
        Assert.Equal(2, secondPageUsernames.Count);
        Assert.Equal(JsonValueKind.Null, secondPage.GetProperty("nextCursor").ValueKind);
        List<string> walkedUsernames = [.. firstPageUsernames, .. secondPageUsernames];
        Assert.Equal(expectedUsernames, walkedUsernames);
    }

    [Fact]
    public void TiedTimestampsPaginateWithoutDuplicates() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        Guid callerUserAccountId = FriendshipTestActions.ResolveUserAccountId(callerAuthToken);
        DateTime tiedTime = DateTime.UtcNow.AddDays(-1);
        for (int index = 0; index < 31; index++)
            FriendshipTestActions.SeedAcceptedFriendship(callerUserAccountId, FriendshipTestActions.ResolveUserAccountId(FriendshipTestActions.CreateUser(container, "Tied Friend")), tiedTime);

        var firstPage = FriendshipTestActions.ListFriends(container, callerAuthToken).ReadContentAsJsonDocument().RootElement;
        string nextCursor = firstPage.GetProperty("nextCursor").GetString();
        var secondPage = FriendshipTestActions.ListFriends(container, callerAuthToken, cursor: nextCursor).ReadContentAsJsonDocument().RootElement;

        List<string> firstPageUsernames = [.. firstPage.GetProperty("friends").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        List<string> secondPageUsernames = [.. secondPage.GetProperty("friends").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        Assert.Equal(30, firstPageUsernames.Count);
        Assert.Single(secondPageUsernames);
        Assert.Equal(JsonValueKind.Null, secondPage.GetProperty("nextCursor").ValueKind);
        List<string> walkedUsernames = [.. firstPageUsernames, .. secondPageUsernames];
        Assert.Equal(31, walkedUsernames.Distinct().Count());
    }

    [Fact]
    public void InvalidCursorReturnsEmptyPageWithGlobalTotalCount() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "First Friend"));
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Second Friend"));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, cursor: "not-a-cursor");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = response.ReadContentAsJsonDocument().RootElement;
        Assert.Empty(root.GetProperty("friends").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public void WrongMarkerCursorReturnsEmptyPage() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Only Friend"));
        string wrongMarkerCursor = CursorCodec.EncodeFeedCursor(51, DateTime.UtcNow.Ticks, 0, Guid.NewGuid());

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, cursor: wrongMarkerCursor);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = response.ReadContentAsJsonDocument().RootElement;
        Assert.Empty(root.GetProperty("friends").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
    }

    // Tests - Search Within Friends

    [Fact]
    public void SearchMatchesDisplayNameContains() {
        using var container = new TestingMockProvidersContainer();
        string marker = "z" + Guid.NewGuid().ToString("N")[..8];
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string matchingAuthToken = FriendshipTestActions.CreateUser(container, "Zebra " + marker);
        FriendshipTestActions.MakeFriends(container, callerAuthToken, matchingAuthToken);
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Plain Other"));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, search: marker);

        var root = response.ReadContentAsJsonDocument().RootElement;
        var onlyRow = root.GetProperty("friends").EnumerateArray().Single();
        Assert.Equal(FriendshipTestActions.ResolveUsername(matchingAuthToken), onlyRow.GetProperty("username").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public void SearchMatchesUsernamePrefix() {
        using var container = new TestingMockProvidersContainer();
        string uniqueUsername = "u" + Guid.NewGuid().ToString("N")[..10] + "1";
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string matchingAuthToken = FriendshipTestActions.CreateUser(container, "Friend Target");
        SetProfile(container, matchingAuthToken, uniqueUsername, "Friend Target");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, matchingAuthToken);
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Plain Other"));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, search: uniqueUsername[..8]);

        var onlyRow = response.ReadContentAsJsonDocument().RootElement.GetProperty("friends").EnumerateArray().Single();
        Assert.Equal(uniqueUsername, onlyRow.GetProperty("username").GetString());
    }

    [Fact]
    public void SearchWithNoMatchesReturnsEmptyButGlobalTotalCount() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        FriendshipTestActions.MakeFriends(container, callerAuthToken, FriendshipTestActions.CreateUser(container, "Only Friend"));

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, search: "q" + Guid.NewGuid().ToString("N"));

        var root = response.ReadContentAsJsonDocument().RootElement;
        Assert.Empty(root.GetProperty("friends").EnumerateArray());
        Assert.Equal(1, root.GetProperty("totalCount").GetInt32());
    }

    // Tests - Other User Mode

    [Fact]
    public void OtherUsersListShowsPerRowStatusIncludingSelf() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string ownerAuthToken = FriendshipTestActions.CreateUser(container, "Owner");
        string strangerAuthToken = FriendshipTestActions.CreateUser(container, "Stranger");
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, callerAuthToken);
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, strangerAuthToken);

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, FriendshipTestActions.ResolveUsername(ownerAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = response.ReadContentAsJsonDocument().RootElement.GetProperty("friends").EnumerateArray().ToDictionary(row => row.GetProperty("username").GetString(), row => row.GetProperty("friendshipStatus").GetString());
        Assert.Equal(2, rows.Count);
        Assert.Equal("self", rows[FriendshipTestActions.ResolveUsername(callerAuthToken)]);
        Assert.Equal("none", rows[FriendshipTestActions.ResolveUsername(strangerAuthToken)]);
    }

    [Fact]
    public void RowStatusBecomesRequestSentAfterSend() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string ownerAuthToken = FriendshipTestActions.CreateUser(container, "Owner");
        string strangerAuthToken = FriendshipTestActions.CreateUser(container, "Stranger");
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, callerAuthToken);
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, strangerAuthToken);
        FriendshipTestActions.SendRequest(container, callerAuthToken, FriendshipTestActions.ResolveUsername(strangerAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, FriendshipTestActions.ResolveUsername(ownerAuthToken));

        var rows = response.ReadContentAsJsonDocument().RootElement.GetProperty("friends").EnumerateArray().ToDictionary(row => row.GetProperty("username").GetString(), row => row.GetProperty("friendshipStatus").GetString());
        Assert.Equal("requestSent", rows[FriendshipTestActions.ResolveUsername(strangerAuthToken)]);
    }

    // Tests - Visibility

    [Fact]
    public void BlockedOwnerReturnsNotFoundMatchingNonexistent() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked Owner");
        FriendshipTestActions.Block(container, callerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage blockedResponse = FriendshipTestActions.ListFriends(container, callerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken));
        HttpResponseMessage nonexistentResponse = FriendshipTestActions.ListFriends(container, callerAuthToken, "ghost" + Guid.NewGuid().ToString("N")[..10] + "1");

        Assert.Equal(HttpStatusCode.NotFound, blockedResponse.StatusCode);
        Assert.Equal(nonexistentResponse.StatusCode, blockedResponse.StatusCode);
        var blockedBody = blockedResponse.ReadContentAsJsonDocument().RootElement;
        var nonexistentBody = nonexistentResponse.ReadContentAsJsonDocument().RootElement;
        List<string> blockedProperties = [.. blockedBody.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> nonexistentProperties = [.. nonexistentBody.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        Assert.Equal(nonexistentProperties, blockedProperties);
        Assert.Equal(nonexistentBody.GetProperty("title").GetString(), blockedBody.GetProperty("title").GetString());
        Assert.Equal(nonexistentBody.GetProperty("status").GetInt32(), blockedBody.GetProperty("status").GetInt32());
    }

    [Fact]
    public void BlockRelatedRowsHiddenButTotalCountGlobal() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string ownerAuthToken = FriendshipTestActions.CreateUser(container, "Owner");
        string hiddenAuthToken = FriendshipTestActions.CreateUser(container, "Hidden Friend");
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, callerAuthToken);
        FriendshipTestActions.MakeFriends(container, ownerAuthToken, hiddenAuthToken);
        FriendshipTestActions.Block(container, callerAuthToken, FriendshipTestActions.ResolveUsername(hiddenAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, callerAuthToken, FriendshipTestActions.ResolveUsername(ownerAuthToken));

        var root = response.ReadContentAsJsonDocument().RootElement;
        var onlyRow = root.GetProperty("friends").EnumerateArray().Single();
        Assert.Equal(FriendshipTestActions.ResolveUsername(callerAuthToken), onlyRow.GetProperty("username").GetString());
        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
    }

    // Tests - Guests

    [Fact]
    public void GuestCallerOwnListIsEmpty() {
        using var container = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, guestAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = response.ReadContentAsJsonDocument().RootElement;
        Assert.Empty(root.GetProperty("friends").EnumerateArray());
        Assert.Equal(0, root.GetProperty("totalCount").GetInt32());
    }

    // Tests - Authentication

    [Fact]
    public void EmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, "");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListFriends(container, "invalid-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/listFriends", new { Username = "someone1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Methods - Helpers

    private static void SetProfile(TestingMockProvidersContainer container, string authToken, string username, string displayName) {
        container.WebClient.PostJson("api/userProfile/updateProfile", new { AuthToken = authToken, Username = username, DisplayName = displayName, Bio = "" }).EnsureSuccessStatusCode();
    }
}
