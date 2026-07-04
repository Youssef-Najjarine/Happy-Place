using System.Collections.Concurrent;
using System.Threading;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HelpAvailabilityUpsertTest {
    // Tests - Upsert

    [Fact]
    public void SetAvailabilityCreatesASingleAvailableRowOnFirstCall() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");

        SetAvailable(container, helperAuthToken, true);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.True(dbContext.HelpAvailabilities.Single().IsAvailable);
    }

    [Fact]
    public void SetAvailabilityFalseOnFirstCallCreatesAnUnavailableRow() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");

        SetAvailable(container, helperAuthToken, false);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.False(dbContext.HelpAvailabilities.Single().IsAvailable);
    }

    [Fact]
    public void RepeatedTogglesLeaveOneRowHoldingTheFinalValue() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");

        SetAvailable(container, helperAuthToken, true);
        SetAvailable(container, helperAuthToken, false);
        SetAvailable(container, helperAuthToken, true);
        SetAvailable(container, helperAuthToken, false);

        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpAvailabilities.Count());
        Assert.False(dbContext.HelpAvailabilities.Single().IsAvailable);
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentSetAvailabilityCallsLeaveExactlyOneRowWithoutError() {
        using var container = new TestingMockProvidersContainer();
        string helperAuthToken = CreateUser(container, "Helper");
        ConcurrentBag<Exception> errors = [];
        List<Thread> threads = [];
        for (int index = 0; index < 8; index++) {
            bool value = index % 2 == 0;
            threads.Add(new Thread(() => {
                try { SetAvailable(container, helperAuthToken, value); }
                catch (Exception error) { errors.Add(error); }
            }));
        }
        foreach (Thread thread in threads)
            thread.Start();
        foreach (Thread thread in threads)
            thread.Join();

        Assert.Empty(errors);
        using var dbContext = HappyPlaceDbContext.Create();
        Assert.Equal(1, dbContext.HelpAvailabilities.Count());
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }
}
