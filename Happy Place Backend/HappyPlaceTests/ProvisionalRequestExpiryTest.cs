using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ProvisionalRequestExpiryTest {
    // Fields

    private static readonly int TtlDays = 7;

    // Tests - Threshold

    [Fact]
    public void RequestUntouchedForOverSevenDaysExpires() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RequestTouchedWithinSevenDaysIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays - 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RequestJustOverTheThresholdExpires() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays) + TimeSpan.FromHours(1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RequestJustUnderTheThresholdIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays) - TimeSpan.FromHours(1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Seeker Activity Keeps A Request Alive

    [Fact]
    public void PollingRefreshesAnOldRequestAndPreventsExpiry() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        PollRequest(container, seeker.AuthToken, seeker.GroupId);

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void MyOpenRequestRefreshesAnOldRequestAndPreventsExpiry() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        MyOpenRequest(container, seeker.AuthToken);

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RecreatingTheRequestRefreshesItAndPreventsExpiry() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        CreateRequest(container, seeker.AuthToken, "Still here");

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void ARequestCreatedLongAgoButRecentlyTouchedIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestTimestamps(seeker.GroupId, TimeSpan.FromDays(30));
        PollRequest(container, seeker.AuthToken, seeker.GroupId);

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Offers No Longer Govern Expiry

    [Fact]
    public void AnActivelySeenRequestWithAStaleOfferIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void AnAbandonedRequestWithARecentOfferStillExpires() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void AnAbandonedRequestWithNoOffersExpires() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    // Tests - Status Protection

    [Fact]
    public void AnActiveGroupWithAStaleLastSeenIsNeverExpired() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        Connect(container, seeker.AuthToken, seeker.GroupId.ToString());
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(30));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
        Assert.Equal(ChatGroupStatus.Active, GroupStatus(seeker.GroupId));
    }

    // Tests - Multiplicity And Isolation

    [Fact]
    public void OnlyTheAbandonedRequestIsDeleted() {
        using var container = new TestingMockProvidersContainer();
        var abandonedSeeker = CreateSeekerRequest(container);
        var freshSeeker = CreateSeekerRequest(container);
        SetRequestLastSeen(abandonedSeeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(abandonedSeeker.GroupId));
        Assert.True(GroupExists(freshSeeker.GroupId));
    }

    [Fact]
    public void AllAbandonedRequestsAreDeletedTogether() {
        using var container = new TestingMockProvidersContainer();
        var firstSeeker = CreateSeekerRequest(container);
        var secondSeeker = CreateSeekerRequest(container);
        SetRequestLastSeen(firstSeeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        SetRequestLastSeen(secondSeeker.GroupId, TimeSpan.FromDays(TtlDays + 2));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(firstSeeker.GroupId));
        Assert.False(GroupExists(secondSeeker.GroupId));
    }

    // Tests - Cascade On Deletion

    [Fact]
    public void ExpiringARequestDeletesItsOffers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "First Helper"), seeker.GroupId.ToString());
        CreateOffer(container, CreateUser(container, "Second Helper"), seeker.GroupId.ToString());
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Equal(0, OfferRowCount(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestDeletesItsMembership() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Equal(0, MemberRowCount(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestRemovesItsOffersNotificationChannel() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(OffersChannelExists(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestWithALiveOffersChannelDismissesTheSeeker() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string seekerDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, seekerDeviceToken);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        Flush();
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Single(DismissalsTo(container, seekerDeviceToken));
    }

    [Fact]
    public void ExpiringARequestRefreshesTheWaitingCountForAvailableHelpers() {
        using var container = new TestingMockProvidersContainer();
        var helper = AvailableHelperWithDevice(container, "Helper");
        var seeker = CreateSeekerRequest(container);
        Flush();
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));
        Flush();

        Assert.Single(DismissalsTo(container, helper.DeviceToken));
    }

    // Tests - Trigger Mechanics

    [Fact]
    public void AnAbandonedRequestRemainsUntilSomeoneBrowses() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void UnauthenticatedBrowseDoesNotTriggerExpiry() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse("not-a-real-token-at-all");

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void BrowsingRepeatedlyAfterExpiryIsSafe() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "First Browser"));
        Browse(CreateUser(container, "Second Browser"));
        Browse(CreateUser(container, "Third Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void SweepWithNoRequestsDoesNothing() {
        using var container = new TestingMockProvidersContainer();

        int feedLength = OpenRequestsFeedLength(container, CreateUser(container, "Browser"));

        Assert.Equal(0, feedLength);
    }

    // Tests - Feed Behavior

    [Fact]
    public void AFreshRequestWithNoOffersIsServedInTheFeed() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);

        int feedLength = OpenRequestsFeedLength(container, CreateUser(container, "Browser"));

        Assert.Equal(1, feedLength);
        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void AnAbandonedRequestIsRemovedFromTheFeedOnBrowse() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        int feedLength = OpenRequestsFeedLength(container, CreateUser(container, "Browser"));

        Assert.Equal(0, feedLength);
        Assert.False(GroupExists(seeker.GroupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static SeekerRequest CreateSeekerRequest(TestingMockProvidersContainer container) {
        string authToken = CreateUser(container, "Seeker");
        string groupId = CreateRequest(container, authToken, "I need help");
        return new SeekerRequest { AuthToken = authToken, GroupId = Guid.Parse(groupId) };
    }

    private sealed class SeekerRequest {
        public string AuthToken { get; init; }
        public Guid GroupId { get; init; }
    }

    private static string CreateRequest(TestingMockProvidersContainer container, string authToken, string topic) {
        return container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = authToken, Topic = topic }).ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
    }

    private static int OpenRequestsFeedLength(TestingMockProvidersContainer container, string helperAuthToken) {
        return container.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken }).ReadContentAsJsonDocument().RootElement.GetArrayLength();
    }

    private static void CreateOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void PollRequest(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/pollRequest", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void MyOpenRequest(TestingMockProvidersContainer container, string authToken) {
        container.WebClient.PostJson("api/helpRequest/myOpenRequest", new { AuthToken = authToken }).EnsureSuccessStatusCode();
    }

    private static void RegisterDevice(TestingMockProvidersContainer container, string authToken, string deviceToken) {
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = authToken, Token = deviceToken, Platform = "ios" }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }

    private static AvailableHelper AvailableHelperWithDevice(TestingMockProvidersContainer container, string name) {
        string authToken = CreateUser(container, name);
        string deviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, authToken, deviceToken);
        SetAvailable(container, authToken, true);
        return new AvailableHelper { AuthToken = authToken, DeviceToken = deviceToken };
    }

    private sealed class AvailableHelper {
        public string AuthToken { get; init; }
        public string DeviceToken { get; init; }
    }

    private static void Browse(string helperAuthToken) {
        HelpOfferManager.GetOpenRequestsForHelper(helperAuthToken);
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

    // Helpers - Aging

    private static void SetOfferCreatedAt(Guid chatGroupId, TimeSpan age) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime createdAt = DateTime.UtcNow - age;
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.CreatedAtUtc, createdAt));
    }

    private static void SetRequestTimestamps(Guid chatGroupId, TimeSpan age) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime timestamp = DateTime.UtcNow - age;
        dbContext.ChatGroups
            .Where(field => field.Id == chatGroupId)
            .ExecuteUpdate(setters => setters
                .SetProperty(field => field.CreatedAtUtc, timestamp)
                .SetProperty(field => field.LastSeenAtUtc, timestamp));
    }

    private static void SetRequestLastSeen(Guid chatGroupId, TimeSpan age) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime timestamp = DateTime.UtcNow - age;
        dbContext.ChatGroups
            .Where(field => field.Id == chatGroupId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.LastSeenAtUtc, timestamp));
    }

    // Helpers - Asserting

    private static bool GroupExists(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Any(field => field.Id == chatGroupId);
    }

    private static ChatGroupStatus GroupStatus(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == chatGroupId).Status;
    }

    private static int OfferRowCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.HelpOffers.Count(field => field.ChatGroupId == chatGroupId);
    }

    private static int MemberRowCount(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == chatGroupId);
    }

    private static bool OffersChannelExists(Guid chatGroupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == chatGroupId);
    }

    private static List<PushMessage> DismissalsTo(TestingMockProvidersContainer container, string deviceToken) {
        return [.. container.PushProvider.SentMessages.Where(message => message.Token == deviceToken && message.IsDismiss)];
    }
}
