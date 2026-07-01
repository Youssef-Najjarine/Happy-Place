using System.Net;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class CreateGuestTest {
    // Tests - Guest Creation

    [Fact]
    public void CreateGuestReturnsAuthToken() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/createGuest", new { });
        var responseData = response.ReadContentAsJsonDocument();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(responseData.RootElement.GetProperty("authToken").GetString()));
    }

    [Fact]
    public void TwoGuestsReceiveDifferentAuthTokens() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        string firstGuestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        string secondGuestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);

        Assert.False(string.IsNullOrEmpty(firstGuestAuthToken));
        Assert.NotEqual(firstGuestAuthToken, secondGuestAuthToken);
    }

    // Tests - Guest Token Is A First Class Credential

    [Fact]
    public void GuestTokenIsAcceptedByAuthenticatedEndpoint() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);

        HttpResponseMessage pollResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/pollOffer", new { AuthToken = guestAuthToken });

        Assert.Equal(HttpStatusCode.OK, pollResponse.StatusCode);
    }

    [Fact]
    public void GuestCanCreateRequest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);

        HttpResponseMessage createResponse = testingMockProvidersContainer.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = guestAuthToken, Topic = "I need help" });

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.Equal("waiting", createResponse.ReadContentAsJsonDocument().RootElement.GetProperty("status").GetString());
    }
}
