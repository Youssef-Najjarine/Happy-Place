using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RegisterDeviceTest {
    // Tests - Registration Authentication Failures

    [Fact]
    public void RegisterDeviceEmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = "", Token = "device-" + Guid.NewGuid(), Platform = "ios" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RegisterDeviceInvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = "not-a-real-token-at-all", Token = "device-" + Guid.NewGuid(), Platform = "ios" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void RegisterDeviceMissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/registerDevice", new { Token = "device-" + Guid.NewGuid(), Platform = "ios" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Registration Stores The Device

    [Fact]
    public void RegisterDeviceThenConnectSendsPushToThatDevice() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken, "ios");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == deviceToken);
    }

    [Fact]
    public void RegisterDeviceStoresPlatform() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Helper " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken, "android");

        using var dbContext = HappyPlaceDbContext.Create();
        DeviceToken stored = dbContext.DeviceTokens.Single(field => field.Token == deviceToken);
        Assert.Equal("android", stored.Platform);
    }

    [Fact]
    public void RegisterSameDeviceTwiceSendsOnlyOnePush() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken, "ios");
        RegisterDevice(container, helperAuthToken, deviceToken, "ios");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == deviceToken);
    }

    [Fact]
    public void RegisterTwoDevicesForHelperSendsPushToBoth() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        string firstDeviceToken = "device-" + Guid.NewGuid();
        string secondDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, firstDeviceToken, "ios");
        RegisterDevice(container, helperAuthToken, secondDeviceToken, "android");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == firstDeviceToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == secondDeviceToken);
    }

    [Fact]
    public void RegisterEmptyDeviceTokenStoresNothing() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        RegisterDevice(container, helperAuthToken, "", "ios");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void RegisterTokenReassignsFromOneHelperToAnother() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string firstHelperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "First Helper " + Guid.NewGuid());
        string secondHelperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Second Helper " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, firstHelperAuthToken, deviceToken, "ios");
        RegisterDevice(container, secondHelperAuthToken, deviceToken, "ios");

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.DoesNotContain(container.PushProvider.SentMessages, message => message.Token == deviceToken);
    }

    // Tests - Unregistration Authentication Failures

    [Fact]
    public void UnregisterDeviceEmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = "", Token = "device-" + Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void UnregisterDeviceInvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = "not-a-real-token-at-all", Token = "device-" + Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void UnregisterDeviceMissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/device/unregisterDevice", new { Token = "device-" + Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Unregistration Removes The Device

    [Fact]
    public void UnregisterDeviceStopsPushes() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, deviceToken, "ios");
        container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = helperAuthToken, Token = deviceToken }).EnsureSuccessStatusCode();

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void UnregisterUnknownTokenSucceeds() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Helper " + Guid.NewGuid());

        HttpResponseMessage response = container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = helperAuthToken, Token = "device-" + Guid.NewGuid() });

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public void UnregisterOnlyRemovesCallersToken() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string ownerHelperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Owner Helper " + Guid.NewGuid());
        string strangerAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Stranger " + Guid.NewGuid());
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, ownerHelperAuthToken, deviceToken, "ios");
        container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = strangerAuthToken, Token = deviceToken }).EnsureSuccessStatusCode();

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.Single(container.PushProvider.SentMessages, message => message.Token == deviceToken);
    }

    [Fact]
    public void UnregisterOneOfTwoDevicesLeavesOther() {
        using var container = new TestingMockProvidersContainer();
        var (seekerAuthToken, chatGroupId) = CreateSeekerWithRequest(container, "I need help");
        string helperAuthToken = CreateHelperWhoOffered(container, chatGroupId, "Helper " + Guid.NewGuid());
        string firstDeviceToken = "device-" + Guid.NewGuid();
        string secondDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, firstDeviceToken, "ios");
        RegisterDevice(container, helperAuthToken, secondDeviceToken, "android");
        container.WebClient.PostJson("api/device/unregisterDevice", new { AuthToken = helperAuthToken, Token = firstDeviceToken }).EnsureSuccessStatusCode();

        Connect(container, seekerAuthToken, chatGroupId);

        Assert.DoesNotContain(container.PushProvider.SentMessages, message => message.Token == firstDeviceToken);
        Assert.Single(container.PushProvider.SentMessages, message => message.Token == secondDeviceToken);
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

    private static void RegisterDevice(TestingMockProvidersContainer container, string authToken, string deviceToken, string platform) {
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = platform }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string seekerAuthToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = seekerAuthToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }
}
