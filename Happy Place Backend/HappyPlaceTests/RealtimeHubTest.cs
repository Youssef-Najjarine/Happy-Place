using System.Collections.Concurrent;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Realtime;
using HappyWorld.HappyPlace.Web.Hubs;
using HappyWorld.HappyPlace.Web.Services;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimeHubTest {
    // Fields

    private static readonly string HubUrl = "http://localhost/hubs/realtime";
    private static readonly int EventWaitTimeoutMs = 5000;
    private static readonly int SilenceWindowMs = 750;

    // Tests - Authentication

    [Fact]
    public void AuthenticateWithValidTokenReturnsAuthenticated() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Hub User");
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);

        string authenticateStatus = Authenticate(hubConnection, authToken);

        Assert.Equal("authenticated", authenticateStatus);
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void AuthenticateWithInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);

        string authenticateStatus = Authenticate(hubConnection, "not-a-real-token");

        Assert.Equal("unauthorized", authenticateStatus);
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void AuthenticateWithEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);

        string authenticateStatus = Authenticate(hubConnection, "");

        Assert.Equal("unauthorized", authenticateStatus);
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void SetListeningBeforeAuthenticateReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);

        string setListeningStatus = SetListening(hubConnection, true);

        Assert.Equal("unauthorized", setListeningStatus);
        DisposeHubConnection(hubConnection);
    }

    // Tests - Delivery

    [Fact]
    public void AuthenticatedConnectionReceivesUserTargetedEvent() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Hub User");
        Guid userAccountId = HelpParticipant.ResolveUserAccountId(authToken).Value;
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = CaptureEvents(hubConnection, RealtimePublisher.FriendsChangedEventName);
        AuthenticateOrFail(hubConnection, authToken);

        RealtimePublisher.PublishFriendsChanged(userAccountId);

        Dictionary<string, string> receivedPayload = WaitForEvent(receivedPayloads);
        Assert.NotNull(receivedPayload);
        Assert.Empty(receivedPayload);
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void UnauthenticatedConnectionReceivesNoUserEvents() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Hub User");
        Guid userAccountId = HelpParticipant.ResolveUserAccountId(authToken).Value;
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = CaptureEvents(hubConnection, RealtimePublisher.FriendsChangedEventName);

        RealtimePublisher.PublishFriendsChanged(userAccountId);

        Assert.True(NoEventArrives(receivedPayloads));
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void EventsAreIsolatedPerUser() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string firstAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "First User");
        string secondAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Second User");
        Guid firstUserAccountId = HelpParticipant.ResolveUserAccountId(firstAuthToken).Value;
        HubConnection firstHubConnection = ConnectHub(testingMockProvidersContainer);
        HubConnection secondHubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> firstReceivedPayloads = CaptureEvents(firstHubConnection, RealtimePublisher.FriendsChangedEventName);
        ConcurrentQueue<Dictionary<string, string>> secondReceivedPayloads = CaptureEvents(secondHubConnection, RealtimePublisher.FriendsChangedEventName);
        AuthenticateOrFail(firstHubConnection, firstAuthToken);
        AuthenticateOrFail(secondHubConnection, secondAuthToken);

        RealtimePublisher.PublishFriendsChanged(firstUserAccountId);

        Assert.NotNull(WaitForEvent(firstReceivedPayloads));
        Assert.True(NoEventArrives(secondReceivedPayloads));
        DisposeHubConnection(firstHubConnection);
        DisposeHubConnection(secondHubConnection);
    }

    [Fact]
    public void ChatGroupChangedReachesActiveMembersWithPayload() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string ownerAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = HelpParticipant.ResolveUserAccountId(ownerAuthToken).Value;
        Guid memberUserAccountId = HelpParticipant.ResolveUserAccountId(memberAuthToken).Value;
        List<Guid> memberUserAccountIds = [ownerUserAccountId, memberUserAccountId];
        Guid chatGroupId = SeedActiveGroup(memberUserAccountIds, ownerUserAccountId);
        HubConnection ownerHubConnection = ConnectHub(testingMockProvidersContainer);
        HubConnection memberHubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> ownerReceivedPayloads = CaptureEvents(ownerHubConnection, RealtimePublisher.ChatGroupChangedEventName);
        ConcurrentQueue<Dictionary<string, string>> memberReceivedPayloads = CaptureEvents(memberHubConnection, RealtimePublisher.ChatGroupChangedEventName);
        AuthenticateOrFail(ownerHubConnection, ownerAuthToken);
        AuthenticateOrFail(memberHubConnection, memberAuthToken);

        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);

        Dictionary<string, string> ownerReceivedPayload = WaitForEvent(ownerReceivedPayloads);
        Dictionary<string, string> memberReceivedPayload = WaitForEvent(memberReceivedPayloads);
        Assert.NotNull(ownerReceivedPayload);
        Assert.NotNull(memberReceivedPayload);
        Assert.Equal(chatGroupId.ToString(), memberReceivedPayload["chatGroupId"]);
        Assert.Equal(RealtimePublisher.MessagesKind, memberReceivedPayload["kind"]);
        DisposeHubConnection(ownerHubConnection);
        DisposeHubConnection(memberHubConnection);
    }

    // Tests - Listening Lifecycle

    [Fact]
    public void ListeningConnectionReceivesOpenRequestsBroadcast() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = CaptureEvents(hubConnection, RealtimePublisher.HelpChangedEventName);
        AuthenticateOrFail(hubConnection, authToken);
        string setListeningStatus = SetListening(hubConnection, true);
        Assert.Equal("ok", setListeningStatus);

        RealtimePublisher.PublishHelpOpenRequestsChanged();

        Assert.NotNull(WaitForEvent(receivedPayloads));
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void StoppedListeningConnectionStopsReceivingBroadcasts() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper");
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = CaptureEvents(hubConnection, RealtimePublisher.HelpChangedEventName);
        AuthenticateOrFail(hubConnection, authToken);
        SetListening(hubConnection, true);
        RealtimePublisher.PublishHelpOpenRequestsChanged();
        Assert.NotNull(WaitForEvent(receivedPayloads));

        SetListening(hubConnection, false);
        RealtimePublisher.PublishHelpOpenRequestsChanged();

        Assert.True(NoEventArrives(receivedPayloads));
        DisposeHubConnection(hubConnection);
    }

    [Fact]
    public void ReauthenticatingAsDifferentUserLeavesPreviousUserGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        InstallSignalRSender(testingMockProvidersContainer);
        string firstAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "First User");
        string secondAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Second User");
        Guid firstUserAccountId = HelpParticipant.ResolveUserAccountId(firstAuthToken).Value;
        Guid secondUserAccountId = HelpParticipant.ResolveUserAccountId(secondAuthToken).Value;
        HubConnection hubConnection = ConnectHub(testingMockProvidersContainer);
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = CaptureEvents(hubConnection, RealtimePublisher.FriendsChangedEventName);
        AuthenticateOrFail(hubConnection, firstAuthToken);
        RealtimePublisher.PublishFriendsChanged(firstUserAccountId);
        Assert.NotNull(WaitForEvent(receivedPayloads));

        AuthenticateOrFail(hubConnection, secondAuthToken);
        RealtimePublisher.PublishFriendsChanged(firstUserAccountId);
        Assert.True(NoEventArrives(receivedPayloads));

        RealtimePublisher.PublishFriendsChanged(secondUserAccountId);
        Assert.NotNull(WaitForEvent(receivedPayloads));
        DisposeHubConnection(hubConnection);
    }

    // Helpers

    private static HubConnection ConnectHub(TestingMockProvidersContainer testingMockProvidersContainer) {
        HubConnection hubConnection = new HubConnectionBuilder()
            .WithUrl(HubUrl, HttpTransportType.LongPolling, options => {
                options.HttpMessageHandlerFactory = defaultHandler => testingMockProvidersContainer.WebClient.CreateServerHandler();
            })
            .Build();
        hubConnection.StartAsync().GetAwaiter().GetResult();
        return hubConnection;
    }

    private static string Authenticate(HubConnection hubConnection, string authToken) {
        return hubConnection.InvokeAsync<string>("Authenticate", authToken).GetAwaiter().GetResult();
    }

    private static void AuthenticateOrFail(HubConnection hubConnection, string authToken) {
        string authenticateStatus = Authenticate(hubConnection, authToken);
        Assert.Equal("authenticated", authenticateStatus);
    }

    private static string SetListening(HubConnection hubConnection, bool isListening) {
        return hubConnection.InvokeAsync<string>("SetListening", isListening).GetAwaiter().GetResult();
    }

    private static void InstallSignalRSender(TestingMockProvidersContainer testingMockProvidersContainer) {
        IHubContext<RealtimeHub> hubContext = testingMockProvidersContainer.WebClient.Services.GetRequiredService<IHubContext<RealtimeHub>>();
        SignalRRealtimeSender signalRRealtimeSender = new(hubContext);
        RealtimeSender.SetInitializer(() => signalRRealtimeSender);
    }

    private static ConcurrentQueue<Dictionary<string, string>> CaptureEvents(HubConnection hubConnection, string eventName) {
        ConcurrentQueue<Dictionary<string, string>> receivedPayloads = new();
        hubConnection.On<Dictionary<string, string>>(eventName, receivedPayload => receivedPayloads.Enqueue(receivedPayload));
        return receivedPayloads;
    }

    private static Dictionary<string, string> WaitForEvent(ConcurrentQueue<Dictionary<string, string>> receivedPayloads) {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(EventWaitTimeoutMs);
        while (DateTime.UtcNow < deadline) {
            if (receivedPayloads.TryDequeue(out Dictionary<string, string> receivedPayload))
                return receivedPayload;
            Thread.Sleep(25);
        }
        return null;
    }

    private static bool NoEventArrives(ConcurrentQueue<Dictionary<string, string>> receivedPayloads) {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(SilenceWindowMs);
        while (DateTime.UtcNow < deadline) {
            if (!receivedPayloads.IsEmpty)
                return false;
            Thread.Sleep(25);
        }
        return true;
    }

    private static void DisposeHubConnection(HubConnection hubConnection) {
        hubConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static Guid SeedActiveGroup(List<Guid> memberUserAccountIds, Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid chatGroupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "Realtime Hub Group", OwnerUserAccountId = ownerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        foreach (Guid memberUserAccountId in memberUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = memberUserAccountId, MemberRole = memberUserAccountId == ownerUserAccountId ? ChatGroupMemberRole.Owner : ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return chatGroupId;
    }
}
