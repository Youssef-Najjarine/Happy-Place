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
    public void OfferOlderThanSevenDaysExpiresTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void OfferYoungerThanSevenDaysKeepsTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays - 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void OfferJustOverSevenDaysExpiresTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays) + TimeSpan.FromHours(1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void OfferJustUnderSevenDaysKeepsTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays) - TimeSpan.FromHours(1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Requests Without A Qualifying Offer Persist

    [Fact]
    public void VeryOldRequestWithNoOffersIsNeverExpired() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestTimestamps(seeker.GroupId, TimeSpan.FromDays(30));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RequestWithStaleLastSeenButNoOffersIsNeverExpired() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void OldDeclinedOfferDoesNotExpireTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string helper = CreateUser(container, "Helper");
        CreateOffer(container, helper, seeker.GroupId.ToString());
        DeclineOffer(container, helper, seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void OldWithdrawnOfferDoesNotExpireTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string helper = CreateUser(container, "Helper");
        CreateOffer(container, helper, seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        WithdrawOffer(container, helper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Mixed Offers

    [Fact]
    public void OldOfferedWithRecentOfferedStillExpires() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string oldHelper = CreateUser(container, "Old Helper");
        string recentHelper = CreateUser(container, "Recent Helper");
        CreateOffer(container, oldHelper, seeker.GroupId.ToString());
        SetOfferCreatedAtForHelper(seeker.GroupId, UserAccountId(oldHelper), TimeSpan.FromDays(TtlDays + 1));
        CreateOffer(container, recentHelper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RecentOfferedWithOldDeclinedIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string declinedHelper = CreateUser(container, "Declined Helper");
        string offeringHelper = CreateUser(container, "Offering Helper");
        CreateOffer(container, declinedHelper, seeker.GroupId.ToString());
        DeclineOffer(container, declinedHelper, seeker.GroupId.ToString());
        SetOfferCreatedAtForHelper(seeker.GroupId, UserAccountId(declinedHelper), TimeSpan.FromDays(TtlDays + 1));
        CreateOffer(container, offeringHelper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void RecentOfferedAfterOldWithdrawnIsKept() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string withdrawingHelper = CreateUser(container, "Withdrawing Helper");
        string offeringHelper = CreateUser(container, "Offering Helper");
        CreateOffer(container, withdrawingHelper, seeker.GroupId.ToString());
        SetOfferCreatedAtForHelper(seeker.GroupId, UserAccountId(withdrawingHelper), TimeSpan.FromDays(TtlDays + 1));
        WithdrawOffer(container, withdrawingHelper, seeker.GroupId.ToString());
        CreateOffer(container, offeringHelper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Status Protection

    [Fact]
    public void ActiveGroupWithOldOffersIsNeverExpired() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        Connect(container, seeker.AuthToken, seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
        Assert.Equal(ChatGroupStatus.Active, GroupStatus(seeker.GroupId));
    }

    [Fact]
    public void ConnectingBeforeTheSweepSavesTheRequest() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        Connect(container, seeker.AuthToken, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.True(GroupExists(seeker.GroupId));
        Assert.Equal(ChatGroupStatus.Active, GroupStatus(seeker.GroupId));
    }

    // Tests - Multiplicity And Isolation

    [Fact]
    public void OnlyTheExpiredRequestIsDeleted() {
        using var container = new TestingMockProvidersContainer();
        var expiredSeeker = CreateSeekerRequest(container);
        var freshSeeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Old Helper"), expiredSeeker.GroupId.ToString());
        CreateOffer(container, CreateUser(container, "Recent Helper"), freshSeeker.GroupId.ToString());
        SetOfferCreatedAt(expiredSeeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(expiredSeeker.GroupId));
        Assert.True(GroupExists(freshSeeker.GroupId));
    }

    [Fact]
    public void AllExpiredRequestsAreDeletedTogether() {
        using var container = new TestingMockProvidersContainer();
        var firstSeeker = CreateSeekerRequest(container);
        var secondSeeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "First Helper"), firstSeeker.GroupId.ToString());
        CreateOffer(container, CreateUser(container, "Second Helper"), secondSeeker.GroupId.ToString());
        SetOfferCreatedAt(firstSeeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        SetOfferCreatedAt(secondSeeker.GroupId, TimeSpan.FromDays(TtlDays + 2));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(firstSeeker.GroupId));
        Assert.False(GroupExists(secondSeeker.GroupId));
    }

    [Fact]
    public void ExpiringAnOfferedRequestLeavesANoOfferRequestUntouched() {
        using var container = new TestingMockProvidersContainer();
        var offeredSeeker = CreateSeekerRequest(container);
        var noOfferSeeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), offeredSeeker.GroupId.ToString());
        SetOfferCreatedAt(offeredSeeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        SetRequestTimestamps(noOfferSeeker.GroupId, TimeSpan.FromDays(30));

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(offeredSeeker.GroupId));
        Assert.True(GroupExists(noOfferSeeker.GroupId));
    }

    // Tests - Cascade On Deletion

    [Fact]
    public void ExpiringARequestDeletesItsOffers() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "First Helper"), seeker.GroupId.ToString());
        CreateOffer(container, CreateUser(container, "Second Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Equal(0, OfferRowCount(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestDeletesItsMembership() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Equal(0, MemberRowCount(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestRemovesItsOffersNotificationChannel() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        Assert.True(OffersChannelExists(seeker.GroupId));
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.False(OffersChannelExists(seeker.GroupId));
    }

    // Tests - Sweep Trigger

    [Fact]
    public void ExpiredRequestRemainsUntilAHelperBrowses() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Assert.True(GroupExists(seeker.GroupId));
        Browse(CreateUser(container, "Browser"));
        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void AnyHelperBrowsingTriggersTheExpirySweep() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Offering Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Unrelated Helper"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void UnauthenticatedBrowseDoesNotTriggerExpiry() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        HelpOfferManager.GetOpenRequestsForHelper("not-a-real-token-at-all");

        Assert.True(GroupExists(seeker.GroupId));
    }

    // Tests - Reoffer And Timestamp Semantics

    [Fact]
    public void ReofferingDoesNotResetTheExpiryClock() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string helper = CreateUser(container, "Helper");
        CreateOffer(container, helper, seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        CreateOffer(container, helper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void DecliningThenReofferingKeepsTheOriginalOfferTimestamp() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string helper = CreateUser(container, "Helper");
        CreateOffer(container, helper, seeker.GroupId.ToString());
        DeclineOffer(container, helper, seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        CreateOffer(container, helper, seeker.GroupId.ToString());

        Browse(CreateUser(container, "Browser"));

        Assert.False(GroupExists(seeker.GroupId));
    }

    // Tests - Idempotency

    [Fact]
    public void BrowsingRepeatedlyAfterExpiryIsSafe() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        string browser = CreateUser(container, "Browser");

        Browse(browser);
        Browse(browser);
        Browse(browser);

        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void SweepWithNoRequestsDoesNothing() {
        using var container = new TestingMockProvidersContainer();
        string browser = CreateUser(container, "Browser");

        List<OpenHelpRequest> openRequests = HelpOfferManager.GetOpenRequestsForHelper(browser);

        Assert.Empty(openRequests);
    }

    // Tests - Notification Side Effects

    [Fact]
    public void ExpiringARequestWithALiveOffersChannelDismissesTheSeeker() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        string seekerDeviceToken = "device-" + Guid.NewGuid();
        RegisterDevice(container, seeker.AuthToken, seekerDeviceToken);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        Flush();
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        Browse(CreateUser(container, "Browser"));

        Assert.Single(DismissalsTo(container, seekerDeviceToken));
        Assert.False(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void ExpiringARequestRefreshesTheWaitingCountForAvailableHelpers() {
        using var container = new TestingMockProvidersContainer();
        var availableHelper = AvailableHelperWithDevice(container, "Bystander");
        var seeker = CreateSeekerRequest(container);
        string offeringHelper = CreateUser(container, "Offering Helper");
        CreateOffer(container, offeringHelper, seeker.GroupId.ToString());
        Flush();
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));
        Browse(offeringHelper);

        Flush();

        Assert.Single(DismissalsTo(container, availableHelper.DeviceToken));
        Assert.False(GroupExists(seeker.GroupId));
    }

    // Tests - Feed Contents

    [Fact]
    public void FreshRequestWithNoOffersIsServedInTheFeed() {
        using var container = new TestingMockProvidersContainer();
        CreateSeekerRequest(container);

        int feedLength = OpenRequestsFeedLength(container, CreateUser(container, "Helper"));

        Assert.Equal(1, feedLength);
    }

    [Fact]
    public void StaleRequestWithNoOffersRemainsInTheFeed() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        SetRequestLastSeen(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

        int feedLength = OpenRequestsFeedLength(container, CreateUser(container, "Helper"));

        Assert.Equal(1, feedLength);
        Assert.True(GroupExists(seeker.GroupId));
    }

    [Fact]
    public void ExpiredRequestIsRemovedFromTheFeedOnBrowse() {
        using var container = new TestingMockProvidersContainer();
        var seeker = CreateSeekerRequest(container);
        CreateOffer(container, CreateUser(container, "Helper"), seeker.GroupId.ToString());
        SetOfferCreatedAt(seeker.GroupId, TimeSpan.FromDays(TtlDays + 1));

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

    private static void DeclineOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void WithdrawOffer(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, string chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
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

    private static Guid UserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static void SetOfferCreatedAt(Guid chatGroupId, TimeSpan age) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime createdAt = DateTime.UtcNow - age;
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.CreatedAtUtc, createdAt));
    }

    private static void SetOfferCreatedAtForHelper(Guid chatGroupId, Guid helperUserAccountId, TimeSpan age) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime createdAt = DateTime.UtcNow - age;
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == helperUserAccountId)
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
