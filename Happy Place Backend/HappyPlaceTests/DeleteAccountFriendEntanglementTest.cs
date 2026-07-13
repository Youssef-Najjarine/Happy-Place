using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeleteAccountFriendEntanglementTest {
    // Tests - Row Cleanup

    [Fact]
    public void DeletingAnAccountRemovesItsFriendshipsInBothDirections() {
        using var container = new TestingMockProvidersContainer();
        string deletedAuthToken = FriendshipTestActions.CreateUser(container, "Deleted");
        string friendAuthToken = FriendshipTestActions.CreateUser(container, "Friend");
        string incomingRequesterAuthToken = FriendshipTestActions.CreateUser(container, "IncomingRequester");
        string outgoingAddresseeAuthToken = FriendshipTestActions.CreateUser(container, "OutgoingAddressee");
        string deletedUsername = FriendshipTestActions.ResolveUsername(deletedAuthToken);
        string friendUsername = FriendshipTestActions.ResolveUsername(friendAuthToken);
        Guid deletedUserAccountId = FriendshipTestActions.ResolveUserAccountId(deletedAuthToken);
        Guid friendUserAccountId = FriendshipTestActions.ResolveUserAccountId(friendAuthToken);
        Guid incomingRequesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(incomingRequesterAuthToken);
        Guid outgoingAddresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(outgoingAddresseeAuthToken);
        FriendshipTestActions.SendRequest(container, deletedAuthToken, friendUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.AcceptRequest(container, friendAuthToken, deletedUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, incomingRequesterAuthToken, deletedUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, deletedAuthToken, FriendshipTestActions.ResolveUsername(outgoingAddresseeAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, friendAuthToken, FriendshipTestActions.ResolveUsername(incomingRequesterAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.AcceptRequest(container, incomingRequesterAuthToken, friendUsername).EnsureSuccessStatusCode();

        DeleteAccount(container, deletedAuthToken);

        Assert.Null(FriendshipTestActions.FindFriendshipBetween(deletedUserAccountId, friendUserAccountId));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(deletedUserAccountId, incomingRequesterUserAccountId));
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(deletedUserAccountId, outgoingAddresseeUserAccountId));
        Friendship survivorFriendship = FriendshipTestActions.FindFriendshipBetween(friendUserAccountId, incomingRequesterUserAccountId);
        Assert.Equal(FriendshipStatus.Accepted, survivorFriendship.Status);
    }

    [Fact]
    public void DeletingAnAccountRemovesItsBlocksAndAudits() {
        using var container = new TestingMockProvidersContainer();
        string deletedAuthToken = FriendshipTestActions.CreateUser(container, "Deleted");
        string otherAuthToken = FriendshipTestActions.CreateUser(container, "Other");
        string thirdAuthToken = FriendshipTestActions.CreateUser(container, "Third");
        string deletedUsername = FriendshipTestActions.ResolveUsername(deletedAuthToken);
        Guid deletedUserAccountId = FriendshipTestActions.ResolveUserAccountId(deletedAuthToken);
        Guid otherUserAccountId = FriendshipTestActions.ResolveUserAccountId(otherAuthToken);
        Guid thirdUserAccountId = FriendshipTestActions.ResolveUserAccountId(thirdAuthToken);
        FriendshipTestActions.SendRequest(container, deletedAuthToken, FriendshipTestActions.ResolveUsername(otherAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, thirdAuthToken, deletedUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, otherAuthToken, FriendshipTestActions.ResolveUsername(thirdAuthToken)).EnsureSuccessStatusCode();
        SeedBlock(deletedUserAccountId, otherUserAccountId);
        SeedBlock(thirdUserAccountId, deletedUserAccountId);

        DeleteAccount(container, deletedAuthToken);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.UserBlocks.Any(field => field.BlockerUserAccountId == deletedUserAccountId || field.BlockedUserAccountId == deletedUserAccountId));
        Assert.False(dbContext.FriendRequestAudits.Any(field => field.RequesterUserAccountId == deletedUserAccountId || field.AddresseeUserAccountId == deletedUserAccountId));
        Assert.Equal(1, FriendshipTestActions.CountAuditsFrom(otherUserAccountId));
    }

    [Fact]
    public void DeletedUsersOwnFriendRequestChannelIsRemoved() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string deletedAuthToken = FriendshipTestActions.CreateUser(container, "Deleted");
        Guid deletedUserAccountId = FriendshipTestActions.ResolveUserAccountId(deletedAuthToken);
        FriendshipTestActions.SendRequest(container, requesterAuthToken, FriendshipTestActions.ResolveUsername(deletedAuthToken)).EnsureSuccessStatusCode();

        DeleteAccount(container, deletedAuthToken);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.FriendRequests && field.RecipientUserAccountId == deletedUserAccountId));
    }

    // Tests - Counterpart Notifications

    [Fact]
    public void DeletionDismissesTheAddresseesNotificationWhenItWasTheOnlyRequest() {
        using var container = new TestingMockProvidersContainer();
        string deletedAuthToken = FriendshipTestActions.CreateUser(container, "Deleted");
        string addresseeAuthToken = FriendshipTestActions.CreateUser(container, "Addressee");
        string addresseeDeviceToken = NotificationTestActions.RegisterNewDevice(container, addresseeAuthToken);
        FriendshipTestActions.SendRequest(container, deletedAuthToken, FriendshipTestActions.ResolveUsername(addresseeAuthToken)).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        DeleteAccount(container, deletedAuthToken);
        NotificationTestActions.Flush();

        PushMessage dismissal = NotificationTestActions.DismissalsTo(container, addresseeDeviceToken).Single();
        Assert.Equal("friend-requests", dismissal.CollapseId);
    }

    [Fact]
    public void DeletionDecrementsTheAddresseesCountWhenOtherRequestsRemain() {
        using var container = new TestingMockProvidersContainer();
        string deletedAuthToken = FriendshipTestActions.CreateUser(container, "Deleted");
        string otherSenderAuthToken = FriendshipTestActions.CreateUser(container, "OtherSender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, deletedAuthToken, recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, otherSenderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();

        DeleteAccount(container, deletedAuthToken);
        NotificationTestActions.Flush();

        List<PushMessage> messages = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
        Assert.Empty(NotificationTestActions.DismissalsTo(container, recipientDeviceToken));
    }

    // Helpers

    private static void DeleteAccount(TestingMockProvidersContainer container, string authToken) {
        container.WebClient.PostJson("api/userProfile/deleteAccount", new { AuthToken = authToken, Password = "Seven74!" }).EnsureSuccessStatusCode();
    }

    private static void SeedBlock(Guid blockerUserAccountId, Guid blockedUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserBlocks.Add(new() { Id = Guid.NewGuid(), BlockerUserAccountId = blockerUserAccountId, BlockedUserAccountId = blockedUserAccountId, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }
}
