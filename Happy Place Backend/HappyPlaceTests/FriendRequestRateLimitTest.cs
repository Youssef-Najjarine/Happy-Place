using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class FriendRequestRateLimitTest {
    // Tests - Hourly Limit

    [Fact]
    public void HourlyLimitBlocksTheTwentyFirstSendWithoutARowOrAudit() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        Guid senderUserAccountId = FriendshipTestActions.ResolveUserAccountId(senderAuthToken);
        Guid targetUserAccountId = FriendshipTestActions.ResolveUserAccountId(targetAuthToken);
        DateTime recentUtc = DateTime.UtcNow.AddMinutes(-5);
        for (int seededAuditNumber = 0; seededAuditNumber < 20; seededAuditNumber++)
            FriendshipTestActions.SeedAudit(senderUserAccountId, Guid.NewGuid(), recentUtc);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(senderUserAccountId, targetUserAccountId));
        Assert.Equal(20, FriendshipTestActions.CountAuditsFrom(senderUserAccountId));
    }

    [Fact]
    public void HourlyLimitIgnoresAuditsOlderThanAnHour() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        Guid senderUserAccountId = FriendshipTestActions.ResolveUserAccountId(senderAuthToken);
        DateTime staleUtc = DateTime.UtcNow.AddMinutes(-70);
        for (int seededAuditNumber = 0; seededAuditNumber < 20; seededAuditNumber++)
            FriendshipTestActions.SeedAudit(senderUserAccountId, Guid.NewGuid(), staleUtc);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    [Fact]
    public void HourlyLimitIsScopedToTheSendingUser() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        Guid unrelatedUserAccountId = Guid.NewGuid();
        DateTime recentUtc = DateTime.UtcNow.AddMinutes(-5);
        for (int seededAuditNumber = 0; seededAuditNumber < 20; seededAuditNumber++)
            FriendshipTestActions.SeedAudit(unrelatedUserAccountId, Guid.NewGuid(), recentUtc);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }

    // Tests - Per Pair Daily Limit

    [Fact]
    public void PerPairDailyLimitBlocksTheFourthSendToTheSamePerson() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string targetUsername = FriendshipTestActions.ResolveUsername(targetAuthToken);
        Guid senderUserAccountId = FriendshipTestActions.ResolveUserAccountId(senderAuthToken);
        Guid targetUserAccountId = FriendshipTestActions.ResolveUserAccountId(targetAuthToken);
        for (int attemptNumber = 0; attemptNumber < 3; attemptNumber++) {
            FriendshipTestActions.SendRequest(container, senderAuthToken, targetUsername).EnsureSuccessStatusCode();
            FriendshipTestActions.CancelRequest(container, senderAuthToken, targetUsername).EnsureSuccessStatusCode();
        }

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, targetUsername);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Null(FriendshipTestActions.FindFriendshipBetween(senderUserAccountId, targetUserAccountId));
        Assert.Equal(3, FriendshipTestActions.CountAuditsFrom(senderUserAccountId));
    }

    [Fact]
    public void PerPairLimitDoesNotBlockRequestsToOtherPeople() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string firstTargetAuthToken = FriendshipTestActions.CreateUser(container, "FirstTarget");
        string secondTargetAuthToken = FriendshipTestActions.CreateUser(container, "SecondTarget");
        string firstTargetUsername = FriendshipTestActions.ResolveUsername(firstTargetAuthToken);
        for (int attemptNumber = 0; attemptNumber < 3; attemptNumber++) {
            FriendshipTestActions.SendRequest(container, senderAuthToken, firstTargetUsername).EnsureSuccessStatusCode();
            FriendshipTestActions.CancelRequest(container, senderAuthToken, firstTargetUsername).EnsureSuccessStatusCode();
        }

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, senderAuthToken, FriendshipTestActions.ResolveUsername(secondTargetAuthToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("requested", FriendshipTestActions.ReadStatus(response));
    }
}
