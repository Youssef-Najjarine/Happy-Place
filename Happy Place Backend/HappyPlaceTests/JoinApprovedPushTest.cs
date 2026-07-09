using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class JoinApprovedPushTest {
    // Tests - Approval Notifies The Requester

    [Fact]
    public void ApprovalSendsAPushToTheRequester() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Sunday Hikers", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, groupId);

        ApproveMember(container, ownerAuthToken, groupId, ResolveUserAccountId(requesterAuthToken));

        PushMessage message = MessagesTo(container, requesterDeviceToken).Single();
        Assert.Equal("joinApproved", message.Data["type"]);
        Assert.Equal(groupId.ToString(), message.Data["chatGroupId"]);
        Assert.Equal($"join-approved-{groupId}", message.CollapseId);
        Assert.True(message.Alerting);
        Assert.Contains("Sunday Hikers", message.Body);
    }

    [Fact]
    public void ApprovalPushReachesAllOfTheRequestersDevices() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string firstDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        string secondDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, groupId);

        ApproveMember(container, ownerAuthToken, groupId, ResolveUserAccountId(requesterAuthToken));

        Assert.Single(MessagesTo(container, firstDeviceToken));
        Assert.Single(MessagesTo(container, secondDeviceToken));
    }

    [Fact]
    public void ApprovalPushDoesNotGoToTheOwner() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        string ownerDeviceToken = RegisterNewDevice(container, ownerAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, groupId);

        ApproveMember(container, ownerAuthToken, groupId, ResolveUserAccountId(requesterAuthToken));

        Assert.Empty(MessagesTo(container, ownerDeviceToken));
    }

    // Tests - No Push On Rejection

    [Fact]
    public void RejectionSendsNoPushToTheRequester() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, groupId);

        RejectMember(container, ownerAuthToken, groupId, ResolveUserAccountId(requesterAuthToken));

        Assert.Empty(MessagesTo(container, requesterDeviceToken));
    }

    // Tests - Idempotency And Resilience

    [Fact]
    public void DoubleApproveSendsExactlyOnePush() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, groupId);
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);

        ApproveMember(container, ownerAuthToken, groupId, requesterUserAccountId);
        ApproveMember(container, ownerAuthToken, groupId, requesterUserAccountId);

        Assert.Single(MessagesTo(container, requesterDeviceToken));
    }

    [Fact]
    public void ApprovingADevicelessRequesterStillApproves() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, groupId);
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);

        ApproveMember(container, ownerAuthToken, groupId, requesterUserAccountId);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == requesterUserAccountId && field.Status == ChatGroupMemberStatus.Active));
    }

    [Fact]
    public void InvalidRequesterTokenIsRemovedAndDoesNotBlockApproval() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        container.PushProvider.InvalidateToken(requesterDeviceToken);
        RequestToJoin(container, requesterAuthToken, groupId);
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);

        ApproveMember(container, ownerAuthToken, groupId, requesterUserAccountId);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == requesterUserAccountId && field.Status == ChatGroupMemberStatus.Active));
        Assert.False(dbContext.DeviceTokens.Any(field => field.Token == requesterDeviceToken));
    }

    [Fact]
    public void ConcurrentApproveAndCancelSendAtMostOnePush() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, groupId);
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);

        List<Exception> exceptions = RunConcurrently(
            () => container.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MemberUserAccountId = requesterUserAccountId }).EnsureSuccessStatusCode(),
            () => container.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode());

        Assert.Empty(exceptions);
        Assert.True(MessagesTo(container, requesterDeviceToken).Count <= 1);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static string RegisterNewDevice(TestingMockProvidersContainer container, string authToken) {
        string deviceToken = "device-" + Guid.NewGuid();
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = "ios" }).EnsureSuccessStatusCode();
        return deviceToken;
    }

    private static void RequestToJoin(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void ApproveMember(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        container.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void RejectMember(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        container.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static List<Exception> RunConcurrently(params Action[] actions) {
        List<Exception> caughtExceptions = [];
        List<Thread> threads = [];
        foreach (Action action in actions) {
            Thread thread = new(() => {
                try { action(); }
                catch (Exception exception) { lock (caughtExceptions) { caughtExceptions.Add(exception); } }
            });
            threads.Add(thread);
        }
        foreach (Thread thread in threads) thread.Start();
        foreach (Thread thread in threads) thread.Join();
        return caughtExceptions;
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    // Helpers - Asserting

    private static List<PushMessage> MessagesTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken)];
    }
}
