using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public static class FriendshipTestActions {
    // Methods - Users

    public static string CreateUser(TestingMockProvidersContainer container, string displayName) {
        return TestUserFactory.CreateVerifiedEmailUser(container, displayName + " " + Guid.NewGuid());
    }

    public static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    public static string ResolveUsername(string authToken) {
        Guid userAccountId = ResolveUserAccountId(authToken);
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userAccountId).Username;
    }

    public static void MakeAnonymous(string authToken) {
        Guid userAccountId = ResolveUserAccountId(authToken);
        using var dbContext = HappyPlaceDbContext.Create();
        var user = dbContext.UserAccounts.Single(field => field.Id == userAccountId);
        user.IsAnonymous = true;
        dbContext.SaveChanges();
    }

    // Methods - Endpoint Calls

    public static HttpResponseMessage SendRequest(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/sendRequest", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage CancelRequest(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/cancelRequest", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage AcceptRequest(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/acceptRequest", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage DeclineRequest(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/declineRequest", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage Unfriend(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/unfriend", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage Block(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/block", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage Unblock(TestingMockProvidersContainer container, string authToken, string username) {
        return container.WebClient.PostJson("api/friendship/unblock", new { AuthToken = authToken, Username = username });
    }

    public static HttpResponseMessage ListBlocked(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/friendship/listBlocked", new { AuthToken = authToken });
    }

    public static HttpResponseMessage ListFriends(TestingMockProvidersContainer container, string authToken, string username = null, string search = null, string cursor = null) {
        return container.WebClient.PostJson("api/friendship/listFriends", new { AuthToken = authToken, Username = username, Search = search, Cursor = cursor });
    }

    public static HttpResponseMessage ListIncomingRequests(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/friendship/listIncomingRequests", new { AuthToken = authToken });
    }

    public static HttpResponseMessage ListOutgoingRequests(TestingMockProvidersContainer container, string authToken) {
        return container.WebClient.PostJson("api/friendship/listOutgoingRequests", new { AuthToken = authToken });
    }

    public static HttpResponseMessage SearchUsers(TestingMockProvidersContainer container, string authToken, string query) {
        return container.WebClient.PostJson("api/friendship/searchUsers", new { AuthToken = authToken, Query = query });
    }

    // Methods - Reading

    public static string ReadStatus(HttpResponseMessage response) {
        return response.ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString();
    }

    public static string ReadBody(HttpResponseMessage response) {
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public static Friendship FindFriendshipBetween(Guid firstUserAccountId, Guid secondUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.Friendships.SingleOrDefault(field => (field.RequesterUserAccountId == firstUserAccountId && field.AddresseeUserAccountId == secondUserAccountId) || (field.RequesterUserAccountId == secondUserAccountId && field.AddresseeUserAccountId == firstUserAccountId));
    }

    public static int CountFriendshipRowsBetween(Guid firstUserAccountId, Guid secondUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.Friendships.Count(field => (field.RequesterUserAccountId == firstUserAccountId && field.AddresseeUserAccountId == secondUserAccountId) || (field.RequesterUserAccountId == secondUserAccountId && field.AddresseeUserAccountId == firstUserAccountId));
    }

    public static int CountAuditsFrom(Guid requesterUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.FriendRequestAudits.Count(field => field.RequesterUserAccountId == requesterUserAccountId);
    }

    public static void SeedAudit(Guid requesterUserAccountId, Guid addresseeUserAccountId, DateTime requestedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.FriendRequestAudits.Add(new() { Id = Guid.NewGuid(), RequesterUserAccountId = requesterUserAccountId, AddresseeUserAccountId = addresseeUserAccountId, RequestedAtUtc = requestedAtUtc });
        dbContext.SaveChanges();
    }

    public static void SeedAcceptedFriendship(Guid requesterUserAccountId, Guid addresseeUserAccountId, DateTime respondedAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.Friendships.Add(new() { Id = Guid.NewGuid(), RequesterUserAccountId = requesterUserAccountId, AddresseeUserAccountId = addresseeUserAccountId, Status = FriendshipStatus.Accepted, CreatedAtUtc = respondedAtUtc, RespondedAtUtc = respondedAtUtc });
        dbContext.SaveChanges();
    }

    public static bool BlockRowExists(Guid blockerUserAccountId, Guid blockedUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserBlocks.Any(field => field.BlockerUserAccountId == blockerUserAccountId && field.BlockedUserAccountId == blockedUserAccountId);
    }

    public static int CountBlockRowsFrom(Guid blockerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserBlocks.Count(field => field.BlockerUserAccountId == blockerUserAccountId);
    }

    // Methods - Seeded Relationships

    public static FriendshipPair CreatePendingPair(TestingMockProvidersContainer container) {
        string requesterAuthToken = CreateUser(container, "Requester");
        string addresseeAuthToken = CreateUser(container, "Addressee");
        SendRequest(container, requesterAuthToken, ResolveUsername(addresseeAuthToken)).EnsureSuccessStatusCode();
        return new FriendshipPair(requesterAuthToken, addresseeAuthToken, ResolveUsername(requesterAuthToken), ResolveUsername(addresseeAuthToken));
    }

    public static FriendshipPair CreateFriends(TestingMockProvidersContainer container) {
        FriendshipPair pendingPair = CreatePendingPair(container);
        AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();
        return pendingPair;
    }

    public static void MakeFriends(TestingMockProvidersContainer container, string firstAuthToken, string secondAuthToken) {
        SendRequest(container, firstAuthToken, ResolveUsername(secondAuthToken)).EnsureSuccessStatusCode();
        AcceptRequest(container, secondAuthToken, ResolveUsername(firstAuthToken)).EnsureSuccessStatusCode();
    }

    // Methods - Concurrency

    public static List<Exception> RunConcurrently(params Action[] actions) {
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
}
