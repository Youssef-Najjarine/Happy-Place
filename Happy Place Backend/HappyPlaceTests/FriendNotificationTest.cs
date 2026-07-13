using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class FriendNotificationTest {
    // Tests - Requesting Alerts The Addressee

    [Fact]
    public void RequestAlertsTheAddresseeWithACount() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(recipientAuthToken)).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        PushMessage message = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken).Single();
        Assert.Equal("friendRequests", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
        Assert.Equal("friend-requests", message.CollapseId);
        Assert.True(message.Alerting);
        Assert.Contains("sent you a friend request", message.Body);
    }

    [Fact]
    public void ThreeRapidRequestsCoalesceIntoOneNotification() {
        using var container = new TestingMockProvidersContainer();
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "First"), recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "Second"), recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "Third"), recipientUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        PushMessage message = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken).Single();
        Assert.Equal("3", message.Data["count"]);
        Assert.True(message.Alerting);
        Assert.Contains("sent you friend requests", message.Body);
    }

    [Fact]
    public void DuplicateSendDoesNotIncrementTheCount() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        Assert.Equal("1", NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken).Single().Data["count"]);
    }

    // Tests - Decrements Are Passive

    [Fact]
    public void CancelDecrementsTheCountWithoutAlerting() {
        using var container = new TestingMockProvidersContainer();
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        string firstSenderAuthToken = FriendshipTestActions.CreateUser(container, "First");
        FriendshipTestActions.SendRequest(container, firstSenderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "Second"), recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.CancelRequest(container, firstSenderAuthToken, recipientUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        List<PushMessage> messages = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
    }

    [Fact]
    public void DeclineDecrementsTheCountWithoutAlerting() {
        using var container = new TestingMockProvidersContainer();
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        string firstSenderAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string firstSenderUsername = FriendshipTestActions.ResolveUsername(firstSenderAuthToken);
        FriendshipTestActions.SendRequest(container, firstSenderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "Second"), recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.DeclineRequest(container, recipientAuthToken, firstSenderUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        List<PushMessage> messages = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
    }

    [Fact]
    public void AcceptDecrementsTheCountWithoutAlerting() {
        using var container = new TestingMockProvidersContainer();
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        string firstSenderAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string firstSenderUsername = FriendshipTestActions.ResolveUsername(firstSenderAuthToken);
        FriendshipTestActions.SendRequest(container, firstSenderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, FriendshipTestActions.CreateUser(container, "Second"), recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.AcceptRequest(container, recipientAuthToken, firstSenderUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        List<PushMessage> messages = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
    }

    // Tests - Dismissal When The Last Request Resolves

    [Fact]
    public void CancellingTheLastRequestDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.CancelRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        PushMessage dismissal = NotificationTestActions.DismissalsTo(container, recipientDeviceToken).Single();
        Assert.Equal("friend-requests", dismissal.CollapseId);
    }

    [Fact]
    public void DecliningTheLastRequestDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string senderUsername = FriendshipTestActions.ResolveUsername(senderAuthToken);
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(recipientAuthToken)).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.DeclineRequest(container, recipientAuthToken, senderUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        Assert.Single(NotificationTestActions.DismissalsTo(container, recipientDeviceToken));
    }

    [Fact]
    public void AcceptingTheLastRequestDismissesTheAddresseesNotification() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        string addresseeDeviceToken = NotificationTestActions.RegisterNewDevice(container, pendingPair.AddresseeAuthToken);
        NotificationTestActions.Flush();
        FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        PushMessage dismissal = NotificationTestActions.DismissalsTo(container, addresseeDeviceToken).Single();
        Assert.Equal("friend-requests", dismissal.CollapseId);
    }

    [Fact]
    public void ReRequestAfterCancelAlertsAgain() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.CancelRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.SendRequest(container, senderAuthToken, recipientUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        List<PushMessage> messages = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.True(messages[1].Alerting);
        Assert.Single(NotificationTestActions.DismissalsTo(container, recipientDeviceToken));
    }

    // Tests - Acceptance Push To The Requester

    [Fact]
    public void AcceptFiresAnAlertingPushToTheRequester() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        string requesterDeviceToken = NotificationTestActions.RegisterNewDevice(container, pendingPair.RequesterAuthToken);
        Guid accepterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);
        using var dbContext = HappyPlaceDbContext.Create();
        var accepter = dbContext.UserAccounts.Single(field => field.Id == accepterUserAccountId);

        FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        PushMessage message = NotificationTestActions.CountUpdatesTo(container, requesterDeviceToken).Single();
        Assert.Equal("friendAccepted", message.Data["type"]);
        Assert.Equal(accepter.Username, message.Data["username"]);
        Assert.Equal($"friend-accepted-{accepterUserAccountId}", message.CollapseId);
        Assert.True(message.Alerting);
        Assert.Contains(accepter.DisplayName, message.Body);
        Assert.Contains("accepted your friend request", message.Body);
    }

    [Fact]
    public void MutualAutoAcceptFiresTheAcceptedPushToTheOriginalRequester() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "OriginalRequester");
        string accepterAuthToken = FriendshipTestActions.CreateUser(container, "Accepter");
        string requesterUsername = FriendshipTestActions.ResolveUsername(requesterAuthToken);
        string accepterUsername = FriendshipTestActions.ResolveUsername(accepterAuthToken);
        string requesterDeviceToken = NotificationTestActions.RegisterNewDevice(container, requesterAuthToken);
        string accepterDeviceToken = NotificationTestActions.RegisterNewDevice(container, accepterAuthToken);
        Guid accepterUserAccountId = FriendshipTestActions.ResolveUserAccountId(accepterAuthToken);
        FriendshipTestActions.SendRequest(container, requesterAuthToken, accepterUsername).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        HttpResponseMessage autoAcceptResponse = FriendshipTestActions.SendRequest(container, accepterAuthToken, requesterUsername);

        NotificationTestActions.Flush();

        Assert.Equal("accepted", FriendshipTestActions.ReadStatus(autoAcceptResponse));
        PushMessage acceptedPush = NotificationTestActions.CountUpdatesTo(container, requesterDeviceToken).Single();
        Assert.Equal("friendAccepted", acceptedPush.Data["type"]);
        Assert.Equal($"friend-accepted-{accepterUserAccountId}", acceptedPush.CollapseId);
        Assert.Single(NotificationTestActions.CountUpdatesTo(container, accepterDeviceToken));
        Assert.Single(NotificationTestActions.DismissalsTo(container, accepterDeviceToken));
    }

    // Tests - Silence

    [Fact]
    public void DeclineIsSilentToTheRequester() {
        using var container = new TestingMockProvidersContainer();
        string requesterAuthToken = FriendshipTestActions.CreateUser(container, "Requester");
        string requesterUsername = FriendshipTestActions.ResolveUsername(requesterAuthToken);
        string addresseeAuthToken = FriendshipTestActions.CreateUser(container, "Addressee");
        string requesterDeviceToken = NotificationTestActions.RegisterNewDevice(container, requesterAuthToken);
        FriendshipTestActions.SendRequest(container, requesterAuthToken, FriendshipTestActions.ResolveUsername(addresseeAuthToken)).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        FriendshipTestActions.DeclineRequest(container, addresseeAuthToken, requesterUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        Assert.Empty(NotificationTestActions.CountUpdatesTo(container, requesterDeviceToken));
        Assert.Empty(NotificationTestActions.DismissalsTo(container, requesterDeviceToken));
    }

    [Fact]
    public void UnfriendIsSilentToBothSides() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        string requesterDeviceToken = NotificationTestActions.RegisterNewDevice(container, friends.RequesterAuthToken);
        string addresseeDeviceToken = NotificationTestActions.RegisterNewDevice(container, friends.AddresseeAuthToken);
        NotificationTestActions.Flush();
        FriendshipTestActions.Unfriend(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        Assert.Empty(NotificationTestActions.CountUpdatesTo(container, requesterDeviceToken));
        Assert.Empty(NotificationTestActions.DismissalsTo(container, requesterDeviceToken));
        Assert.Empty(NotificationTestActions.CountUpdatesTo(container, addresseeDeviceToken));
        Assert.Empty(NotificationTestActions.DismissalsTo(container, addresseeDeviceToken));
    }

    // Tests - Targeting And Scoping

    [Fact]
    public void BystandersDoNotReceiveTheNotification() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string bystanderAuthToken = FriendshipTestActions.CreateUser(container, "Bystander");
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        string bystanderDeviceToken = NotificationTestActions.RegisterNewDevice(container, bystanderAuthToken);
        FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(recipientAuthToken)).EnsureSuccessStatusCode();

        NotificationTestActions.Flush();

        Assert.Single(NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken));
        Assert.Empty(NotificationTestActions.CountUpdatesTo(container, bystanderDeviceToken));
        Assert.Empty(NotificationTestActions.DismissalsTo(container, bystanderDeviceToken));
    }

    // Tests - Delivery Resilience

    [Fact]
    public void ZeroDeviceRecipientRecoversTheNotificationOnRegistration() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(recipientAuthToken)).EnsureSuccessStatusCode();
        NotificationTestActions.Flush();
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);

        NotificationTestActions.Flush();

        PushMessage message = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken).Single();
        Assert.Equal("friendRequests", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
    }

    // Tests - Channel Integrity

    [Fact]
    public void ConcurrentRequestsCreateExactlyOneChannel() {
        using var container = new TestingMockProvidersContainer();
        string recipientAuthToken = FriendshipTestActions.CreateUser(container, "Recipient");
        string recipientUsername = FriendshipTestActions.ResolveUsername(recipientAuthToken);
        Guid recipientUserAccountId = FriendshipTestActions.ResolveUserAccountId(recipientAuthToken);
        string recipientDeviceToken = NotificationTestActions.RegisterNewDevice(container, recipientAuthToken);
        string firstSenderAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondSenderAuthToken = FriendshipTestActions.CreateUser(container, "Second");
        string thirdSenderAuthToken = FriendshipTestActions.CreateUser(container, "Third");

        List<Exception> exceptions = FriendshipTestActions.RunConcurrently(
            () => FriendshipTestActions.SendRequest(container, firstSenderAuthToken, recipientUsername).EnsureSuccessStatusCode(),
            () => FriendshipTestActions.SendRequest(container, secondSenderAuthToken, recipientUsername).EnsureSuccessStatusCode(),
            () => FriendshipTestActions.SendRequest(container, thirdSenderAuthToken, recipientUsername).EnsureSuccessStatusCode());
        NotificationTestActions.Flush();

        Assert.Empty(exceptions);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.NotificationChannels.Count(field => field.Kind == NotificationChannelKind.FriendRequests && field.RecipientUserAccountId == recipientUserAccountId));
        PushMessage message = NotificationTestActions.CountUpdatesTo(container, recipientDeviceToken).Single();
        Assert.Equal("3", message.Data["count"]);
        Assert.True(message.Alerting);
    }
}
