using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class UnfriendTest {
    // Tests - Happy Path

    [Fact]
    public void UnfriendDeletesTheFriendship() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("unfriended", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    [Fact]
    public void EitherSideCanUnfriend() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, friends.AddresseeAuthToken, friends.RequesterUsername);

        Assert.Equal("unfriended", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken)));
    }

    // Tests - Guarded Transitions

    [Fact]
    public void UnfriendWithOnlyAPendingRequestReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public void UnfriendANonFriendReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void ReRequestAfterUnfriendWorks() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        FriendshipTestActions.Unfriend(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Callers And Targets

    [Fact]
    public void GuestCallerReturnsAccountRequired() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void NonexistentUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Unfriender");

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, authToken, "nonexistentuser999999");

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Unfriend(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/unfriend", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
