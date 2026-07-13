using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class JoinRequestNotificationTest {
    // Tests - Requesting Alerts The Owner

    [Fact]
    public void RequestToJoinAlertsTheOwnerWithACount() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "My Private Group");
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, owner.DeviceToken).Single();
        Assert.Equal("joinRequests", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
        Assert.Equal(owner.GroupId.ToString(), message.Data["chatGroupId"]);
        Assert.Equal($"join-requests-{owner.GroupId}", message.CollapseId);
        Assert.True(message.Alerting);
        Assert.Contains("My Private Group", message.Body);
    }

    [Fact]
    public void ThreeRapidRequestsCoalesceIntoOneNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        RequestToJoin(container, CreateUser(container, "First"), owner.GroupId);
        RequestToJoin(container, CreateUser(container, "Second"), owner.GroupId);
        RequestToJoin(container, CreateUser(container, "Third"), owner.GroupId);

        Flush();

        PushMessage message = CountUpdatesTo(container, owner.DeviceToken).Single();
        Assert.Equal("3", message.Data["count"]);
        Assert.True(message.Alerting);
    }

    [Fact]
    public void DuplicateRequestDoesNotIncrementTheCount() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, owner.GroupId);
        RequestToJoin(container, requesterAuthToken, owner.GroupId);

        Flush();

        Assert.Equal("1", CountUpdatesTo(container, owner.DeviceToken).Single().Data["count"]);
    }

    // Tests - Decrements Are Passive

    [Fact]
    public void CancelDecrementsTheCountWithoutAlerting() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string firstRequesterAuthToken = CreateUser(container, "First");
        RequestToJoin(container, firstRequesterAuthToken, owner.GroupId);
        RequestToJoin(container, CreateUser(container, "Second"), owner.GroupId);
        Flush();
        CancelJoinRequest(container, firstRequesterAuthToken, owner.GroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, owner.DeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
    }

    [Fact]
    public void ApprovingDecrementsTheOwnersCount() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string firstRequesterAuthToken = CreateUser(container, "First");
        Guid firstRequesterUserAccountId = ResolveUserAccountId(firstRequesterAuthToken);
        RequestToJoin(container, firstRequesterAuthToken, owner.GroupId);
        RequestToJoin(container, CreateUser(container, "Second"), owner.GroupId);
        Flush();
        ApproveMember(container, owner.AuthToken, owner.GroupId, firstRequesterUserAccountId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, owner.DeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.False(messages[1].Alerting);
    }

    // Tests - Dismissal When The Last Request Resolves

    [Fact]
    public void RejectingTheLastRequestDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, owner.GroupId);
        Flush();
        RejectMember(container, owner.AuthToken, owner.GroupId, ResolveUserAccountId(requesterAuthToken));

        Flush();

        PushMessage dismissal = DismissalsTo(container, owner.DeviceToken).Single();
        Assert.Equal($"join-requests-{owner.GroupId}", dismissal.CollapseId);
    }

    [Fact]
    public void CancellingTheLastRequestDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, owner.GroupId);
        Flush();
        CancelJoinRequest(container, requesterAuthToken, owner.GroupId);

        Flush();

        Assert.Single(DismissalsTo(container, owner.DeviceToken));
    }

    [Fact]
    public void ReRequestAfterCancelAlertsAgain() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string requesterAuthToken = CreateUser(container, "Requester");
        RequestToJoin(container, requesterAuthToken, owner.GroupId);
        Flush();
        CancelJoinRequest(container, requesterAuthToken, owner.GroupId);
        Flush();
        RequestToJoin(container, requesterAuthToken, owner.GroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, owner.DeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages[1].Data["count"]);
        Assert.True(messages[1].Alerting);
    }

    // Tests - Targeting And Scoping

    [Fact]
    public void RequesterAndMembersDoNotReceiveTheOwnersNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string memberAuthToken = CreateUser(container, "Member");
        string memberDeviceToken = RegisterNewDevice(container, memberAuthToken);
        AddActiveMember(owner.GroupId, ResolveUserAccountId(memberAuthToken));
        string requesterAuthToken = CreateUser(container, "Requester");
        string requesterDeviceToken = RegisterNewDevice(container, requesterAuthToken);
        RequestToJoin(container, requesterAuthToken, owner.GroupId);

        Flush();

        Assert.Single(CountUpdatesTo(container, owner.DeviceToken));
        Assert.Empty(CountUpdatesTo(container, memberDeviceToken));
        Assert.Empty(CountUpdatesTo(container, requesterDeviceToken));
    }

    [Fact]
    public void RequestsForTwoGroupsProduceIndependentNotifications() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        string ownerDeviceToken = RegisterNewDevice(container, ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "First Group", false);
        Guid secondGroupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Second Group", false);
        RequestToJoin(container, CreateUser(container, "First Requester"), firstGroupId);
        RequestToJoin(container, CreateUser(container, "Second Requester"), secondGroupId);
        RequestToJoin(container, CreateUser(container, "Third Requester"), secondGroupId);

        Flush();

        List<PushMessage> messages = CountUpdatesTo(container, ownerDeviceToken);
        Assert.Equal(2, messages.Count);
        Assert.Equal("1", messages.Single(message => message.CollapseId == $"join-requests-{firstGroupId}").Data["count"]);
        Assert.Equal("2", messages.Single(message => message.CollapseId == $"join-requests-{secondGroupId}").Data["count"]);
    }

    [Fact]
    public void MakingGroupPublicKeepsThePendingNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);
        Flush();
        SetVisibility(container, owner.AuthToken, owner.GroupId, true);

        Flush();

        Assert.Single(CountUpdatesTo(container, owner.DeviceToken));
        Assert.Empty(DismissalsTo(container, owner.DeviceToken));
    }

    // Tests - Group Lifecycle Teardown

    [Fact]
    public void GroupDeleteDismissesALiveJoinRequestNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);
        Flush();

        DeleteGroup(container, owner.AuthToken, owner.GroupId);

        Assert.Single(DismissalsTo(container, owner.DeviceToken));
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == owner.GroupId));
    }

    [Fact]
    public void OwnerLeaveMakePublicDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);
        Flush();

        Leave(container, owner.AuthToken, owner.GroupId, "makePublic");
        Flush();

        Assert.Single(DismissalsTo(container, owner.DeviceToken));
        Assert.Single(CountUpdatesTo(container, owner.DeviceToken));
    }

    [Fact]
    public void OwnerLeaveDeleteDismissesTheNotification() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);
        Flush();

        Leave(container, owner.AuthToken, owner.GroupId, "delete");

        Assert.Single(DismissalsTo(container, owner.DeviceToken));
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == owner.GroupId));
    }

    [Fact]
    public void OwnerLeaveWithTransferMovesTheNotificationToTheNewOwner() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string successorAuthToken = CreateUser(container, "Successor");
        string successorDeviceToken = RegisterNewDevice(container, successorAuthToken);
        AddActiveMember(owner.GroupId, ResolveUserAccountId(successorAuthToken));
        RequestToJoin(container, CreateUser(container, "Requester"), owner.GroupId);
        Flush();

        Leave(container, owner.AuthToken, owner.GroupId, null);
        Flush();

        Assert.Single(DismissalsTo(container, owner.DeviceToken));
        PushMessage successorMessage = CountUpdatesTo(container, successorDeviceToken).Single();
        Assert.Equal("joinRequests", successorMessage.Data["type"]);
        Assert.Equal("1", successorMessage.Data["count"]);
        Assert.True(successorMessage.Alerting);
    }

    // Tests - Delivery Resilience

    [Fact]
    public void ZeroDeviceOwnerRecoversTheNotificationOnRegistration() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Private Group", false);
        RequestToJoin(container, CreateUser(container, "Requester"), groupId);
        Flush();
        string ownerDeviceToken = RegisterNewDevice(container, ownerAuthToken);

        Flush();

        PushMessage message = CountUpdatesTo(container, ownerDeviceToken).Single();
        Assert.Equal("joinRequests", message.Data["type"]);
        Assert.Equal("1", message.Data["count"]);
    }

    // Tests - Channel Integrity

    [Fact]
    public void ConcurrentRequestsCreateExactlyOneChannel() {
        using var container = new TestingMockProvidersContainer();
        var owner = OwnerWithDeviceAndPrivateGroup(container, "Private Group");
        string firstRequesterAuthToken = CreateUser(container, "First");
        string secondRequesterAuthToken = CreateUser(container, "Second");
        string thirdRequesterAuthToken = CreateUser(container, "Third");
        string fourthRequesterAuthToken = CreateUser(container, "Fourth");

        List<Exception> exceptions = RunConcurrently(
            () => RequestToJoin(container, firstRequesterAuthToken, owner.GroupId),
            () => RequestToJoin(container, secondRequesterAuthToken, owner.GroupId),
            () => RequestToJoin(container, thirdRequesterAuthToken, owner.GroupId),
            () => RequestToJoin(container, fourthRequesterAuthToken, owner.GroupId));
        Flush();

        Assert.Empty(exceptions);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.NotificationChannels.Count(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == owner.GroupId));
        Assert.Equal("4", CountUpdatesTo(container, owner.DeviceToken).Single().Data["count"]);
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

    private static void CancelJoinRequest(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/chatGroup/cancelJoinRequest", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void ApproveMember(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        container.WebClient.PostJson("api/chatGroup/approveMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void RejectMember(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        container.WebClient.PostJson("api/chatGroup/rejectMember", new { AuthToken = authToken, ChatGroupId = chatGroupId, MemberUserAccountId = memberUserAccountId }).EnsureSuccessStatusCode();
    }

    private static void SetVisibility(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, bool isPublic) {
        container.WebClient.PostJson("api/chatGroup/setVisibility", new { AuthToken = authToken, ChatGroupId = chatGroupId, IsPublic = isPublic }).EnsureSuccessStatusCode();
    }

    private static void DeleteGroup(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void Leave(TestingMockProvidersContainer container, string authToken, Guid chatGroupId, string disposition) {
        container.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = authToken, ChatGroupId = chatGroupId, Disposition = disposition }).EnsureSuccessStatusCode();
    }

    private static OwnerContext OwnerWithDeviceAndPrivateGroup(TestingMockProvidersContainer container, string groupName) {
        string ownerAuthToken = CreateUser(container, "Owner");
        string ownerDeviceToken = RegisterNewDevice(container, ownerAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), groupName, false);
        return new OwnerContext(ownerAuthToken, ownerDeviceToken, groupId);
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

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
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

    // Records

    private record OwnerContext(string AuthToken, string DeviceToken, Guid GroupId);
}
