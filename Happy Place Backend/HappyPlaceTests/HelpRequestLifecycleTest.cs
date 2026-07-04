using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HelpRequestLifecycleTest {
    // Tests - Create And Dedup

    [Fact]
    public void CreateRequestTwiceForOneSeekerReusesTheSameProvisionalGroup() {
        using var container = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(container, "Seeker");
        string firstGroupId = CreateRequest(container, seekerAuthToken, "First topic");
        string secondGroupId = CreateRequest(container, seekerAuthToken, "Second topic");

        Assert.Equal(firstGroupId, secondGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count(field => field.Status == ChatGroupStatus.Provisional));
    }

    [Fact]
    public void ConcurrentCreateRequestsForOneSeekerLeaveExactlyOneProvisionalGroup() {
        using var container = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(container, "Seeker");
        ConcurrentBag<Exception> errors = [];
        ConcurrentBag<string> groupIds = [];
        List<Thread> threads = [];
        for (int index = 0; index < 8; index++) {
            int captured = index;
            threads.Add(new Thread(() => {
                try { groupIds.Add(CreateRequest(container, seekerAuthToken, "Topic " + captured)); }
                catch (Exception error) { errors.Add(error); }
            }));
        }
        foreach (Thread thread in threads)
            thread.Start();
        foreach (Thread thread in threads)
            thread.Join();

        Assert.Empty(errors);
        Assert.Single(groupIds.Distinct());
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count(field => field.Status == ChatGroupStatus.Provisional));
    }

    // Tests - Cancel

    [Fact]
    public void CancelDeletesTheGroupAndCascadesMembersAndOffers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);

        Cancel(container, seeker.AuthToken, seeker.ChatGroupId);

        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.ChatGroups.Any(field => field.Id == groupId));
        Assert.False(dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId));
        Assert.False(dbContext.HelpOffers.Any(field => field.ChatGroupId == groupId));
    }

    [Fact]
    public void CancelIsRejectedForAnAlreadyActiveGroup() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        string cancelStatus = Cancel(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("none", cancelStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Active, dbContext.ChatGroups.Single(field => field.Id == groupId).Status);
    }

    [Fact]
    public void CancelIsRejectedForANonOwner() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string strangerAuthToken = CreateUser(container, "Stranger");

        string cancelStatus = Cancel(container, strangerAuthToken, seeker.ChatGroupId);

        Assert.Equal("none", cancelStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == groupId).Status);
    }

    // Tests - Connect

    [Fact]
    public void ConnectWithNoOffersKeepsTheGroupProvisional() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");

        string connectStatus = Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("noOffers", connectStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == groupId).Status);
    }

    [Fact]
    public void ConnectWithAnOfferActivatesTheGroupAndInvitesTheHelper() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        string helperDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, helperDeviceToken);
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);

        string connectStatus = Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("connected", connectStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Active, dbContext.ChatGroups.Single(field => field.Id == groupId).Status);
        Assert.Single(InvitesTo(container, helperDeviceToken));
    }

    [Fact]
    public void ConnectIsIdempotentAndInvitesTheHelperOnlyOnce() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string helperAuthToken = CreateUser(container, "Helper");
        string helperDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, helperAuthToken, helperDeviceToken);
        CreateOffer(container, helperAuthToken, seeker.ChatGroupId);

        string firstStatus = Connect(container, seeker.AuthToken, seeker.ChatGroupId);
        string secondStatus = Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("connected", firstStatus);
        Assert.Equal("connected", secondStatus);
        Assert.Single(InvitesTo(container, helperDeviceToken));
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.ChatGroups.Count(field => field.Id == groupId && field.Status == ChatGroupStatus.Active));
    }

    [Fact]
    public void ConnectAfterCancelReturnsNoneAndLeavesNoGroup() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Cancel(container, seeker.AuthToken, seeker.ChatGroupId);

        string connectStatus = Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("none", connectStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.ChatGroups.Any(field => field.Id == groupId));
    }

    [Fact]
    public void ConnectByANonOwnerReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        string strangerAuthToken = CreateUser(container, "Stranger");

        string connectStatus = Connect(container, strangerAuthToken, seeker.ChatGroupId);

        Assert.Equal("none", connectStatus);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(ChatGroupStatus.Provisional, dbContext.ChatGroups.Single(field => field.Id == groupId).Status);
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentConnectAndCancelLeaveAConsistentState() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        ConcurrentBag<Exception> errors = [];

        Thread connectThread = new(() => {
            try { Connect(container, seeker.AuthToken, seeker.ChatGroupId); }
            catch (Exception error) { errors.Add(error); }
        });
        Thread cancelThread = new(() => {
            try { Cancel(container, seeker.AuthToken, seeker.ChatGroupId); }
            catch (Exception error) { errors.Add(error); }
        });
        connectThread.Start();
        cancelThread.Start();
        connectThread.Join();
        cancelThread.Join();

        Assert.Empty(errors);
        Guid groupId = Guid.Parse(seeker.ChatGroupId);
        using var dbContext = HappyPlaceDbContext.Create();
        List<ChatGroup> groups = [.. dbContext.ChatGroups.Where(field => field.Id == groupId)];
        Assert.True(groups.Count <= 1);
        if (groups.Count == 1) {
            Assert.Equal(ChatGroupStatus.Active, groups[0].Status);
        }
        else {
            Assert.False(dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId));
            Assert.False(dbContext.HelpOffers.Any(field => field.ChatGroupId == groupId));
        }
    }

    // Tests - Poll Status

    [Fact]
    public void PollReturnsWaitingWithZeroReadyForAFreshRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");

        (string status, int readyHelperCount) = Poll(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("waiting", status);
        Assert.Equal(0, readyHelperCount);
    }

    [Fact]
    public void PollReflectsTheNumberOfOfferingHelpers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "First"), seeker.ChatGroupId);
        CreateOffer(container, CreateUser(container, "Second"), seeker.ChatGroupId);

        (string status, int readyHelperCount) = Poll(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("waiting", status);
        Assert.Equal(2, readyHelperCount);
    }

    [Fact]
    public void PollReturnsConnectedAfterTheGroupIsActivated() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        CreateOffer(container, CreateUser(container, "Helper"), seeker.ChatGroupId);
        Connect(container, seeker.AuthToken, seeker.ChatGroupId);

        (string status, _) = Poll(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("connected", status);
    }

    [Fact]
    public void PollReturnsNoneAfterCancel() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        Cancel(container, seeker.AuthToken, seeker.ChatGroupId);

        (string status, _) = Poll(container, seeker.AuthToken, seeker.ChatGroupId);

        Assert.Equal("none", status);
    }

    [Fact]
    public void PollByANonOwnerReturnsNone() {
        using var container = new TestingMockProvidersContainer();
        var seeker = SeekerWithDeviceAndRequest(container, "Seeker", "I need help");
        string strangerAuthToken = CreateUser(container, "Stranger");

        (string status, _) = Poll(container, strangerAuthToken, seeker.ChatGroupId);

        Assert.Equal("none", status);
    }

    // Tests - My Open Request

    [Fact]
    public void MyOpenRequestReturnsTheProvisionalThenNoneAfterCancel() {
        using var container = new TestingMockProvidersContainer();
        string seekerAuthToken = CreateUser(container, "Seeker");
        string chatGroupId = CreateRequest(container, seekerAuthToken, "I need help");

        (string beforeStatus, string beforeGroupId) = MyOpenRequest(container, seekerAuthToken);
        Cancel(container, seekerAuthToken, chatGroupId);
        (string afterStatus, _) = MyOpenRequest(container, seekerAuthToken);

        Assert.Equal("waiting", beforeStatus);
        Assert.Equal(chatGroupId, beforeGroupId);
        Assert.Equal("none", afterStatus);
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

    private static string Connect(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        return container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();
    }

    private static string Cancel(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        return container.WebClient.PostJson("api/helpRequest/cancel", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();
    }

    private static (string Status, int ReadyHelperCount) Poll(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        JsonElement root = container.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement;
        string status = root.GetProperty("status").GetString();
        int readyHelperCount = root.TryGetProperty("readyHelperCount", out JsonElement countElement) && countElement.ValueKind == JsonValueKind.Number ? countElement.GetInt32() : 0;
        return (status, readyHelperCount);
    }

    private static (string Status, string ChatGroupId) MyOpenRequest(TestingMockProvidersContainer container, string authToken) {
        JsonElement root = container.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement;
        string status = root.GetProperty("status").GetString();
        string chatGroupId = root.TryGetProperty("chatGroupId", out JsonElement idElement) && idElement.ValueKind == JsonValueKind.String ? idElement.GetString() : null;
        return (status, chatGroupId);
    }

    private static (string AuthToken, string DeviceToken, string ChatGroupId) SeekerWithDeviceAndRequest(TestingMockProvidersContainer container, string name, string topic) {
        string authToken = CreateUser(container, name);
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, authToken, deviceToken);
        string chatGroupId = CreateRequest(container, authToken, topic);
        return (authToken, deviceToken, chatGroupId);
    }

    // Helpers - Asserting

    private static List<PushMessage> InvitesTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.Data != null && message.Data.ContainsKey("type") && message.Data["type"] == "invite")];
    }
}
