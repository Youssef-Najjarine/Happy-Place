using System.Net;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HelpAvailabilityTest {
    // Tests - Authentication Failures

    [Fact]
    public void EmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = "", IsAvailable = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void InvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = "not-a-real-token-at-all", IsAvailable = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void MissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { IsAvailable = true });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Availability Upsert

    [Fact]
    public void SetAvailabilityCreatesAvailabilityRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        HelpAvailability availability = dbContext.HelpAvailabilities.Single(field => field.HelperUserAccountId == helperUserAccountId);
        Assert.True(availability.IsAvailable);
    }

    [Fact]
    public void SetAvailabilityIsIdempotentPerHelper() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpAvailabilities.Count(field => field.HelperUserAccountId == helperUserAccountId));
    }

    [Fact]
    public void SetAvailabilityCanToggleOff() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();
        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = false }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.HelpAvailabilities.Single(field => field.HelperUserAccountId == helperUserAccountId).IsAvailable);
    }

    [Fact]
    public void SetAvailabilityAdvancesLastSeenAtUtc() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);
        DateTime staleLastSeen = DateTime.UtcNow.AddMinutes(-10);
        using (var seedContext = HappyPlaceDbContext.Create()) {
            seedContext.HelpAvailabilities.Add(new() { Id = Guid.NewGuid(), HelperUserAccountId = helperUserAccountId, IsAvailable = true, LastSeenAtUtc = staleLastSeen });
            seedContext.SaveChanges();
        }

        testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }).EnsureSuccessStatusCode();

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.HelpAvailabilities.Single(field => field.HelperUserAccountId == helperUserAccountId).LastSeenAtUtc > staleLastSeen);
    }

    [Fact]
    public void ConcurrentDuplicateSetAvailabilityProducesOneRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string helperAuthToken = TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, "Helper " + Guid.NewGuid());
        Guid helperUserAccountId = Guid.Parse(UserAuthenticationToken.ValidateToken(helperAuthToken).Identifier);

        HttpResponseMessage firstResponse = null;
        HttpResponseMessage secondResponse = null;
        Thread firstThread = new(() => firstResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }));
        Thread secondThread = new(() => secondResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = helperAuthToken, IsAvailable = true }));
        firstThread.Start();
        secondThread.Start();
        firstThread.Join();
        secondThread.Join();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpAvailabilities.Count(field => field.HelperUserAccountId == helperUserAccountId));
    }
}
