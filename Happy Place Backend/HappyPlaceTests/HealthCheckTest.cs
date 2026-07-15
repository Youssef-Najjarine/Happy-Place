using HappyWorld.HappyPlace.Data;
using System.Net;
using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HealthCheckTest {
    // Tests - Healthy

    [Fact]
    public void CheckReturnsHealthyWhenDatabaseIsAvailable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage healthResponse = testingMockProvidersContainer.WebClient.Get("api/health/check");
        using var healthStream = healthResponse.Content.ReadAsStream();
        using var healthReader = new StreamReader(healthStream);
        string healthBody = healthReader.ReadToEnd();
        JsonElement healthJson = JsonSerializer.Deserialize<JsonElement>(healthBody);

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal("healthy", healthJson.GetProperty("status").GetString());
        Assert.True(healthJson.GetProperty("database").GetBoolean());
    }

    // Tests - Unavailable

    [Fact]
    public void CheckReturnsServiceUnavailableWhenDatabaseIsUnreachable() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HappyPlaceDbContext.SetConnectionString("Server=tcp:127.0.0.1,1;Database=HappyPlaceTests;Trusted_Connection=True;Connect Timeout=1;trustservercertificate=yes");
        HttpResponseMessage healthResponse = testingMockProvidersContainer.WebClient.Get("api/health/check");
        using var healthStream = healthResponse.Content.ReadAsStream();
        using var healthReader = new StreamReader(healthStream);
        string healthBody = healthReader.ReadToEnd();
        JsonElement healthJson = JsonSerializer.Deserialize<JsonElement>(healthBody);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, healthResponse.StatusCode);
        Assert.Equal("unavailable", healthJson.GetProperty("status").GetString());
        Assert.False(healthJson.GetProperty("database").GetBoolean());
    }
}
