using System.Net;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class FriendRequestListsTest {
    // Tests - Ordering

    [Fact]
    public void IncomingListsPendingRequestsNewestFirst() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string olderSenderAuthToken = FriendshipTestActions.CreateUser(container, "Older Sender");
        string newerSenderAuthToken = FriendshipTestActions.CreateUser(container, "Newer Sender");
        string callerUsername = FriendshipTestActions.ResolveUsername(callerAuthToken);
        FriendshipTestActions.SendRequest(container, olderSenderAuthToken, callerUsername).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, newerSenderAuthToken, callerUsername).EnsureSuccessStatusCode();
        BackdatePendingRequest(FriendshipTestActions.ResolveUserAccountId(olderSenderAuthToken), FriendshipTestActions.ResolveUserAccountId(callerAuthToken), DateTime.UtcNow.AddMinutes(-5));

        HttpResponseMessage response = FriendshipTestActions.ListIncomingRequests(container, callerAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<string> usernames = [.. response.ReadContentAsJsonDocument().RootElement.GetProperty("requests").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(newerSenderAuthToken), FriendshipTestActions.ResolveUsername(olderSenderAuthToken)];
        Assert.Equal(expectedUsernames, usernames);
    }

    [Fact]
    public void OutgoingListsPendingRequestsNewestFirst() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = FriendshipTestActions.CreateUser(container, "Caller");
        string olderTargetAuthToken = FriendshipTestActions.CreateUser(container, "Older Target");
        string newerTargetAuthToken = FriendshipTestActions.CreateUser(container, "Newer Target");
        FriendshipTestActions.SendRequest(container, callerAuthToken, FriendshipTestActions.ResolveUsername(olderTargetAuthToken)).EnsureSuccessStatusCode();
        FriendshipTestActions.SendRequest(container, callerAuthToken, FriendshipTestActions.ResolveUsername(newerTargetAuthToken)).EnsureSuccessStatusCode();
        BackdatePendingRequest(FriendshipTestActions.ResolveUserAccountId(callerAuthToken), FriendshipTestActions.ResolveUserAccountId(olderTargetAuthToken), DateTime.UtcNow.AddMinutes(-5));

        HttpResponseMessage response = FriendshipTestActions.ListOutgoingRequests(container, callerAuthToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<string> usernames = [.. response.ReadContentAsJsonDocument().RootElement.GetProperty("requests").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
        List<string> expectedUsernames = [FriendshipTestActions.ResolveUsername(newerTargetAuthToken), FriendshipTestActions.ResolveUsername(olderTargetAuthToken)];
        Assert.Equal(expectedUsernames, usernames);
    }

    // Tests - List Contents

    [Fact]
    public void IncomingAndOutgoingAreDisjoint() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);

        var requesterIncoming = ReadUsernames(FriendshipTestActions.ListIncomingRequests(container, pendingPair.RequesterAuthToken));
        var requesterOutgoing = ReadUsernames(FriendshipTestActions.ListOutgoingRequests(container, pendingPair.RequesterAuthToken));
        var addresseeIncoming = ReadUsernames(FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken));
        var addresseeOutgoing = ReadUsernames(FriendshipTestActions.ListOutgoingRequests(container, pendingPair.AddresseeAuthToken));

        Assert.Empty(requesterIncoming);
        List<string> expectedOutgoing = [pendingPair.AddresseeUsername];
        List<string> expectedIncoming = [pendingPair.RequesterUsername];
        Assert.Equal(expectedOutgoing, requesterOutgoing);
        Assert.Equal(expectedIncoming, addresseeIncoming);
        Assert.Empty(addresseeOutgoing);
    }

    [Fact]
    public void ResponseContainsExactlyExpectedProperties() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken);

        var root = response.ReadContentAsJsonDocument().RootElement;
        List<string> rootProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRootProperties = ["requests"];
        Assert.Equal(expectedRootProperties, rootProperties);
        var firstRow = root.GetProperty("requests").EnumerateArray().Single();
        List<string> rowProperties = [.. firstRow.EnumerateObject().Select(property => property.Name).OrderBy(name => name)];
        List<string> expectedRowProperties = ["avatarColor", "displayName", "profilePhotoUrl", "requestedAtUtc", "username"];
        Assert.Equal(expectedRowProperties, rowProperties);
    }

    [Fact]
    public void RequestedAtUtcIsRecentAndParseable() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);

        HttpResponseMessage response = FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken);

        var firstRow = response.ReadContentAsJsonDocument().RootElement.GetProperty("requests").EnumerateArray().Single();
        DateTime requestedAtUtc = firstRow.GetProperty("requestedAtUtc").GetDateTime();
        Assert.True(requestedAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    // Tests - Removal On Resolution

    [Fact]
    public void AcceptRemovesFromBothLists() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        Assert.Empty(ReadUsernames(FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken)));
        Assert.Empty(ReadUsernames(FriendshipTestActions.ListOutgoingRequests(container, pendingPair.RequesterAuthToken)));
    }

    [Fact]
    public void DeclineRemovesFromBothLists() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.DeclineRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();

        Assert.Empty(ReadUsernames(FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken)));
        Assert.Empty(ReadUsernames(FriendshipTestActions.ListOutgoingRequests(container, pendingPair.RequesterAuthToken)));
    }

    [Fact]
    public void CancelRemovesFromBothLists() {
        using var container = new TestingMockProvidersContainer();
        FriendshipPair pendingPair = FriendshipTestActions.CreatePendingPair(container);
        FriendshipTestActions.CancelRequest(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();

        Assert.Empty(ReadUsernames(FriendshipTestActions.ListIncomingRequests(container, pendingPair.AddresseeAuthToken)));
        Assert.Empty(ReadUsernames(FriendshipTestActions.ListOutgoingRequests(container, pendingPair.RequesterAuthToken)));
    }

    // Tests - Guests

    [Fact]
    public void GuestCallerGetsEmptyLists() {
        using var container = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(container);

        HttpResponseMessage incomingResponse = FriendshipTestActions.ListIncomingRequests(container, guestAuthToken);
        HttpResponseMessage outgoingResponse = FriendshipTestActions.ListOutgoingRequests(container, guestAuthToken);

        Assert.Equal(HttpStatusCode.OK, incomingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, outgoingResponse.StatusCode);
        Assert.Empty(ReadUsernames(incomingResponse));
        Assert.Empty(ReadUsernames(outgoingResponse));
    }

    // Tests - Authentication

    [Fact]
    public void IncomingEmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListIncomingRequests(container, "");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void IncomingInvalidAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListIncomingRequests(container, "invalid-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void IncomingMissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/listIncomingRequests", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OutgoingEmptyAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListOutgoingRequests(container, "");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OutgoingInvalidAuthTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = FriendshipTestActions.ListOutgoingRequests(container, "invalid-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OutgoingMissingAuthTokenFieldReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/friendship/listOutgoingRequests", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Methods - Helpers

    private static List<string> ReadUsernames(HttpResponseMessage response) {
        return [.. response.ReadContentAsJsonDocument().RootElement.GetProperty("requests").EnumerateArray().Select(row => row.GetProperty("username").GetString())];
    }

    private static void BackdatePendingRequest(Guid requesterUserAccountId, Guid addresseeUserAccountId, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == requesterUserAccountId && field.AddresseeUserAccountId == addresseeUserAccountId && field.Status == FriendshipStatus.Pending)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.CreatedAtUtc, createdAtUtc));
    }
}
