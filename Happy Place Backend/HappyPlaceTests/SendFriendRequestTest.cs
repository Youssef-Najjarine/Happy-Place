using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SendFriendRequestTest {
    // Tests - Happy Path

    [Fact]
    public void SendCreatesAPendingRequestWithTheCorrectDirection() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string addresseeAuthToken = FriendshipTestActions.CreateUser(container, "Addressee");
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(requesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(addresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, requesterAuthToken, FriendshipTestActions.ResolveUsername(addresseeAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId);
        Assert.NotNull(friendship);
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
        Assert.Equal(requesterUserAccountId, friendship.RequesterUserAccountId);
        Assert.Equal(addresseeUserAccountId, friendship.AddresseeUserAccountId);
        Assert.Null(friendship.RespondedAtUtc);
    }

    [Fact]
    public void SendWritesExactlyOneAuditRow() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string addresseeAuthToken = FriendshipTestActions.CreateUser(container, "Addressee");
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(requesterAuthToken);

        FriendshipTestActions.SendRequest(container, requesterAuthToken, FriendshipTestActions.ResolveUsername(addresseeAuthToken)).EnsureSuccessStatusCode();

        Assert.Equal(1, FriendshipTestActions.CountAuditsFrom(requesterUserAccountId));
    }

    // Tests - None Targets

    [Fact]
    public void SelfRequestReturnsNoneWithoutARow() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Loner");
        Guid userAccountId = FriendshipTestActions.ResolveUserAccountId(authToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, authToken, FriendshipTestActions.ResolveUsername(authToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountFriendshipRowsBetween(userAccountId, userAccountId));
    }

    [Fact]
    public void NonexistentUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Sender");

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, authToken, "nonexistentuser999999");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void GuestTargetReturnsNoneWithoutARow() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string guestTargetAuthToken = FriendshipTestActions.CreateUser(container, "GuestTarget");
        string guestTargetUsername = FriendshipTestActions.ResolveUsername(guestTargetAuthToken);
        FriendshipTestActions.MakeAnonymous(guestTargetAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, guestTargetUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountFriendshipRowsBetween(FriendshipTestActions.ResolveUserAccountId(senderAuthToken), FriendshipTestActions.ResolveUserAccountId(guestTargetAuthToken)));
    }

    [Fact]
    public void EmptyUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Sender");

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, authToken, "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void MissingUsernameFieldReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Sender");

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/sendRequest", new { AuthToken = authToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void NoneResponsesAreIdenticalAcrossTargetKinds() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string guestTargetAuthToken = FriendshipTestActions.CreateUser(container, "GuestTarget");
        string guestTargetUsername = FriendshipTestActions.ResolveUsername(guestTargetAuthToken);
        FriendshipTestActions.MakeAnonymous(guestTargetAuthToken);

        string nonexistentBody = FriendshipTestActions.ReadBody(FriendshipTestActions.SendRequest(container, senderAuthToken, "nonexistentuser999999"));
        string guestBody = FriendshipTestActions.ReadBody(FriendshipTestActions.SendRequest(container, senderAuthToken, guestTargetUsername));
        string selfBody = FriendshipTestActions.ReadBody(FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(senderAuthToken)));

        Assert.Equal(nonexistentBody, guestBody);
        Assert.Equal(nonexistentBody, selfBody);
    }

    // Tests - Guest Caller

    [Fact]
    public void GuestCallerReturnsAccountRequiredWithoutARow() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountFriendshipRowsBetween(FriendshipTestActions.ResolveUserAccountId(guestAuthToken), FriendshipTestActions.ResolveUserAccountId(targetAuthToken)));
    }

    // Tests - Repeat Sends

    [Fact]
    public void DuplicateSendReturnsAlreadyRequestedWithoutASecondRowOrAudit() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("alreadyRequested", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(1, FriendshipTestActions.CountFriendshipRowsBetween(requesterUserAccountId, addresseeUserAccountId));
        Assert.Equal(1, FriendshipTestActions.CountAuditsFrom(requesterUserAccountId));
    }

    [Fact]
    public void SendWhenAlreadyFriendsReturnsAlreadyFriends() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);

        HttpResponseMessage requesterResponse = FriendshipTestActions.SendRequest(container, friends.RequesterAuthToken, friends.AddresseeUsername);
        HttpResponseMessage addresseeResponse = FriendshipTestActions.SendRequest(container, friends.AddresseeAuthToken, friends.RequesterUsername);

        Assert.Equal("alreadyFriends", FriendshipTestActions.ReadStatus(requesterResponse));
        Assert.Equal("alreadyFriends", FriendshipTestActions.ReadStatus(addresseeResponse));
    }

    [Fact]
    public void SendAfterCancelCreatesAFreshRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken));
        Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    }

    [Fact]
    public void SendAfterDeclineCreatesAFreshRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.DeclineRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void SendAfterUnfriendCreatesAFreshRequest() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        FriendshipTestActions.Unfriend(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, friends.AddresseeAuthToken, friends.RequesterUsername);

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Username Handling

    [Fact]
    public void UsernameLookupIsCaseInsensitiveAndTrimmed() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string addresseeAuthToken = FriendshipTestActions.CreateUser(container, "Addressee");
        string addresseeUsername = FriendshipTestActions.ResolveUsername(addresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, requesterAuthToken, "  " + addresseeUsername.ToUpperInvariant() + "  ");

        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void FriendshipSurvivesTheTargetChangingTheirUsername() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken);
        string newUsername = "renamed" + (DateTime.UtcNow.Ticks % 1000000000);
        container.WebClient.PostJson("api/userProfile/updateProfile", new { AuthToken = friends.AddresseeAuthToken, Username = newUsername, DisplayName = "Renamed User", Bio = "" }).EnsureSuccessStatusCode();

        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId);
        HttpResponseMessage oldUsernameResponse = FriendshipTestActions.SendRequest(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.Equal("none", FriendshipTestActions.ReadStatus(oldUsernameResponse));
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/sendRequest", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
