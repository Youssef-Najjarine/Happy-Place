using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class AvailableHelpersTest {
    // Tests - Authentication Failures

    [Fact]
    public void AvailableHelpersEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/availableHelpers", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AvailableHelpersInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/availableHelpers", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void AvailableHelpersMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/availableHelpers", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Availability Filtering

    [Fact]
    public void NoAvailabilityRowsReturnsEmptyArray() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public void AvailableUserAppears() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Helper", null);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.True(ContainsHelper(root, helperUserAccountId));
    }

    [Fact]
    public void UnavailableUserDoesNotAppear() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Helper", null);
        SeedAvailability(helperUserAccountId, false, DateTime.UtcNow);

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.False(ContainsHelper(root, helperUserAccountId));
    }

    [Fact]
    public void RequesterExcludedEvenWhenAvailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(requesterAuthToken);
        SeedAvailability(requesterUserAccountId, true, DateTime.UtcNow);

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.False(ContainsHelper(root, requesterUserAccountId));
    }

    [Fact]
    public void MultipleAvailableUsersAllAppear() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid firstHelperUserAccountId = SeedUser("Helper One", null);
        Guid secondHelperUserAccountId = SeedUser("Helper Two", null);
        SeedAvailability(firstHelperUserAccountId, true, DateTime.UtcNow);
        SeedAvailability(secondHelperUserAccountId, true, DateTime.UtcNow);

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.True(ContainsHelper(root, firstHelperUserAccountId));
        Assert.True(ContainsHelper(root, secondHelperUserAccountId));
    }

    // Tests - Block Relations

    [Fact]
    public void BlockRelatedHelpersAreHiddenInBothDirections() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string blockerAuthToken = CreateUser(testingMockProvidersContainer, "Blocker");
        string blockedAuthToken = CreateUser(testingMockProvidersContainer, "Blocked Helper");
        Guid blockerUserAccountId = ResolveUserAccountId(blockerAuthToken);
        Guid blockedUserAccountId = ResolveUserAccountId(blockedAuthToken);
        Guid controlUserAccountId = SeedUser("Control Helper", null);
        SeedAvailability(blockerUserAccountId, true, DateTime.UtcNow);
        SeedAvailability(blockedUserAccountId, true, DateTime.UtcNow);
        SeedAvailability(controlUserAccountId, true, DateTime.UtcNow);
        FriendshipTestActions.Block(testingMockProvidersContainer, blockerAuthToken, FriendshipTestActions.ResolveUsername(blockedAuthToken)).EnsureSuccessStatusCode();

        JsonElement blockerRoot = AvailableHelpers(testingMockProvidersContainer, blockerAuthToken);
        JsonElement blockedRoot = AvailableHelpers(testingMockProvidersContainer, blockedAuthToken);

        Assert.False(ContainsHelper(blockerRoot, blockedUserAccountId));
        Assert.True(ContainsHelper(blockerRoot, controlUserAccountId));
        Assert.False(ContainsHelper(blockedRoot, blockerUserAccountId));
        Assert.True(ContainsHelper(blockedRoot, controlUserAccountId));
    }

    // Tests - Response Shape

    [Fact]
    public void AvailableHelperItemContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Helper", null);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);
        List<string> actualProperties = [.. helper.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["avatarColor", "id", "isAnonymous", "name", "profilePhotoUrl", "username"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void NameReflectsDisplayName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Compassionate Listener", null);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.Equal("Compassionate Listener", helper.GetProperty("name").GetString());
    }

    [Fact]
    public void ProfilePhotoUrlReflectsUserAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Helper", "/api/photo/" + Guid.NewGuid());
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.StartsWith("/api/photo/", helper.GetProperty("profilePhotoUrl").GetString());
    }

    [Fact]
    public void UsernameReflectsUserAccount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        string helperAuthToken = CreateUser(testingMockProvidersContainer, "Named Helper");
        Guid helperUserAccountId = ResolveUserAccountId(helperAuthToken);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.Equal(FriendshipTestActions.ResolveUsername(helperAuthToken), helper.GetProperty("username").GetString());
        Assert.False(helper.GetProperty("isAnonymous").GetBoolean());
    }

    [Fact]
    public void UsernameIsNullWhenUserHasNoUsername() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Incomplete Helper", null);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.Equal(JsonValueKind.Null, helper.GetProperty("username").ValueKind);
    }

    [Fact]
    public void AnonymousHelpersAreMarkedAnonymous() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedAnonymousUser("Guest Helper");
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.True(helper.GetProperty("isAnonymous").GetBoolean());
    }

    // Tests - Ordering And Cap

    [Fact]
    public void ReturnsMostRecentlyAvailableFirst() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid staleHelperUserAccountId = SeedUser("Stale Helper", null);
        Guid freshHelperUserAccountId = SeedUser("Fresh Helper", null);
        SeedAvailability(staleHelperUserAccountId, true, DateTime.UtcNow.AddMinutes(-30));
        SeedAvailability(freshHelperUserAccountId, true, DateTime.UtcNow);

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.Equal(freshHelperUserAccountId.ToString(), root[0].GetProperty("id").GetString());
    }

    [Fact]
    public void ReturnsAtMostFiftyAvailableHelpers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        for (int index = 0; index < 51; index++) {
            Guid helperUserAccountId = SeedUser("Helper " + index, null);
            SeedAvailability(helperUserAccountId, true, DateTime.UtcNow.AddSeconds(-index));
        }

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.Equal(50, root.GetArrayLength());
    }

    [Fact]
    public void CapKeepsMostRecentAndDropsOldest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid oldestHelperUserAccountId = SeedUser("Oldest Helper", null);
        SeedAvailability(oldestHelperUserAccountId, true, DateTime.UtcNow.AddHours(-1));
        Guid newestHelperUserAccountId = SeedUser("Newest Helper", null);
        SeedAvailability(newestHelperUserAccountId, true, DateTime.UtcNow);
        for (int index = 0; index < 49; index++) {
            Guid fillerHelperUserAccountId = SeedUser("Filler " + index, null);
            SeedAvailability(fillerHelperUserAccountId, true, DateTime.UtcNow.AddMinutes(-1 - index));
        }

        JsonElement root = AvailableHelpers(testingMockProvidersContainer, requesterAuthToken);

        Assert.Equal(50, root.GetArrayLength());
        Assert.True(ContainsHelper(root, newestHelperUserAccountId));
        Assert.False(ContainsHelper(root, oldestHelperUserAccountId));
    }

    [Fact]
    public void AvatarColorIsPopulated() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid helperUserAccountId = SeedUser("Helper", null);
        SeedAvailability(helperUserAccountId, true, DateTime.UtcNow);

        JsonElement helper = GetHelper(AvailableHelpers(testingMockProvidersContainer, requesterAuthToken), helperUserAccountId);

        Assert.False(string.IsNullOrEmpty(helper.GetProperty("avatarColor").GetString()));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement AvailableHelpers(TestingMockProvidersContainer testingMockProvidersContainer, string authToken) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/availableHelpers", new { AuthToken = authToken }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static Guid SeedUser(string displayName, string profilePhotoUrl) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName, IsAnonymous = false, CreatedAtUtc = DateTime.UtcNow, ProfilePhotoUrl = profilePhotoUrl });
        dbContext.SaveChanges();
        return userAccountId;
    }

    private static Guid SeedAnonymousUser(string displayName) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName, IsAnonymous = true, CreatedAtUtc = DateTime.UtcNow, ProfilePhotoUrl = null });
        dbContext.SaveChanges();
        return userAccountId;
    }

    private static void SeedAvailability(Guid helperUserAccountId, bool isAvailable, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.HelpAvailabilities.Add(new HelpAvailability { Id = Guid.NewGuid(), HelperUserAccountId = helperUserAccountId, IsAvailable = isAvailable, LastSeenAtUtc = lastSeenAtUtc });
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool ContainsHelper(JsonElement root, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
    }

    private static JsonElement GetHelper(JsonElement root, Guid userAccountId) {
        string target = userAccountId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Available helper was not present in the response.");
    }
}
