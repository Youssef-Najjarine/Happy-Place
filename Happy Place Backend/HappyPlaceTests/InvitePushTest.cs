using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class InvitePushTest {
    // Tests - Invite Fan-Out

    [Fact]
    public void ConnectSendsOneInvitePushPerInvitedHelper() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string firstToken = RegisteredHelperWhoOffered(container, chatGroupId, "First Helper " + Guid.NewGuid());
        string secondToken = RegisteredHelperWhoOffered(container, chatGroupId, "Second Helper " + Guid.NewGuid());
        string thirdToken = RegisteredHelperWhoOffered(container, chatGroupId, "Third Helper " + Guid.NewGuid());

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Equal(3, container.PushProvider.SentMessages.Count());
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == firstToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == secondToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == thirdToken);
    }

    [Fact]
    public void ConnectSendsNoPushToHelperWithoutDevice() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void ConnectSendsPushesOnlyToHelpersWithDevices() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string twoDeviceHelperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Two Device Helper " + Guid.NewGuid());
        string firstToken = "device-" + Guid.NewGuid();
        string secondToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, twoDeviceHelperAuthToken, firstToken, "ios");
        RegisterDevice(container, twoDeviceHelperAuthToken, secondToken, "android");
        CreateHelperWhoOffered(container, chatGroupId, "No Device Helper " + Guid.NewGuid());
        string oneDeviceHelperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "One Device Helper " + Guid.NewGuid());
        string thirdToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, oneDeviceHelperAuthToken, thirdToken, "ios");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Equal(3, container.PushProvider.SentMessages.Count());
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == firstToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == secondToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == thirdToken);
    }

    [Fact]
    public void ConnectDoesNotPushDeclinedHelper() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string offeringToken = RegisteredHelperWhoOffered(container, chatGroupId, "Offering Helper " + Guid.NewGuid());
        string declinerAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Declining Helper " + Guid.NewGuid());
        string declinerToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, declinerAuthToken, declinerToken, "ios");
        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = declinerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == offeringToken);
        Assert.DoesNotContain(container.PushProvider.SentMessages, message => message.Token == declinerToken);
    }

    [Fact]
    public void ConnectWithNoOffersSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void ConnectTwiceDoesNotResendPush() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        RegisteredHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        Connect(container, seekerAuthToken, chatGroupId);
        int countAfterFirstConnect = container.PushProvider.SentMessages.Count();

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Equal(1, countAfterFirstConnect);
        Assert.Equal(countAfterFirstConnect, container.PushProvider.SentMessages.Count());
    }

    [Fact]
    public void ConnectDoesNotPushTheSeeker() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string seekerToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seekerAuthToken, seekerToken, "ios");
        string helperToken = RegisteredHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == helperToken);
        Assert.DoesNotContain(container.PushProvider.SentMessages, message => message.Token == seekerToken);
    }

    // Tests - Invite Payload

    [Fact]
    public void InvitePushHasInviteTypeAndChatGroup() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string deviceToken = RegisteredHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());

        Connect(container, seekerAuthToken, chatGroupId);

        PushMessage message = container.PushProvider.SentMessages.Single(field => field.Token == deviceToken);
        Assert.Equal("invite", message.Data["type"]);
        Assert.Equal(chatGroupId, message.Data["chatGroupId"]);
        Assert.True(message.Data.ContainsKey("chatGroupName"));
    }

    // Tests - Other Actions Send No Push

    [Fact]
    public void CreateOfferSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "device-" + Guid.NewGuid(), "ios");

        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void DeclineOfferSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "device-" + Guid.NewGuid(), "ios");

        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void WithdrawOfferSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (_, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "device-" + Guid.NewGuid(), "ios");

        container.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void CancelRequestSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "device-" + Guid.NewGuid(), "ios");

        container.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void JoinGroupSendsNoPush() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "device-" + Guid.NewGuid(), "ios");
        Connect(container, seekerAuthToken, chatGroupId);
        int countAfterConnect = container.PushProvider.SentMessages.Count();

        container.WebClient.PostJson("api/helpOffer/join", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();

        Assert.Equal(countAfterConnect, container.PushProvider.SentMessages.Count());
    }

    // Tests - Resilience

    [Fact]
    public void ConnectStillNotifiesOthersWhenOnePushFails() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string failingToken = RegisteredHelperWhoOffered(container, chatGroupId, "Failing Helper " + Guid.NewGuid());
        string healthyToken = RegisteredHelperWhoOffered(container, chatGroupId, "Healthy Helper " + Guid.NewGuid());
        container.PushProvider.FailToken(failingToken);

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == healthyToken);
        Assert.DoesNotContain(container.PushProvider.SentMessages, message => message.Token == failingToken);
    }

    [Fact]
    public void ConnectRemovesInvalidTokenAndNotifiesOthers() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string invalidHelperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Invalid Helper " + Guid.NewGuid());
        string invalidToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, invalidHelperAuthToken, invalidToken, "ios");
        string healthyToken = RegisteredHelperWhoOffered(container, chatGroupId, "Healthy Helper " + Guid.NewGuid());
        container.PushProvider.InvalidateToken(invalidToken);

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == healthyToken);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.DeviceTokens.Any(field => field.Token == invalidToken));
    }

    // Helpers

    private static (string AuthToken, string ChatGroupId) CreateSeekerWithRequest(TestingMockProvidersContainer container, string topic) {
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Seeker " + Guid.NewGuid());
        string chatGroupId = container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return (seekerAuthToken, chatGroupId);
    }

    private static string CreateHelperWhoOffered(TestingMockProvidersContainer container, string chatGroupId, string helperName) {
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, helperName);
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = helperAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
        return helperAuthToken;
    }

    private static string RegisteredHelperWhoOffered(TestingMockProvidersContainer container, string chatGroupId, string helperName) {
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, helperName);
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken, "ios");
        return deviceToken;
    }

    private static void RegisterDevice(TestingMockProvidersContainer container, string authToken, string deviceToken, string platform) {
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = platform }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string seekerAuthToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }
}
