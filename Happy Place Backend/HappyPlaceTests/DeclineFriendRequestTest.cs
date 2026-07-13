using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeclineFriendRequestTest {
    // Tests - Happy Path

    [Fact]
    public void DeclineDeletesThePendingRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("declined", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    // Tests - Guarded Transitions

    [Fact]
    public void DeclineWithNothingPendingReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void DeclineByTheRequesterReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public void DeclineDoesNotTouchAnAcceptedFriendship() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, friends.AddresseeAuthToken, friends.RequesterUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
    }

    [Fact]
    public void ResendAfterDeclineWorks() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.DeclineRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Callers And Targets

    [Fact]
    public void GuestCallerReturnsAccountRequired() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.DeclineRequest(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/declineRequest", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
