using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class NotificationDispatchTest {
    // Tests - Count Math

    [Fact]
    public void OfferNotificationCountsOfferedOffers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "First"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "Second"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "Third"), seeker.ChatGroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, seeker.DeviceToken).Single();
        Assert.Equal("helpOffers", message.Data["type"]);
        Assert.Equal("3", message.Data["count"]);
    }

    [Fact]
    public void WaitingNotificationCountsProvisionalRequests() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        CreateRequest(container, CreateUser(container, "First Seeker"), "First");
        CreateRequest(container, CreateUser(container, "Second Seeker"), "Second");

        Flush();

        PushMessage message = CountUpdatesTo(container, helper.DeviceToken).Single();
        Assert.Equal("helpWaiting", message.Data["type"]);
        Assert.Equal("2", message.Data["count"]);
    }

    [Fact]
    public void WaitingCountExcludesViewersOwnRequest() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        CreateRequest(container, helper.AuthToken, "My own request");
        CreateRequest(container, CreateUser(container, "Other Seeker"), "Their request");

        Flush();

        PushMessage message = CountUpdatesTo(container, helper.DeviceToken).Single();
        Assert.Equal("1", message.Data["count"]);
    }

    [Fact]
    public void WaitingCountExcludesRequestsTheHelperDeclined() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        string declinedGroupId = CreateRequest(container, CreateUser(container, "Declined Seeker"), "Declined");
        CreateRequest(container, CreateUser(container, "Kept Seeker"), "Kept");
        DeclineOffer(container, helper.AuthToken, declinedGroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, helper.DeviceToken).Last();
        Assert.Equal("1", message.Data["count"]);
    }

    [Fact]
    public void OfferCountIgnoresDeclinedOffers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Offering"), seeker.ChatGroupId);
        string declinerAuthToken = CreateUser(container, "Decliner");
        CreateOffer(container, declinerAuthToken, seeker.ChatGroupId);
        DeclineOffer(container, declinerAuthToken, seeker.ChatGroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, seeker.DeviceToken).Last();
        Assert.Equal("1", message.Data["count"]);
    }

    // Tests - Burst Coalescing

    [Fact]
    public void TwentyOffersCoalesceIntoOneSendWithCountTwenty() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        for (int helperIndex = 0; helperIndex < 20; helperIndex++)
            CreateOffer(container, CreateUser(container, "Helper " + helperIndex), seeker.ChatGroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, seeker.DeviceToken);
        Assert.Single(messages);
        Assert.Equal("20", messages[0].Data["count"]);
    }

    [Fact]
    public void TwentyRequestsCoalesceIntoOneWaitingSendForHelper() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        for (int seekerIndex = 0; seekerIndex < 20; seekerIndex++)
            CreateRequest(container, CreateUser(container, "Seeker " + seekerIndex), "Request " + seekerIndex);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, helper.DeviceToken);
        Assert.Single(messages);
        Assert.Equal("20", messages[0].Data["count"]);
    }

    // Tests - Incremental Updates

    [Fact]
    public void CountUpdatesAcrossTwoBurstsReflectLatestTotal() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "A"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "B"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "C"), seeker.ChatGroupId);
        Flush();
        CreateOffer(container, CreateUser(container, "D"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "E"), seeker.ChatGroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, seeker.DeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("3", messages[0].Data["count"]);
        Assert.Equal("5", messages[1].Data["count"]);
    }

    // Tests - Alert Once

    [Fact]
    public void EachAdditionalOfferAlertsAndADecreaseIsSilent() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string withdrawingHelper = CreateUser(container, "First");
        CreateOffer(container, withdrawingHelper, seeker.ChatGroupId);
        Flush();
        CreateOffer(container, CreateUser(container, "Second"), seeker.ChatGroupId);
        Flush();
        WithdrawOffer(container, withdrawingHelper, seeker.ChatGroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, seeker.DeviceToken);
        Assert.Equal(3, messages.Count);
        Assert.True(messages[0].Alerting);
        Assert.True(messages[1].Alerting);
        Assert.False(messages[2].Alerting);
    }

    [Fact]
    public void OfferReappearingAfterDismissAlertsAgain() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string firstHelper = CreateUser(container, "First");
        CreateOffer(container, firstHelper, seeker.ChatGroupId);
        Flush();
        WithdrawOffer(container, firstHelper, seeker.ChatGroupId);
        Flush();
        CreateOffer(container, CreateUser(container, "Second"), seeker.ChatGroupId);

        Flush();

        List<PushMessage> countUpdates = CountUpdatesTo(container, seeker.DeviceToken);
        Assert.Equal(2, countUpdates.Count);
        Assert.True(countUpdates[0].Alerting);
        Assert.True(countUpdates[1].Alerting);
    }

    // Tests - Dismissal On Zero

    [Fact]
    public void WithdrawingTheLastOfferDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);
        Flush();
        WithdrawOffer(container, helperAuthToken, seeker.ChatGroupId);

        Flush();

        Assert.Single(DismissalsTo(container, seeker.DeviceToken));
    }

    [Fact]
    public void OfferAndWithdrawBeforeAnySendProducesNoNotification() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);
        WithdrawOffer(container, helperAuthToken, seeker.ChatGroupId);

        Flush();

        Assert.Empty(MessagesTo(container, seeker.DeviceToken));
    }

    // Tests - Connect

    [Fact]
    public void ConnectDismissesTheSeekersOffersNotification() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();

        Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        PushMessage dismissal = DismissalsTo(container, seeker.DeviceToken).Single();
        Assert.Equal($"help-offers-{seeker.ChatGroupId}", dismissal.CollapseId);
    }

    [Fact]
    public void ConnectClearsHelpersWaitingNotification() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Bystander Helper");
        string seekerAuthToken = CreateUser(container, "Seeker");
        string chatGroupId = CreateRequest(container, seekerAuthToken, "I need help");
        Flush();
        CreateOffer(container, CreateUser(container, "Offering Helper"), chatGroupId);
        Connect(container, seekerAuthToken, chatGroupId);

        Flush();

        Assert.Single(DismissalsTo(container, helper.DeviceToken));
    }

    // Tests - Cancel

    [Fact]
    public void CancelDismissesTheSeekersOffersNotification() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();

        Cancel(container, seeker.AuthToken, seeker.ChatGroupId);

        PushMessage dismissal = DismissalsTo(container, seeker.DeviceToken).Single();
        Assert.Equal($"help-offers-{seeker.ChatGroupId}", dismissal.CollapseId);
    }

    [Fact]
    public void CancelClearsHelpersWaitingNotification() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        string seekerAuthToken = CreateUser(container, "Seeker");
        string chatGroupId = CreateRequest(container, seekerAuthToken, "I need help");
        Flush();

        Cancel(container, seekerAuthToken, chatGroupId);
        Flush();

        Assert.Single(DismissalsTo(container, helper.DeviceToken));
    }

    // Tests - Fan-Out

    [Fact]
    public void WaitingBroadcastReachesEveryAvailableHelper() {
        using var container = new TestingMockProvidersContainer();
        var firstHelper = AvailableHelperWithDevice(container, "First Helper");
        var secondHelper = AvailableHelperWithDevice(container, "Second Helper");
        var thirdHelper = AvailableHelperWithDevice(container, "Third Helper");
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        Assert.Single(CountUpdatesTo(container, firstHelper.DeviceToken));
        Assert.Single(CountUpdatesTo(container, secondHelper.DeviceToken));
        Assert.Single(CountUpdatesTo(container, thirdHelper.DeviceToken));
    }

    [Fact]
    public void WaitingBroadcastSkipsHelpersWhoWentUnavailable() {
        using var container = new TestingMockProvidersContainer();
        var stayingHelper = AvailableHelperWithDevice(container, "Staying Helper");
        var leavingHelper = AvailableHelperWithDevice(container, "Leaving Helper");
        SetAvailable(container, leavingHelper.AuthToken, false);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        Assert.Single(CountUpdatesTo(container, stayingHelper.DeviceToken));
        Assert.Empty(CountUpdatesTo(container, leavingHelper.DeviceToken));
    }

    [Fact]
    public void OffersNotificationGoesToTheSeekerNotTheHelpers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        string helperDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, helperDeviceToken);
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);

        Flush();

        Assert.Single(CountUpdatesTo(container, seeker.DeviceToken));
        Assert.Empty(MessagesTo(container, helperDeviceToken));
    }

    // Tests - Multi-Device

    [Fact]
    public void WaitingNotificationReachesAllOfAHelpersDevices() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");
        string firstDeviceToken = "device-" + Guid.NewGuid();
        string secondDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, firstDeviceToken, "ios");
        RegisterDevice(container, helperAuthToken, secondDeviceToken, "android");
        SetAvailable(container, helperAuthToken, true);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        PushMessage firstMessage = CountUpdatesTo(container, firstDeviceToken).Single();
        PushMessage secondMessage = CountUpdatesTo(container, secondDeviceToken).Single();
        Assert.Equal(firstMessage.CollapseId, secondMessage.CollapseId);
    }

    [Fact]
    public void OffersNotificationReachesAllOfASeekersDevices() {
        using var container = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(container, "Seeker");
        string firstDeviceToken = "device-" + Guid.NewGuid();
        string secondDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seekerAuthToken, firstDeviceToken, "ios");
        RegisterDevice(container, seekerAuthToken, secondDeviceToken, "android");
        string chatGroupId = CreateRequest(container, seekerAuthToken, "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), chatGroupId);

        Flush();

        Assert.Single(CountUpdatesTo(container, firstDeviceToken));
        Assert.Single(CountUpdatesTo(container, secondDeviceToken));
    }

    // Tests - Channel Isolation

    [Fact]
    public void OffersOnOneRequestDoNotNotifyAnotherSeeker() {
        using var container = new TestingMockProvidersContainer();
        var firstSeeker = SeekerWithDeviceAndRequest(container, "First Seeker", "First request");
        var secondSeeker = SeekerWithDeviceAndRequest(container, "Second Seeker", "Second request");
        CreateOffer(container, CreateUser(container, "Helper"), firstSeeker.ChatGroupId);

        Flush();

        Assert.Single(CountUpdatesTo(container, firstSeeker.DeviceToken));
        Assert.Empty(MessagesTo(container, secondSeeker.DeviceToken));
    }

    [Fact]
    public void TwoSeekersOfferCountsAreIndependent() {
        using var container = new TestingMockProvidersContainer();
        var firstSeeker = SeekerWithDeviceAndRequest(container, "First Seeker", "First request");
        var secondSeeker = SeekerWithDeviceAndRequest(container, "Second Seeker", "Second request");
        CreateOffer(container, CreateUser(container, "A"), firstSeeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "B"), firstSeeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "C"), secondSeeker.ChatGroupId);

        Flush();

        Assert.Equal("2", CountUpdatesTo(container, firstSeeker.DeviceToken).Single().Data["count"]);
        Assert.Equal("1", CountUpdatesTo(container, secondSeeker.DeviceToken).Single().Data["count"]);
    }

    // Tests - Availability Transitions

    [Fact]
    public void HelperGoingAvailableReceivesTheCurrentWaitingCount() {
        using var container = new TestingMockProvidersContainer();
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");
        var helper = AvailableHelperWithDevice(container, "Late Helper");

        Flush();

        PushMessage message = CountUpdatesTo(container, helper.DeviceToken).Single();
        Assert.Equal("1", message.Data["count"]);
    }

    [Fact]
    public void HelperGoingUnavailableStopsReceivingWaitingUpdates() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        CreateRequest(container, CreateUser(container, "First Seeker"), "First");
        Flush();
        SetAvailable(container, helper.AuthToken, false);
        CreateRequest(container, CreateUser(container, "Second Seeker"), "Second");

        Flush();

        Assert.Single(CountUpdatesTo(container, helper.DeviceToken));
    }

    // Tests - Lifecycle Decrements

    [Fact]
    public void WithdrawDecrementsTheOfferCount() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string withdrawingHelper = CreateUser(container, "Withdrawing");
        CreateOffer(container, withdrawingHelper, seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "Staying"), seeker.ChatGroupId);
        Flush();
        WithdrawOffer(container, withdrawingHelper, seeker.ChatGroupId);

        Flush();

        Assert.Equal("1", CountUpdatesTo(container, seeker.DeviceToken).Last().Data["count"]);
    }

    [Fact]
    public void DeclineDecrementsTheOfferCount() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string decliningHelper = CreateUser(container, "Declining");
        CreateOffer(container, decliningHelper, seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "Staying"), seeker.ChatGroupId);
        Flush();
        DeclineOffer(container, decliningHelper, seeker.ChatGroupId);

        Flush();

        Assert.Equal("1", CountUpdatesTo(container, seeker.DeviceToken).Last().Data["count"]);
    }

    // Tests - Change Detection

    [Fact]
    public void RepeatedOfferFromSameHelperCountsOnce() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, seeker.DeviceToken).Single();
        Assert.Equal("1", message.Data["count"]);
    }

    [Fact]
    public void ResweepWithUnchangedCountSendsNothing() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);
        Flush();
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);

        Flush();

        Assert.Single(CountUpdatesTo(container, seeker.DeviceToken));
    }

    // Tests - No-Op Sweep

    [Fact]
    public void SweepWithNoDirtyChannelsSendsNothing() {
        using var container = new TestingMockProvidersContainer();
        SeekerWithDeviceAndRequest(container, "Seeker", "I need help");

        Flush();

        Assert.Empty(container.PushProvider.SentMessages);
    }

    // Tests - Resilience

    [Fact]
    public void OneFailingDeviceDoesNotBlockOtherRecipients() {
        using var container = new TestingMockProvidersContainer();
        var failingHelper = AvailableHelperWithDevice(container, "Failing Helper");
        var healthyHelper = AvailableHelperWithDevice(container, "Healthy Helper");
        container.PushProvider.FailToken(failingHelper.DeviceToken);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        Assert.Single(CountUpdatesTo(container, healthyHelper.DeviceToken));
        Assert.Empty(MessagesTo(container, failingHelper.DeviceToken));
    }

    [Fact]
    public void InvalidTokenIsRemovedWhenWaitingPushIsSent() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        container.PushProvider.InvalidateToken(helper.DeviceToken);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.DeviceTokens.Any(field => field.Token == helper.DeviceToken));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static void RegisterDevice(TestingMockProvidersContainer container, string authToken, string deviceToken, string platform = "ios") {
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = platform }).EnsureSuccessStatusCode();
    }

    private static string CreateRequest(TestingMockProvidersContainer container, string authToken, string topic) {
        return container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = authToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
    }

    private static void CreateOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void DeclineOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void WithdrawOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void Cancel(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }

    private static (string AuthToken, string DeviceToken) AvailableHelperWithDevice(TestingMockProvidersContainer container, string name) {
        string authToken = CreateUser(container, name);
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, authToken, deviceToken);
        SetAvailable(container, authToken, true);
        return (authToken, deviceToken);
    }

    private static (string AuthToken, string DeviceToken, string ChatGroupId) SeekerWithDeviceAndRequest(TestingMockProvidersContainer container, string name, string topic) {
        string authToken = CreateUser(container, name);
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, authToken, deviceToken);
        string chatGroupId = CreateRequest(container, authToken, topic);
        return (authToken, deviceToken, chatGroupId);
    }

    // Helpers - Sweeping

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

    private static List<PushMessage> MessagesTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken)];
    }

    private static List<PushMessage> CountUpdatesTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && !message.IsDismiss)];
    }

    private static List<PushMessage> DismissalsTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.IsDismiss)];
    }
}
