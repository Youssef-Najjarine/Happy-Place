using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class AvailabilityOfferInteractionTest {
    // Tests - Going unavailable withdraws outstanding offers

    [Fact]
    public void GoingUnavailableWithdrawsAnOutstandingOffer() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        SetAvailable(container, helper, true);

        SetAvailable(container, helper, false);

        Assert.Equal(0, OfferedCount(chatGroupId));
    }

    [Fact]
    public void GoingUnavailableDeletesTheOfferRow() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);

        SetAvailable(container, helper, false);

        Assert.Equal(0, OfferRowCount(chatGroupId));
    }

    [Fact]
    public void GoingUnavailableWithdrawsOffersAcrossEveryRequest() {
        using var container = new TestingMockProvidersContainer();
        Guid firstRequest = CreateRequest(container, CreateGuest(container), "First");
        Guid secondRequest = CreateRequest(container, CreateGuest(container), "Second");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, firstRequest);
        CreateOffer(container, helper, secondRequest);

        SetAvailable(container, helper, false);

        Assert.Equal(0, OfferedCount(firstRequest));
        Assert.Equal(0, OfferedCount(secondRequest));
    }

    [Fact]
    public void WhenTheOnlyHelperGoesUnavailableTheSeekerHasNoReadyHelpers() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);

        SetAvailable(container, helper, false);

        Assert.Equal(0, OfferedCount(chatGroupId));
        Assert.Equal(ChatGroupStatus.Provisional, GroupStatus(chatGroupId));
    }

    // Tests - Multiple helpers

    [Fact]
    public void OneHelperGoingUnavailableLeavesTheOtherHelpersOffer() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string leavingHelper = CreateGuest(container);
        string stayingHelper = CreateGuest(container);
        CreateOffer(container, leavingHelper, chatGroupId);
        CreateOffer(container, stayingHelper, chatGroupId);

        SetAvailable(container, leavingHelper, false);

        Assert.Equal(1, OfferedCount(chatGroupId));
        Assert.True(HasOfferedFromHelper(chatGroupId, AccountId(stayingHelper)));
        Assert.False(HasOfferedFromHelper(chatGroupId, AccountId(leavingHelper)));
    }

    // Tests - Connect interactions

    [Fact]
    public void ConnectAfterTheOnlyHelperGoesUnavailableDoesNotActivate() {
        using var container = new TestingMockProvidersContainer();
        string seeker = CreateGuest(container);
        Guid chatGroupId = CreateRequest(container, seeker, "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        SetAvailable(container, helper, false);

        Connect(container, seeker, chatGroupId);

        Assert.Equal(ChatGroupStatus.Provisional, GroupStatus(chatGroupId));
    }

    [Fact]
    public void GoingUnavailableAfterConnectingKeepsTheConnectedOffer() {
        using var container = new TestingMockProvidersContainer();
        string seeker = CreateGuest(container);
        Guid chatGroupId = CreateRequest(container, seeker, "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        Connect(container, seeker, chatGroupId);

        SetAvailable(container, helper, false);

        Assert.Equal(ChatGroupStatus.Active, GroupStatus(chatGroupId));
        Assert.Equal(1, ConnectedCount(chatGroupId));
    }

    // Tests - Availability toggling

    [Fact]
    public void ComingBackAvailableDoesNotRestoreAWithdrawnOffer() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        SetAvailable(container, helper, false);

        SetAvailable(container, helper, true);

        Assert.Equal(0, OfferedCount(chatGroupId));
    }

    [Fact]
    public void AHelperCanReofferAfterWithdrawingByGoingUnavailable() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        SetAvailable(container, helper, false);
        SetAvailable(container, helper, true);

        CreateOffer(container, helper, chatGroupId);

        Assert.Equal(1, OfferedCount(chatGroupId));
    }

    // Tests - Scope of the withdrawal

    [Fact]
    public void GoingUnavailableDoesNotDeleteADeclinedOffer() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        DeclineOffer(container, helper, chatGroupId);

        SetAvailable(container, helper, false);

        Assert.Equal(1, OfferRowCount(chatGroupId));
        Assert.Equal(0, OfferedCount(chatGroupId));
    }

    [Fact]
    public void GoingUnavailableWithdrawsOffersButKeepsTheHelpersOwnRequest() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        Guid ownRequest = CreateRequest(container, person, "I need help too");
        Guid otherRequest = CreateRequest(container, CreateGuest(container), "Someone else");
        CreateOffer(container, person, otherRequest);

        SetAvailable(container, person, false);

        Assert.Equal(0, OfferedCount(otherRequest));
        Assert.True(GroupExists(ownRequest));
        Assert.Equal(ChatGroupStatus.Provisional, GroupStatus(ownRequest));
    }

    // Tests - No-op and idempotency

    [Fact]
    public void GoingUnavailableWithNoOffersDoesNothing() {
        using var container = new TestingMockProvidersContainer();
        string helper = CreateGuest(container);
        SetAvailable(container, helper, true);

        SetAvailable(container, helper, false);

        Assert.True(true);
    }

    [Fact]
    public void GoingUnavailableTwiceIsIdempotent() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);

        SetAvailable(container, helper, false);
        SetAvailable(container, helper, false);

        Assert.Equal(0, OfferedCount(chatGroupId));
    }

    // Tests - Notification side effect

    [Fact]
    public void GoingUnavailableDismissesTheSeekersLiveOffersNotification() {
        using var container = new TestingMockProvidersContainer();
        string seeker = CreateGuest(container);
        string seekerDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker, seekerDeviceToken);
        Guid chatGroupId = CreateRequest(container, seeker, "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);
        Flush();

        SetAvailable(container, helper, false);
        Flush();

        Assert.Single(DismissalsTo(container, seekerDeviceToken));
        Assert.Equal(0, OfferedCount(chatGroupId));
    }

    // Helpers - Acting

    private static string CreateGuest(TestingMockProvidersContainer container) {
        return TestUserFactory.CreateGuestUser(container);
    }

    private static Guid CreateRequest(TestingMockProvidersContainer container, string authToken, string topic) {
        string chatGroupId = container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = authToken, Topic = topic })
            .ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return Guid.Parse(chatGroupId);
    }

    private static void CreateOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void DeclineOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }

    private static void RegisterDevice(TestingMockProvidersContainer container, string authToken, string deviceToken) {
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = "ios" }).EnsureSuccessStatusCode();
    }

    private static void Flush() {
        MakeAllDirtyChannelsDue();
        NotificationDispatchManager.Sweep();
    }

    private static void MakeAllDirtyChannelsDue() {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime farPast = DateTime.UtcNow.AddMinutes(-10);
        dbContext.NotificationChannels
            .Where(field => field.DueAtUtc != null)
            .ExecuteUpdate(setters => setters
                .SetProperty(field => field.FirstDirtyAtUtc, farPast)
                .SetProperty(field => field.DueAtUtc, farPast)
                .SetProperty(field => field.LastSentAtUtc, (DateTime?)null));
    }

    // Helpers - Asserting

    private static Guid AccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static int OfferedCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered);
    }

    private static int ConnectedCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Connected);
    }

    private static int OfferRowCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId);
    }

    private static bool HasOfferedFromHelper(Guid chatGroupId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Any(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Offered);
    }

    private static bool GroupExists(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == chatGroupId);
    }

    private static ChatGroupStatus GroupStatus(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId).Status;
    }

    private static List<PushMessage> DismissalsTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.IsDismiss)];
    }
}
