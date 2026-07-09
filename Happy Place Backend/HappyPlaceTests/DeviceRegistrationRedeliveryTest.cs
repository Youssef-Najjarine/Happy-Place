using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class DeviceRegistrationRedeliveryTest {
    // Tests - Zero-Device Sweeps Preserve State

    [Fact]
    public void SweepWithNoDevicesLeavesTheChannelUnsent() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);

        Flush();

        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupGuid = Guid.Parse(seeker.ChatGroupId);
        NotificationChannel channel = dbContext.NotificationChannels.Single(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == chatGroupGuid);
        Assert.Equal(0, channel.LastSentCount);
        Assert.False(channel.IsLive);
    }

    // Tests - Late Registration Delivers Missed State

    [Fact]
    public void OfferArrivingBeforeSeekerHasADeviceIsDeliveredOnceTheDeviceRegisters() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, deviceToken);

        Flush();

        PushMessage message = CountUpdatesTo(container, deviceToken).Single();
        Assert.Equal("helpOffers", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
        Assert.True(message.Alerting);
    }

    [Fact]
    public void WaitingCountArrivingBeforeHelperHasADeviceIsDeliveredOnceTheDeviceRegisters() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");
        SetAvailable(container, helperAuthToken, true);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");
        Flush();
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken);

        Flush();

        PushMessage message = CountUpdatesTo(container, deviceToken).Single();
        Assert.Equal("helpWaiting", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
    }

    // Tests - Token Rotation

    [Fact]
    public void RotatedDeviceTokenRecoversTheMissedNotificationAfterReregistration() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "First Helper"), seeker.ChatGroupId);
        Flush();
        container.PushProvider.InvalidateToken(seeker.DeviceToken);
        CreateOffer(container, CreateUser(container, "Second Helper"), seeker.ChatGroupId);
        Flush();
        string rotatedDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, rotatedDeviceToken);

        Flush();

        PushMessage message = CountUpdatesTo(container, rotatedDeviceToken).Single();
        Assert.Equal("2", message.Data["count"]);
        Assert.True(message.Alerting);
        Assert.Equal("1", CountUpdatesTo(container, seeker.DeviceToken).Single().Data["count"]);
    }

    [Fact]
    public void StaleLiveChannelSendsDismissalToTheNewDeviceAfterRegistration() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);
        Flush();
        container.PushProvider.InvalidateToken(seeker.DeviceToken);
        WithdrawOffer(container, helperAuthToken, seeker.ChatGroupId);
        Flush();
        string rotatedDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, rotatedDeviceToken);

        Flush();

        Assert.Single(DismissalsTo(container, rotatedDeviceToken));
    }

    // Tests - Token Reassignment

    [Fact]
    public void TokenReassignedToANewAccountDeliversThatAccountsPendingNotification() {
        using var container = new TestingMockProvidersContainer();
        string previousOwnerAuthToken = CreateUser(container, "Previous Owner");
        string sharedDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, previousOwnerAuthToken, sharedDeviceToken);
        var seeker = SeekerWithRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();
        RegisterDevice(container, seeker.AuthToken, sharedDeviceToken);

        Flush();

        PushMessage message = CountUpdatesTo(container, sharedDeviceToken).Single();
        Assert.Equal("helpOffers", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
    }

    [Fact]
    public void WaitingPushFollowsTheDeviceWhenTokenReassignsToANewAvailableHelper() {
        using var container = new TestingMockProvidersContainer();
        string firstHelperAuthToken = CreateUser(container, "First Helper");
        string sharedDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, firstHelperAuthToken, sharedDeviceToken);
        Guid firstHelperUserAccountId = LookupDeviceOwner(sharedDeviceToken);
        SetAvailable(container, firstHelperAuthToken, true);
        string secondHelperAuthToken = CreateUser(container, "Second Helper");
        RegisterDevice(container, secondHelperAuthToken, sharedDeviceToken);
        SetAvailable(container, secondHelperAuthToken, true);
        CreateRequest(container, CreateUser(container, "Seeker"), "I need help");

        Flush();

        PushMessage message = CountUpdatesTo(container, sharedDeviceToken).Single();
        Assert.Equal("helpWaiting", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
        using var dbContext = HappyPlaceDbContext.Create();
        NotificationChannel firstHelperChannel = dbContext.NotificationChannels.Single(field => field.Kind == NotificationChannelKind.Waiting && field.RecipientUserAccountId == firstHelperUserAccountId);
        Assert.Equal(0, firstHelperChannel.LastSentCount);
        Assert.False(firstHelperChannel.IsLive);
    }

    [Fact]
    public void HelperWhoseDeviceWasReassignedAwayRecoversWhenTheDeviceReturns() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken);
        SetAvailable(container, helperAuthToken, true);
        CreateRequest(container, CreateUser(container, "First Seeker"), "First");
        Flush();
        string strangerAuthToken = CreateUser(container, "Stranger");
        RegisterDevice(container, strangerAuthToken, deviceToken);
        CreateRequest(container, CreateUser(container, "Second Seeker"), "Second");
        Flush();
        RegisterDevice(container, helperAuthToken, deviceToken);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, deviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[0].Data["count"]);
        Assert.Equal("2", messages[1].Data["count"]);
    }

    // Tests - Second Device

    [Fact]
    public void SecondDeviceRegisteredWhileNotificationIsLiveReceivesTheCurrentCount() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();
        string secondDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, secondDeviceToken);

        Flush();

        Assert.Equal("1", CountUpdatesTo(container, secondDeviceToken).Single().Data["count"]);
        List<PushMessage> firstDeviceMessages = CountUpdatesTo(container, seeker.DeviceToken);
        Assert.Equal(2, firstDeviceMessages.Count);
        Assert.Equal("1", firstDeviceMessages[1].Data["count"]);
    }

    // Tests - Heartbeat Safety

    [Fact]
    public void HeartbeatReregistrationOfTheSameTokenDoesNotResend() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Flush();
        RegisterDevice(container, seeker.AuthToken, seeker.DeviceToken);

        Flush();

        Assert.Single(CountUpdatesTo(container, seeker.DeviceToken));
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

    private static void WithdrawOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }

    private static Guid LookupDeviceOwner(string deviceToken) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.DeviceTokens.Single(field => field.Token == deviceToken).UserAccountId;
    }

    private static (string AuthToken, string ChatGroupId) SeekerWithRequest(TestingMockProvidersContainer container, string name, string topic) {
        string authToken = CreateUser(container, name);
        string chatGroupId = CreateRequest(container, authToken, topic);
        return (authToken, chatGroupId);
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

    private static List<PushMessage> CountUpdatesTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && !message.IsDismiss)];
    }

    private static List<PushMessage> DismissalsTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.IsDismiss)];
    }
}
