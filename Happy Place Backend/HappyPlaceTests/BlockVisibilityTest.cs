using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class BlockVisibilityTest {
    // Tests - Profile Invisibility

    [Fact]
    public void BlockedTargetProfileReturnsNotFoundToTheBlocker() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockedUsername = FriendshipTestActions.ResolveUsername(blockedAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, blockedUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = ProfileTestActions.GetPublicUserProfile(container, blockerAuthToken, blockedUsername);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void BlockersProfileReturnsNotFoundToTheBlockedUser() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockerUsername = FriendshipTestActions.ResolveUsername(blockerAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage response = ProfileTestActions.GetPublicUserProfile(container, blockedAuthToken, blockerUsername);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void UnblockRestoresProfileVisibility() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockedUsername = FriendshipTestActions.ResolveUsername(blockedAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, blockedUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.Unblock(container, blockerAuthToken, blockedUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = ProfileTestActions.GetPublicUserProfile(container, blockerAuthToken, blockedUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", response.ReadContentAsJsonDocument().RootElement.GetProperty("friendshipStatus").GetString());
    }

    [Fact]
    public void OneSidedUnblockKeepsMutualInvisibility() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");
        string firstUsername = FriendshipTestActions.ResolveUsername(firstAuthToken);
        string secondUsername = FriendshipTestActions.ResolveUsername(secondAuthToken);
        FriendshipTestActions.Block(container, firstAuthToken, secondUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.Block(container, secondAuthToken, firstUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.Unblock(container, firstAuthToken, secondUsername).EnsureSuccessStatusCode();

        HttpResponseMessage firstViewResponse = ProfileTestActions.GetPublicUserProfile(container, firstAuthToken, secondUsername);
        HttpResponseMessage secondViewResponse = ProfileTestActions.GetPublicUserProfile(container, secondAuthToken, firstUsername);

        Assert.Equal(HttpStatusCode.NotFound, firstViewResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondViewResponse.StatusCode);
    }

    // Tests - Enumeration Safety

    [Fact]
    public void BlockedProfileResponseMatchesNonexistentUserResponse() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockerUsername = FriendshipTestActions.ResolveUsername(blockerAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        HttpResponseMessage blockedResponse = ProfileTestActions.GetPublicUserProfile(container, blockedAuthToken, blockerUsername);
        HttpResponseMessage nonexistentResponse = ProfileTestActions.GetPublicUserProfile(container, blockedAuthToken, "nonexistentuser999999");

        Assert.Equal(HttpStatusCode.NotFound, blockedResponse.StatusCode);
        Assert.Equal(nonexistentResponse.StatusCode, blockedResponse.StatusCode);
        var blockedBody = blockedResponse.ReadContentAsJsonDocument();
        var nonexistentBody = nonexistentResponse.ReadContentAsJsonDocument();
        List<string> blockedProperties = [.. blockedBody.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> nonexistentProperties = [.. nonexistentBody.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        Assert.Equal(nonexistentProperties, blockedProperties);
        Assert.Equal(nonexistentBody.RootElement.GetProperty("title").GetString(), blockedBody.RootElement.GetProperty("title").GetString());
        Assert.Equal(nonexistentBody.RootElement.GetProperty("status").GetInt32(), blockedBody.RootElement.GetProperty("status").GetInt32());
    }
}
