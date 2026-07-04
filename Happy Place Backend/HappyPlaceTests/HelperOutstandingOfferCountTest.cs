using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class HelperOutstandingOfferCountTest {
    // Tests

    [Fact]
    public void AHelperWithNoOffersHasAnOutstandingCountOfZero() {
        using var container = new TestingMockProvidersContainer();
        CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);

        Assert.Equal(0, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void OfferingToOneRequestMakesTheOutstandingCountOne() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);

        Assert.Equal(1, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void TheOutstandingCountReflectsEveryRequestOfferedTo() {
        using var container = new TestingMockProvidersContainer();
        string helper = CreateGuest(container);
        CreateOffer(container, helper, CreateRequest(container, CreateGuest(container), "First"));
        CreateOffer(container, helper, CreateRequest(container, CreateGuest(container), "Second"));
        CreateOffer(container, helper, CreateRequest(container, CreateGuest(container), "Third"));

        Assert.Equal(3, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void GoingUnavailableDropsTheOutstandingCountToZero() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, chatGroupId);

        SetAvailable(container, helper, false);

        Assert.Equal(0, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void WithdrawingOneOfferLeavesTheOthersCounted() {
        using var container = new TestingMockProvidersContainer();
        Guid firstRequest = CreateRequest(container, CreateGuest(container), "First");
        Guid secondRequest = CreateRequest(container, CreateGuest(container), "Second");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, firstRequest);
        CreateOffer(container, helper, secondRequest);

        WithdrawOffer(container, helper, firstRequest);

        Assert.Equal(1, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void AConnectedOfferIsNoLongerCountedAsOutstanding() {
        using var container = new TestingMockProvidersContainer();
        string seeker = CreateGuest(container);
        Guid connectedRequest = CreateRequest(container, seeker, "Connecting");
        Guid stillOpenRequest = CreateRequest(container, CreateGuest(container), "Still open");
        string helper = CreateGuest(container);
        CreateOffer(container, helper, connectedRequest);
        CreateOffer(container, helper, stillOpenRequest);
        Connect(container, seeker, connectedRequest);

        Assert.Equal(1, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void DecliningARequestIsNotCountedAsAnOutstandingOffer() {
        using var container = new TestingMockProvidersContainer();
        Guid chatGroupId = CreateRequest(container, CreateGuest(container), "I need help");
        string helper = CreateGuest(container);
        DeclineOffer(container, helper, chatGroupId);

        Assert.Equal(0, OutstandingOfferCount(container, helper));
    }

    [Fact]
    public void AHelpersOwnRequestIsNotCountedAsAnOutstandingOffer() {
        using var container = new TestingMockProvidersContainer();
        string helper = CreateGuest(container);
        CreateRequest(container, helper, "I need help too");
        Guid otherRequest = CreateRequest(container, CreateGuest(container), "Someone else");
        CreateOffer(container, helper, otherRequest);

        Assert.Equal(1, OutstandingOfferCount(container, helper));
    }

    // Helpers - the count exactly as the app derives it: "offered" entries in the openRequests feed

    private static int OutstandingOfferCount(TestingMockProvidersContainer container, string helperAuthToken) {
        var feed = container.WebClient.PostJson("api/helpOffer/openRequests", new { AuthToken = helperAuthToken })
            .ReadContentAsJsonDocument().RootElement;
        int count = 0;
        foreach (var entry in feed.EnumerateArray()) {
            if (entry.GetProperty("offerStatus").GetString() == "offered")
                count++;
        }
        return count;
    }

    private static string CreateGuest(TestingMockProvidersContainer container) {
        return TestUserFactory.CreateGuestUser(container);
    }

    private static Guid CreateRequest(TestingMockProvidersContainer container, string authToken, string topic) {
        string chatGroupId = container.WebClient.PostJson("api/helpRequest/createRequest", new { AuthToken = authToken, Topic = topic })
            .ReadContentAsJsonDocument().RootElement.GetProperty("chatGroupId").GetString();
        return Guid.Parse(chatGroupId);
    }

    private static void CreateOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/createOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void DeclineOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/declineOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void WithdrawOffer(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpOffer/withdrawOffer", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void Connect(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        container.WebClient.PostJson("api/helpRequest/connect", new { AuthToken = authToken, ChatGroupId = chatGroupId.ToString() }).EnsureSuccessStatusCode();
    }

    private static void SetAvailable(TestingMockProvidersContainer container, string authToken, bool isAvailable) {
        container.WebClient.PostJson("api/helpAvailability/setAvailability", new { AuthToken = authToken, IsAvailable = isAvailable }).EnsureSuccessStatusCode();
    }
}
