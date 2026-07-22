using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SeekerHelperExclusivityTest {
    // Tests - Creating A Request Ends Helper Mode

    [Fact]
    public void CreateRequestClearsTheCallersAvailability() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        SetAvailable(container, person, true);

        CreateRequest(container, person, "I need help now");

        Assert.False(IsAvailableInDb(AccountId(person)));
    }

    [Fact]
    public void GetAvailabilityReadsFalseAfterCreatingARequest() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        SetAvailable(container, person, true);
        CreateRequest(container, person, "I need help now");

        JsonElement root = GetAvailabilityJson(container, person);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public void CreateRequestWithdrawsTheCallersOutstandingOffers() {
        using var container = new TestingMockProvidersContainer();
        Guid otherRequest = CreateRequest(container, CreateGuest(container), "Someone else");
        string person = CreateGuest(container);
        CreateOffer(container, person, otherRequest);

        CreateRequest(container, person, "I need help too");

        Assert.Equal(0, OfferedCount(otherRequest));
    }

    [Fact]
    public void CreateRequestLeavesOtherHelpersOffersUntouched() {
        using var container = new TestingMockProvidersContainer();
        Guid otherRequest = CreateRequest(container, CreateGuest(container), "Someone else");
        string leavingHelper = CreateGuest(container);
        string stayingHelper = CreateGuest(container);
        CreateOffer(container, leavingHelper, otherRequest);
        CreateOffer(container, stayingHelper, otherRequest);

        CreateRequest(container, leavingHelper, "I need help too");

        Assert.Equal(1, OfferedCount(otherRequest));
        Assert.True(HasOfferedFromHelper(otherRequest, AccountId(stayingHelper)));
    }

    [Fact]
    public void CreateRequestLeavesTheCallersConnectedOffersAlone() {
        using var container = new TestingMockProvidersContainer();
        string otherSeeker = CreateGuest(container);
        Guid otherRequest = CreateRequest(container, otherSeeker, "Someone else");
        string person = CreateGuest(container);
        CreateOffer(container, person, otherRequest);
        Connect(container, otherSeeker, otherRequest);

        CreateRequest(container, person, "I need help too");

        Assert.Equal(1, ConnectedCount(otherRequest));
    }

    [Fact]
    public void CreateRequestTearsDownTheCallersWaitingChannel() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        SetAvailable(container, person, true);

        CreateRequest(container, person, "I need help now");

        Assert.False(WaitingChannelExists(AccountId(person)));
    }

    [Fact]
    public void CreateRequestDismissesALiveWaitingNotification() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        string personDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, person, personDeviceToken);
        SetAvailable(container, person, true);
        CreateRequest(container, CreateGuest(container), "Someone else");
        Flush();

        CreateRequest(container, person, "I need help too");

        Assert.Single(DismissalsTo(container, personDeviceToken));
    }

    [Fact]
    public void RecreatingAnExistingRequestStillClearsAvailability() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        CreateRequest(container, person, "I need help now");
        Guid personUserAccountId = AccountId(person);
        using (var seedContext = HappyPlaceDbContext.Create()) {
            seedContext.HelpAvailabilities.Add(new() { Id = Guid.NewGuid(), HelperUserAccountId = personUserAccountId, IsAvailable = true, LastSeenAtUtc = DateTime.UtcNow });
            seedContext.SaveChanges();
        }

        CreateRequest(container, person, "I need help now");

        Assert.False(IsAvailableInDb(personUserAccountId));
    }

    // Tests - Going Available While Seeking Is Refused

    [Fact]
    public void SetAvailabilityTrueWhileSeekingReturnsSeeking() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        CreateRequest(container, person, "I need help now");

        JsonElement root = SetAvailabilityJson(container, person, true);

        Assert.Equal("seeking", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public void SetAvailabilityTrueWhileSeekingCreatesNoAvailabilityRow() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        CreateRequest(container, person, "I need help now");

        SetAvailabilityJson(container, person, true);

        Assert.Equal(0, AvailabilityRowCount(AccountId(person)));
    }

    [Fact]
    public void SetAvailabilityTrueWhileSeekingLeavesAnExistingRowUnavailable() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        SetAvailable(container, person, true);
        CreateRequest(container, person, "I need help now");

        SetAvailabilityJson(container, person, true);

        Assert.False(IsAvailableInDb(AccountId(person)));
    }

    [Fact]
    public void SetAvailabilityTrueWhileSeekingCreatesNoWaitingChannel() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        CreateRequest(container, person, "I need help now");

        SetAvailabilityJson(container, person, true);

        Assert.False(WaitingChannelExists(AccountId(person)));
    }

    [Fact]
    public void SetAvailabilityFalseWhileSeekingStillSucceeds() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        CreateRequest(container, person, "I need help now");

        JsonElement root = SetAvailabilityJson(container, person, false);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("isAvailable").GetBoolean());
    }

    // Tests - Helper Mode Resumes After Seeking Ends

    [Fact]
    public void SetAvailabilityTrueAfterCancelSucceeds() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        Guid ownRequest = CreateRequest(container, person, "I need help now");
        Cancel(container, person, ownRequest);

        JsonElement root = SetAvailabilityJson(container, person, true);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.True(IsAvailableInDb(AccountId(person)));
    }

    [Fact]
    public void SetAvailabilityTrueAfterConnectSucceeds() {
        using var container = new TestingMockProvidersContainer();
        string person = CreateGuest(container);
        Guid ownRequest = CreateRequest(container, person, "I need help now");
        CreateOffer(container, CreateGuest(container), ownRequest);
        Connect(container, person, ownRequest);

        JsonElement root = SetAvailabilityJson(container, person, true);

        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.True(IsAvailableInDb(AccountId(person)));
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

    private static void Connect(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void Cancel(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }

    private static JsonElement SetAvailabilityJson(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        return container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement GetAvailabilityJson(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/helpAvailability/getAvailability", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static bool IsAvailableInDb(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        HelpAvailability availability = dbContext.HelpAvailabilities.SingleOrDefault(field => field.HelperUserAccountId == helperUserAccountId);
        return availability != null && availability.IsAvailable;
    }

    private static int AvailabilityRowCount(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpAvailabilities.Count(field => field.HelperUserAccountId == helperUserAccountId);
    }

    private static bool WaitingChannelExists(Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.Waiting && field.RecipientUserAccountId == helperUserAccountId);
    }

    private static int OfferedCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Offered);
    }

    private static int ConnectedCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId && field.Status == HelpOfferStatus.Connected);
    }

    private static bool HasOfferedFromHelper(Guid chatGroupId, Guid helperUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Any(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId && field.Status == HelpOfferStatus.Offered);
    }

    private static List<PushMessage> DismissalsTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.IsDismiss)];
    }
}
