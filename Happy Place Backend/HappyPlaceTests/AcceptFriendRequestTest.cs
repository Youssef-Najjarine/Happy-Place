using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class AcceptFriendRequestTest {
    // Tests - Happy Path

    [Fact]
    public void AcceptFlipsThePendingRequestToAccepted() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("accepted", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId);
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.Equal(requesterUserAccountId, friendship.RequesterUserAccountId);
        Assert.NotNull(friendship.RespondedAtUtc);
    }

    // Tests - Guarded Transitions

    [Fact]
    public void AcceptWithNoPendingRequestReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void AcceptingYourOwnOutgoingRequestReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public void DoubleAcceptReturnsAlreadyFriends() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("alreadyFriends", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void AcceptAfterCancelReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountFriendshipRowsBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken)));
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentAcceptVersusCancelEndsInExactlyOneOutcome() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);
        HttpResponseMessage acceptResponse = null;
        HttpResponseMessage cancelResponse = null;

        List<Exception> exceptions = FriendshipTestActions.RunConcurrently(
            () => acceptResponse = FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername),
            () => cancelResponse = FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername));

        Assert.Empty(exceptions);
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId);
        string acceptStatus = FriendshipTestActions.ReadStatus(acceptResponse);
        string cancelStatus = FriendshipTestActions.ReadStatus(cancelResponse);
        if (friendship != null) {
            Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
            Assert.Equal("accepted", acceptStatus);
            Assert.Equal("none", cancelStatus);
        }
        else {
            Assert.Equal("none", acceptStatus);
            Assert.Equal("canceled", cancelStatus);
        }
    }

    // Tests - Callers And Targets

    [Fact]
    public void GuestCallerReturnsAccountRequired() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void NonexistentUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Accepter");

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, authToken, "nonexistentuser999999");

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/acceptRequest", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
