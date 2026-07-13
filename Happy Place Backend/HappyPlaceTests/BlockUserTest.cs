using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class BlockUserTest {
    // Tests - Blocking

    [Fact]
    public void BlockCreatesABlockRow() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        Guid blockerUserAccountId = FriendshipTestActions.ResolveUserAccountId(blockerAuthToken);
        Guid targetUserAccountId = FriendshipTestActions.ResolveUserAccountId(targetAuthToken);

        HttpResponseMessage response = FriendshipTestActions.Block(container, blockerAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("blocked", FriendshipTestActions.ReadStatus(response));
        Assert.True(FriendshipTestActions.BlockRowExists(blockerUserAccountId, targetUserAccountId));
        Assert.False(FriendshipTestActions.BlockRowExists(targetUserAccountId, blockerUserAccountId));
    }

    [Fact]
    public void BlockSeversAnAcceptedFriendship() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.Block(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal("blocked", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
        Assert.True(FriendshipTestActions.BlockRowExists(requesterUserAccountId, addresseeUserAccountId));
    }

    [Fact]
    public void BlockSeversTheCallersOutgoingPendingRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        FriendshipTestActions.Block(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();

        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    [Fact]
    public void BlockSeversTheCallersIncomingPendingRequest() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        FriendshipTestActions.Block(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    [Fact]
    public void DoubleBlockIsIdempotent() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string targetUsername = FriendshipTestActions.ResolveUsername(targetAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, targetUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.Block(container, blockerAuthToken, targetUsername);

        Assert.Equal("blocked", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(1, FriendshipTestActions.CountBlockRowsFrom(FriendshipTestActions.ResolveUserAccountId(blockerAuthToken)));
    }

    [Fact]
    public void MutualBlocksCoexist() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");
        Guid firstUserAccountId = FriendshipTestActions.ResolveUserAccountId(firstAuthToken);
        Guid secondUserAccountId = FriendshipTestActions.ResolveUserAccountId(secondAuthToken);

        FriendshipTestActions.Block(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.Block(container, secondAuthToken, FriendshipTestActions.ResolveUsername(firstAuthToken)).EnsureSuccessStatusCode();

        Assert.True(FriendshipTestActions.BlockRowExists(firstUserAccountId, secondUserAccountId));
        Assert.True(FriendshipTestActions.BlockRowExists(secondUserAccountId, firstUserAccountId));
    }

    // Tests - Unblocking

    [Fact]
    public void UnblockRemovesOnlyTheCallersRow() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");
        string firstUsername = FriendshipTestActions.ResolveUsername(firstAuthToken);
        string secondUsername = FriendshipTestActions.ResolveUsername(secondAuthToken);
        Guid firstUserAccountId = FriendshipTestActions.ResolveUserAccountId(firstAuthToken);
        Guid secondUserAccountId = FriendshipTestActions.ResolveUserAccountId(secondAuthToken);
        FriendshipTestActions.Block(container, firstAuthToken, secondUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.Block(container, secondAuthToken, firstUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.Unblock(container, firstAuthToken, secondUsername);

        Assert.Equal("unblocked", FriendshipTestActions.ReadStatus(response));
        Assert.False(FriendshipTestActions.BlockRowExists(firstUserAccountId, secondUserAccountId));
        Assert.True(FriendshipTestActions.BlockRowExists(secondUserAccountId, firstUserAccountId));
        Assert.Equal("none", FriendshipTestActions.ReadStatus(FriendshipTestActions.SendRequest(container, firstAuthToken, secondUsername)));
    }

    [Fact]
    public void UnblockDoesNotRestoreTheFriendship() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(friends.AddresseeAuthToken);
        FriendshipTestActions.Block(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        FriendshipTestActions.Unblock(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        Assert.Null(FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId));
        Assert.Equal("requested", FriendshipTestActions.ReadStatus(FriendshipTestActions.SendRequest(container, friends.RequesterAuthToken, friends.AddresseeUsername)));
    }

    [Fact]
    public void UnblockWithNothingBlockedReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");

        HttpResponseMessage response = FriendshipTestActions.Unblock(container, firstAuthToken, FriendshipTestActions.ResolveUsername(secondAuthToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Blocked Interactions

    [Fact]
    public void NeitherSideCanSendARequestAcrossABlock() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockerUsername = FriendshipTestActions.ResolveUsername(blockerAuthToken);
        string blockedUsername = FriendshipTestActions.ResolveUsername(blockedAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, blockedUsername).EnsureSuccessStatusCode();

        HttpResponseMessage blockerSendResponse = FriendshipTestActions.SendRequest(container, blockerAuthToken, blockedUsername);
        HttpResponseMessage blockedSendResponse = FriendshipTestActions.SendRequest(container, blockedAuthToken, blockerUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(blockerSendResponse));
        Assert.Equal("none", FriendshipTestActions.ReadStatus(blockedSendResponse));
        Assert.Equal(0, FriendshipTestActions.CountFriendshipRowsBetween(FriendshipTestActions.ResolveUserAccountId(blockerAuthToken), FriendshipTestActions.ResolveUserAccountId(blockedAuthToken)));
    }

    [Fact]
    public void BlockedSendResponsesMatchNonexistentTargetResponses() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string blockedAuthToken = FriendshipTestActions.CreateUser(container, "Blocked");
        string blockerUsername = FriendshipTestActions.ResolveUsername(blockerAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        string blockedBody = FriendshipTestActions.ReadBody(FriendshipTestActions.SendRequest(container, blockedAuthToken, blockerUsername));
        string nonexistentBody = FriendshipTestActions.ReadBody(FriendshipTestActions.SendRequest(container, blockedAuthToken, "nonexistentuser999999"));

        Assert.Equal(nonexistentBody, blockedBody);
    }

    [Fact]
    public void AcceptAcrossABlockReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.Block(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken), FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken)));
    }

    // Tests - Notifications

    [Fact]
    public void BlockingAPendingRequesterDismissesTheBlockersOwnNotification() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string requesterUsername = FriendshipTestActions.ResolveUsername(requesterAuthToken);
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, requesterAuthToken, FriendshipTestActions.ResolveUsername(recipientAuthToken)).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        FriendshipTestActions.Block(container, recipientAuthToken, requesterUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        PushMessage dismissal = NotificationTestActions.DismissalsTo(container, recipientDeviceToken).Single();
        Assert.Equal("friend-requests", dismissal.CollapseId);
        Assert.Single(NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken));
    }

    [Fact]
    public void BlockingAPendingAddresseeDismissesTheirNotification() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string targetUsername = FriendshipTestActions.ResolveUsername(targetAuthToken);
        string targetDeviceToken = NotificationTestActions.RegisterNewDevice(container, targetAuthToken);
        FriendshipTestActions.SendRequest(container, blockerAuthToken, targetUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        FriendshipTestActions.Block(container, blockerAuthToken, targetUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        PushMessage dismissal = NotificationTestActions.DismissalsTo(container, targetDeviceToken).Single();
        Assert.Equal("friend-requests", dismissal.CollapseId);
    }

    // Tests - None Targets And Callers

    [Fact]
    public void BlockingSelfReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Loner");

        HttpResponseMessage response = FriendshipTestActions.Block(container, authToken, FriendshipTestActions.ResolveUsername(authToken));

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountBlockRowsFrom(FriendshipTestActions.ResolveUserAccountId(authToken)));
    }

    [Fact]
    public void BlockingANonexistentUsernameReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Blocker");

        HttpResponseMessage response = FriendshipTestActions.Block(container, authToken, "nonexistentuser999999");

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void BlockingAGuestTargetReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string guestTargetAuthToken = FriendshipTestActions.CreateUser(container, "GuestTarget");
        string guestTargetUsername = FriendshipTestActions.ResolveUsername(guestTargetAuthToken);
        FriendshipTestActions.MakeAnonymous(guestTargetAuthToken);

        HttpResponseMessage response = FriendshipTestActions.Block(container, blockerAuthToken, guestTargetUsername);

        Assert.Equal("none", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountBlockRowsFrom(FriendshipTestActions.ResolveUserAccountId(blockerAuthToken)));
    }

    [Fact]
    public void GuestCallerReturnsAccountRequiredForBlock() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.Block(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
        Assert.Equal(0, FriendshipTestActions.CountBlockRowsFrom(FriendshipTestActions.ResolveUserAccountId(guestAuthToken)));
    }

    [Fact]
    public void GuestCallerReturnsAccountRequiredForUnblock() {
        using var container = new TestingMockProvidersContainer();
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.Unblock(container, guestAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("accountRequired", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Blocked List

    [Fact]
    public void ListBlockedReturnsBlockedUsersNewestFirst() {
        using var container = new TestingMockProvidersContainer();
        string blockerAuthToken = FriendshipTestActions.CreateUser(container, "Blocker");
        string firstBlockedAuthToken = FriendshipTestActions.CreateUser(container, "FirstBlocked");
        string secondBlockedAuthToken = FriendshipTestActions.CreateUser(container, "SecondBlocked");
        string firstBlockedUsername = FriendshipTestActions.ResolveUsername(firstBlockedAuthToken);
        string secondBlockedUsername = FriendshipTestActions.ResolveUsername(secondBlockedAuthToken);
        Guid blockerUserAccountId = FriendshipTestActions.ResolveUserAccountId(blockerAuthToken);
        Guid firstBlockedUserAccountId = FriendshipTestActions.ResolveUserAccountId(firstBlockedAuthToken);
        FriendshipTestActions.Block(container, blockerAuthToken, firstBlockedUsername).EnsureSuccessStatusCode();
        using (var dbContext = HappyPlaceDbContext.Create()) {
            var firstBlockRow = dbContext.UserBlocks.Single(field => field.BlockerUserAccountId == blockerUserAccountId && field.BlockedUserAccountId == firstBlockedUserAccountId);
            firstBlockRow.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
            dbContext.SaveChanges();
        }
        FriendshipTestActions.Block(container, blockerAuthToken, secondBlockedUsername).EnsureSuccessStatusCode();

        HttpResponseMessage response = FriendshipTestActions.ListBlocked(container, blockerAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var blockedUsers = response.ReadContentAsJsonDocument().RootElement.GetProperty("blockedUsers");
        Assert.Equal(2, blockedUsers.GetArrayLength());
        Assert.Equal(secondBlockedUsername, blockedUsers[0].GetProperty("username").GetString());
        Assert.Equal(firstBlockedUsername, blockedUsers[1].GetProperty("username").GetString());
        Assert.True(blockedUsers[0].TryGetProperty("displayName", out _));
        Assert.True(blockedUsers[0].TryGetProperty("avatarColor", out _));
    }

    [Fact]
    public void ListBlockedIsEmptyWhenNothingIsBlocked() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "Blocker");

        HttpResponseMessage response = FriendshipTestActions.ListBlocked(container, authToken);

        Assert.Equal(0, response.ReadContentAsJsonDocument().RootElement.GetProperty("blockedUsers").GetArrayLength());
    }

    [Fact]
    public void ListBlockedIsEmptyForAGuestCaller() {
        using var container = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage response = FriendshipTestActions.ListBlocked(container, guestAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, response.ReadContentAsJsonDocument().RootElement.GetProperty("blockedUsers").GetArrayLength());
    }

    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorizedForBlock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Block(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorizedForBlock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Block(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorizedForBlock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/block", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyTokenReturnsUnauthorizedForUnblock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Unblock(container, "", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorizedForUnblock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.Unblock(container, "not-a-real-token", "anyuser1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorizedForUnblock() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/unblock", new { Username = "anyuser1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void EmptyTokenReturnsUnauthorizedForListBlocked() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListBlocked(container, "");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorizedForListBlocked() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListBlocked(container, "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingTokenFieldReturnsUnauthorizedForListBlocked() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/listBlocked", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
