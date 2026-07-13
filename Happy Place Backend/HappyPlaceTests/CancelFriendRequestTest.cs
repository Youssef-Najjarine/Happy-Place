using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CancelFriendRequestTest {
    // Tests - Happy Path

    [Fact]
    public void CancelDeletesThePendingRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("canceled", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    // Tests - Guarded Transitions

    [Fact]
    public void CancelWithNothingPendingReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void CancelDoesNotTouchAnAcceptedFriendship() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
    }

    [Fact]
    public void CancelOnlyRemovesTheCallersOwnOutgoingRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public void ResendAfterCancelWorks() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Callers And Targets

    [Fact]
    public void GuestCallerReturnsAccountRequired() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void NonexistentUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Canceler");

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, authToken, "nonexistentuser999999");

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.CancelRequest(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/cancelRequest", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
