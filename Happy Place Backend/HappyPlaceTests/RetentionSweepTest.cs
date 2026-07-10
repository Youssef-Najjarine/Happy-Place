using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.PushNotifications;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RetentionSweepTest {
    // Tests - Device Token Pruning

    [Fact]
    public void DeviceTokenUnseenPastRetentionIsPruned() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Dormant User");
        string staleDeviceToken = SeedDeviceToken(userAccountId, DateTime.UtcNow.AddDays(-31));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.DeviceTokens.Any(field => field.Token == staleDeviceToken));
    }

    [Fact]
    public void RecentlySeenDeviceTokenIsKept() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Active User");
        string freshDeviceToken = SeedDeviceToken(userAccountId, DateTime.UtcNow.AddDays(-29));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.DeviceTokens.Any(field => field.Token == freshDeviceToken));
    }

    [Fact]
    public void PruningADeviceDoesNotTouchTheAccount() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Dormant User");
        SeedDeviceToken(userAccountId, DateTime.UtcNow.AddDays(-40));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.UserAccounts.Any(field => field.Id == userAccountId));
    }

    // Tests - Orphaned Waiting Channels

    [Fact]
    public void OrphanedWaitingChannelIsRemovedWithoutSendingAnything() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Ghost Helper");
        SeedWaitingChannel(userAccountId, DateTime.UtcNow.AddDays(-31));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.NotificationChannels.Any(field => field.RecipientUserAccountId == userAccountId));
        Assert.Empty(container.PushProvider.SentMessages);
    }

    [Fact]
    public void WaitingChannelWithALiveDeviceIsKept() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Equipped Helper");
        SeedDeviceToken(userAccountId, DateTime.UtcNow.AddDays(-5));
        SeedWaitingChannel(userAccountId, DateTime.UtcNow.AddDays(-60));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.NotificationChannels.Any(field => field.RecipientUserAccountId == userAccountId && field.Kind == NotificationChannelKind.Waiting));
    }

    [Fact]
    public void WaitingChannelWithRecentActivityIsKept() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Recently Active Helper");
        SeedWaitingChannel(userAccountId, DateTime.UtcNow.AddDays(-5));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.NotificationChannels.Any(field => field.RecipientUserAccountId == userAccountId && field.Kind == NotificationChannelKind.Waiting));
    }

    [Fact]
    public void OffersAndJoinRequestChannelsAreNeverTouched() {
        using var container = new TestingMockProvidersContainer();
        Guid seekerUserAccountId = SeedUser("Deviceless Seeker");
        Guid ownerUserAccountId = SeedUser("Deviceless Owner");
        Guid offersGroupId = SeedGroupRow(seekerUserAccountId);
        Guid joinGroupId = SeedGroupRow(ownerUserAccountId);
        SeedChannel(seekerUserAccountId, NotificationChannelKind.Offers, offersGroupId, DateTime.UtcNow.AddDays(-90));
        SeedChannel(ownerUserAccountId, NotificationChannelKind.JoinRequests, joinGroupId, DateTime.UtcNow.AddDays(-90));

        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.Offers && field.ScopeChatGroupId == offersGroupId));
        Assert.True(dbContext.NotificationChannels.Any(field => field.Kind == NotificationChannelKind.JoinRequests && field.ScopeChatGroupId == joinGroupId));
    }

    // Tests - Resilience

    [Fact]
    public void RetentionSweepIsIdempotent() {
        using var container = new TestingMockProvidersContainer();
        Guid userAccountId = SeedUser("Dormant User");
        SeedDeviceToken(userAccountId, DateTime.UtcNow.AddDays(-45));
        SeedWaitingChannel(userAccountId, DateTime.UtcNow.AddDays(-45));

        NotificationDispatchManager.RunRetentionSweep();
        NotificationDispatchManager.RunRetentionSweep();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.DeviceTokens.Any(field => field.UserAccountId == userAccountId));
        Assert.False(dbContext.NotificationChannels.Any(field => field.RecipientUserAccountId == userAccountId));
    }

    [Fact]
    public void PrunedDeviceThatReturnsRecoversItsLiveNotification() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Returning Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        string staleDeviceToken = SeedDeviceToken(helperUserAccountId, DateTime.UtcNow.AddDays(-40));
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();
        string seekerAuthToken = TestUserFactory.CreateVerifiedEmailUser(container, "Waiting Seeker " + Guid.NewGuid());
        container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = seekerAuthToken, Topic = "Need someone to talk to" }).EnsureSuccessStatusCode();
        Flush();
        Assert.Contains(container.PushProvider.SentMessages, message => message.Token == staleDeviceToken && !message.IsDismiss);
        NotificationDispatchManager.RunRetentionSweep();

        string returningDeviceToken = "device-" + Guid.NewGuid();
        container.WebClient.PostJson("api/device/registerDevice", new { AuthToken = helperAuthToken, Token = returningDeviceToken, Platform = "ios" }).EnsureSuccessStatusCode();
        Flush();

        Assert.Contains(container.PushProvider.SentMessages, message => message.Token == returningDeviceToken && !message.IsDismiss);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.DeviceTokens.Any(field => field.Token == staleDeviceToken));
    }

    // Helpers - Seeding

    private static Guid SeedUser(string displayName) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName + " " + Guid.NewGuid(), IsAnonymous = false, CreatedAtUtc = DateTime.UtcNow, ProfilePhotoUrl = null });
        dbContext.SaveChanges();
        return userAccountId;
    }

    private static string SeedDeviceToken(Guid userAccountId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        string deviceToken = "device-" + Guid.NewGuid();
        dbContext.DeviceTokens.Add(new DeviceToken { Id = Guid.NewGuid(), UserAccountId = userAccountId, Token = deviceToken, Platform = "ios", CreatedAtUtc = lastSeenAtUtc, LastSeenAtUtc = lastSeenAtUtc });
        dbContext.SaveChanges();
        return deviceToken;
    }

    private static void SeedWaitingChannel(Guid recipientUserAccountId, DateTime lastEventAtUtc) {
        SeedChannel(recipientUserAccountId, NotificationChannelKind.Waiting, null, lastEventAtUtc);
    }


    private static void SeedChannel(Guid recipientUserAccountId, NotificationChannelKind kind, Guid? scopeChatGroupId, DateTime lastEventAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.NotificationChannels.Add(new NotificationChannel { Id = Guid.NewGuid(), RecipientUserAccountId = recipientUserAccountId, Kind = kind, ScopeChatGroupId = scopeChatGroupId, LastSentCount = 0, IsLive = false, LastEventAtUtc = lastEventAtUtc });
        dbContext.SaveChanges();
    }

    private static Guid SeedGroupRow(Guid ownerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = "Retention Group " + Guid.NewGuid(), OwnerUserAccountId = ownerUserAccountId, IsPublic = false, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
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
}
