using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class RealtimeFriendsPublishTest {
    // Tests - Requests

    [Fact]
    public void SendRequestPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/sendRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void SendRequestToUnknownUsernamePublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/sendRequest", new { AuthToken = aliceAuthToken, Username = "nobodyhere" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void SendRequestAutoAcceptPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedPendingFriendship(bobUserAccountId, aliceUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/sendRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void CancelRequestPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedPendingFriendship(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/cancelRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void CancelWithoutPendingRequestPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/cancelRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Responses

    [Fact]
    public void AcceptRequestPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedPendingFriendship(bobUserAccountId, aliceUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/acceptRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void DeclineRequestPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedPendingFriendship(bobUserAccountId, aliceUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/declineRequest", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    // Tests - Unfriend

    [Fact]
    public void UnfriendPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedAcceptedFriendship(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/unfriend", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    // Tests - Blocking

    [Fact]
    public void BlockOfFriendPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedAcceptedFriendship(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/block", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void RepeatBlockPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedBlock(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/block", new { AuthToken = aliceAuthToken, Username = "bob" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    [Fact]
    public void UnblockPublishesToBothParties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        string bobAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        Guid aliceUserAccountId = HelpParticipant.ResolveUserAccountId(aliceAuthToken).Value;
        Guid bobUserAccountId = HelpParticipant.ResolveUserAccountId(bobAuthToken).Value;
        SeedBlock(aliceUserAccountId, bobUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/unblock", new { AuthToken = aliceAuthToken, Username = "bob" });

        List<Guid> expectedUserAccountIds = [aliceUserAccountId, bobUserAccountId];
        AssertFriendsChangedForUsers(EventsAfter(testingMockProvidersContainer, baselineCount), expectedUserAccountIds);
    }

    [Fact]
    public void UnblockWithoutBlockPublishesNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string aliceAuthToken = CreateUserWithUsername(testingMockProvidersContainer, "Alice", "alice");
        CreateUserWithUsername(testingMockProvidersContainer, "Bob", "bob");
        int baselineCount = CountEvents(testingMockProvidersContainer);

        PostJsonOrFail(testingMockProvidersContainer, "api/friendship/unblock", new { AuthToken = aliceAuthToken, Username = "bob" });

        Assert.Empty(EventsAfter(testingMockProvidersContainer, baselineCount));
    }

    // Tests - Account Deletion

    [Fact]
    public void AccountDeletionUntanglePublishesToEveryFriendshipCounterparty() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string danaAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Dana");
        string acceptedFriendAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Accepted Friend");
        string pendingOutgoingAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending Outgoing");
        string pendingIncomingAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Pending Incoming");
        Guid danaUserAccountId = HelpParticipant.ResolveUserAccountId(danaAuthToken).Value;
        Guid acceptedFriendUserAccountId = HelpParticipant.ResolveUserAccountId(acceptedFriendAuthToken).Value;
        Guid pendingOutgoingUserAccountId = HelpParticipant.ResolveUserAccountId(pendingOutgoingAuthToken).Value;
        Guid pendingIncomingUserAccountId = HelpParticipant.ResolveUserAccountId(pendingIncomingAuthToken).Value;
        SeedAcceptedFriendship(danaUserAccountId, acceptedFriendUserAccountId);
        SeedPendingFriendship(danaUserAccountId, pendingOutgoingUserAccountId);
        SeedPendingFriendship(pendingIncomingUserAccountId, danaUserAccountId);
        int baselineCount = CountEvents(testingMockProvidersContainer);

        FriendshipManager.UntangleUserForAccountDeletion(danaUserAccountId);

        List<RealtimeSentEvent> sentEvents = EventsAfter(testingMockProvidersContainer, baselineCount);
        List<Guid> expectedUserAccountIds = [acceptedFriendUserAccountId, pendingOutgoingUserAccountId, pendingIncomingUserAccountId];
        AssertFriendsChangedForUsers(sentEvents, expectedUserAccountIds);
        Assert.DoesNotContain(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(danaUserAccountId));
    }

    // Helpers

    private static HttpResponseMessage PostJsonOrFail(TestingMockProvidersContainer testingMockProvidersContainer, string url, object jsonData) {
        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson(url, jsonData);
        Assert.True(response.IsSuccessStatusCode);
        return response;
    }

    private static string CreateUserWithUsername(TestingMockProvidersContainer testingMockProvidersContainer, string displayName, string username) {
        string authToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, displayName);
        Guid userAccountId = HelpParticipant.ResolveUserAccountId(authToken).Value;
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts
            .Where(field => field.Id == userAccountId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Username, username));
        return authToken;
    }

    private static void SeedPendingFriendship(Guid requesterUserAccountId, Guid addresseeUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.Friendships.Add(new Friendship { Id = Guid.NewGuid(), RequesterUserAccountId = requesterUserAccountId, AddresseeUserAccountId = addresseeUserAccountId, Status = FriendshipStatus.Pending, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void SeedAcceptedFriendship(Guid requesterUserAccountId, Guid addresseeUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        DateTime now = DateTime.UtcNow;
        dbContext.Friendships.Add(new Friendship { Id = Guid.NewGuid(), RequesterUserAccountId = requesterUserAccountId, AddresseeUserAccountId = addresseeUserAccountId, Status = FriendshipStatus.Accepted, CreatedAtUtc = now, RespondedAtUtc = now });
        dbContext.SaveChanges();
    }

    private static void SeedBlock(Guid blockerUserAccountId, Guid blockedUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserBlocks.Add(new UserBlock { Id = Guid.NewGuid(), BlockerUserAccountId = blockerUserAccountId, BlockedUserAccountId = blockedUserAccountId, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static int CountEvents(TestingMockProvidersContainer testingMockProvidersContainer) {
        return testingMockProvidersContainer.RealtimeProvider.SentEvents.Count();
    }

    private static List<RealtimeSentEvent> EventsAfter(TestingMockProvidersContainer testingMockProvidersContainer, int baselineCount) {
        return [.. testingMockProvidersContainer.RealtimeProvider.SentEvents.Skip(baselineCount)];
    }

    private static void AssertFriendsChangedForUsers(List<RealtimeSentEvent> sentEvents, List<Guid> expectedUserAccountIds) {
        Assert.Equal(expectedUserAccountIds.Count, sentEvents.Count);
        foreach (Guid expectedUserAccountId in expectedUserAccountIds)
            Assert.Contains(sentEvents, field => field.GroupName == RealtimePublisher.BuildUserGroupName(expectedUserAccountId));
        foreach (RealtimeSentEvent sentEvent in sentEvents) {
            Assert.Equal(RealtimePublisher.FriendsChangedEventName, sentEvent.EventName);
            Assert.Empty(sentEvent.Payload);
        }
    }
}
