using HappyWorld.HappyPlace.Data;
using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class MutualFriendRequestTest {
    // Tests - Auto Accept

    [Fact]
    public void SendWhileTheOppositeRequestIsPendingAutoAccepts() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        HttpResponseMessage response = FriendshipTestActions.SendRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("accepted", FriendshipTestActions.ReadStatus(response));
        Friendship friendship = FriendshipTestActions.FindFriendshipBetween(requesterUserAccountId, addresseeUserAccountId);
        Assert.Equal(FriendshipStatus.Accepted, friendship.Status);
        Assert.Equal(requesterUserAccountId, friendship.RequesterUserAccountId);
        Assert.NotNull(friendship.RespondedAtUtc);
        Assert.Equal(1, FriendshipTestActions.CountFriendshipRowsBetween(requesterUserAccountId, addresseeUserAccountId));
    }

    [Fact]
    public void AutoAcceptDoesNotWriteAnAuditRowForTheSecondSender() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);
        Guid requesterUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.RequesterAuthToken);
        Guid addresseeUserAccountId = FriendshipTestActions.ResolveUserAccountId(pendingPair.AddresseeAuthToken);

        FriendshipTestActions.SendRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        Assert.Equal(0, FriendshipTestActions.CountAuditsFrom(addresseeUserAccountId));
        Assert.Equal(1, FriendshipTestActions.CountAuditsFrom(requesterUserAccountId));
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentOppositeSendsEndWithExactlyOneAcceptedRow() {
        using var container = new TestingMockProvidersContainer();
        string firstAuthToken = FriendshipTestActions.CreateUser(container, "First");
        string secondAuthToken = FriendshipTestActions.CreateUser(container, "Second");
        string firstUsername = FriendshipTestActions.ResolveUsername(firstAuthToken);
        string secondUsername = FriendshipTestActions.ResolveUsername(secondAuthToken);
        HttpResponseMessage firstResponse = null;
        HttpResponseMessage secondResponse = null;

        List<Exception> exceptions = FriendshipTestActions.RunConcurrently(
            () => firstResponse = FriendshipTestActions.SendRequest(container, firstAuthToken, secondUsername),
            () => secondResponse = FriendshipTestActions.SendRequest(container, secondAuthToken, firstUsername));

        Assert.Empty(exceptions);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        List<string> sortedStatuses = new List<string> { FriendshipTestActions.ReadStatus(firstResponse), FriendshipTestActions.ReadStatus(secondResponse) }.OrderBy(status => status).ToList();
        Assert.Equal(new List<string> { "accepted", "requested" }, sortedStatuses);
        Guid firstUserAccountId = FriendshipTestActions.ResolveUserAccountId(firstAuthToken);
        Guid secondUserAccountId = FriendshipTestActions.ResolveUserAccountId(secondAuthToken);
        Assert.Equal(1, FriendshipTestActions.CountFriendshipRowsBetween(firstUserAccountId, secondUserAccountId));
        Assert.Equal(FriendshipStatus.Accepted, FriendshipTestActions.FindFriendshipBetween(firstUserAccountId, secondUserAccountId).Status);
    }

    [Fact]
    public void ConcurrentDuplicateSendsCreateExactlyOnePendingRowAndOneAudit() {
        using var container = new TestingMockProvidersContainer();
        string senderAuthToken = FriendshipTestActions.CreateUser(container, "Sender");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");
        string targetUsername = FriendshipTestActions.ResolveUsername(targetAuthToken);
        HttpResponseMessage firstResponse = null;
        HttpResponseMessage secondResponse = null;
        HttpResponseMessage thirdResponse = null;

        List<Exception> exceptions = FriendshipTestActions.RunConcurrently(
            () => firstResponse = FriendshipTestActions.SendRequest(container, senderAuthToken, targetUsername),
            () => secondResponse = FriendshipTestActions.SendRequest(container, senderAuthToken, targetUsername),
            () => thirdResponse = FriendshipTestActions.SendRequest(container, senderAuthToken, targetUsername));

        Assert.Empty(exceptions);
        List<string> sortedStatuses = new List<string> { FriendshipTestActions.ReadStatus(firstResponse), FriendshipTestActions.ReadStatus(secondResponse), FriendshipTestActions.ReadStatus(thirdResponse) }.OrderBy(status => status).ToList();
        Assert.Equal(new List<string> { "alreadyRequested", "alreadyRequested", "requested" }, sortedStatuses);
        Guid senderUserAccountId = FriendshipTestActions.ResolveUserAccountId(senderAuthToken);
        Guid targetUserAccountId = FriendshipTestActions.ResolveUserAccountId(targetAuthToken);
        Assert.Equal(1, FriendshipTestActions.CountFriendshipRowsBetween(senderUserAccountId, targetUserAccountId));
        Assert.Equal(FriendshipStatus.Pending, FriendshipTestActions.FindFriendshipBetween(senderUserAccountId, targetUserAccountId).Status);
        Assert.Equal(1, FriendshipTestActions.CountAuditsFrom(senderUserAccountId));
    }
}
