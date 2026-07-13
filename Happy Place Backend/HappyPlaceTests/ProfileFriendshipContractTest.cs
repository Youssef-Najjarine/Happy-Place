using System.Text.Json;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ProfileFriendshipContractTest {
    // Tests - Friendship Status

    [Fact]
    public void PublicProfileShowsNoneBetweenStrangers() {
        using var container = new TestingMockProvidersContainer();
        string viewerAuthToken = FriendshipTestActions.CreateUser(container, "Viewer");
        string targetAuthToken = FriendshipTestActions.CreateUser(container, "Target");

        JsonDocument profileData = ReadProfile(container, viewerAuthToken, FriendshipTestActions.ResolveUsername(targetAuthToken));

        Assert.Equal("none", profileData.RootElement.GetProperty("friendshipStatus").GetString());
    }

    [Fact]
    public void PublicProfileShowsRequestSentAndRequestReceived() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        JsonDocument requesterView = ReadProfile(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);
        JsonDocument addresseeView = ReadProfile(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal("requestSent", requesterView.RootElement.GetProperty("friendshipStatus").GetString());
        Assert.Equal("requestReceived", addresseeView.RootElement.GetProperty("friendshipStatus").GetString());
    }

    [Fact]
    public void PublicProfileShowsFriendsInBothDirections() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);

        JsonDocument requesterView = ReadProfile(container, friends.RequesterAuthToken, friends.AddresseeUsername);
        JsonDocument addresseeView = ReadProfile(container, friends.AddresseeAuthToken, friends.RequesterUsername);

        Assert.Equal("friends", requesterView.RootElement.GetProperty("friendshipStatus").GetString());
        Assert.Equal("friends", addresseeView.RootElement.GetProperty("friendshipStatus").GetString());
    }

    [Fact]
    public void PublicProfileShowsSelfForYourOwnUsername() {
        using var container = new TestingMockProvidersContainer();
        string authToken = FriendshipTestActions.CreateUser(container, "SelfViewer");

        JsonDocument profileData = ReadProfile(container, authToken, FriendshipTestActions.ResolveUsername(authToken));

        Assert.Equal("self", profileData.RootElement.GetProperty("friendshipStatus").GetString());
    }

    [Fact]
    public void FriendshipStatusResetsToNoneAfterUnfriend() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        FriendshipTestActions.Unfriend(container, friends.RequesterAuthToken, friends.AddresseeUsername).EnsureSuccessStatusCode();

        JsonDocument profileData = ReadProfile(container, friends.RequesterAuthToken, friends.AddresseeUsername);

        Assert.Equal("none", profileData.RootElement.GetProperty("friendshipStatus").GetString());
    }

    // Tests - Friend Count

    [Fact]
    public void PublicFriendCountMovesWithAcceptAndUnfriend() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        int countBeforeAccept = ReadProfile(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).RootElement.GetProperty("friendCount").GetInt32();
        FriendshipTestActions.AcceptRequest(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername).EnsureSuccessStatusCode();
        int countAfterAccept = ReadProfile(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).RootElement.GetProperty("friendCount").GetInt32();
        FriendshipTestActions.Unfriend(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).EnsureSuccessStatusCode();
        int countAfterUnfriend = ReadProfile(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername).RootElement.GetProperty("friendCount").GetInt32();

        Assert.Equal(0, countBeforeAccept);
        Assert.Equal(1, countAfterAccept);
        Assert.Equal(0, countAfterUnfriend);
    }

    [Fact]
    public void PendingRequestsDoNotCountAsFriends() {
        using var container = new TestingMockProvidersContainer();
        var pendingPair = FriendshipTestActions.CreatePendingPair(container);

        JsonDocument addresseeProfile = ReadProfile(container, pendingPair.RequesterAuthToken, pendingPair.AddresseeUsername);
        JsonDocument requesterProfile = ReadProfile(container, pendingPair.AddresseeAuthToken, pendingPair.RequesterUsername);

        Assert.Equal(0, addresseeProfile.RootElement.GetProperty("friendCount").GetInt32());
        Assert.Equal(0, requesterProfile.RootElement.GetProperty("friendCount").GetInt32());
    }

    [Fact]
    public void FriendCountIsVisibleToStrangers() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        string strangerAuthToken = FriendshipTestActions.CreateUser(container, "Stranger");

        JsonDocument profileData = ReadProfile(container, strangerAuthToken, friends.RequesterUsername);

        Assert.Equal(1, profileData.RootElement.GetProperty("friendCount").GetInt32());
    }

    [Fact]
    public void GetMyProfileIncludesFriendCount() {
        using var container = new TestingMockProvidersContainer();
        var friends = FriendshipTestActions.CreateFriends(container);
        string lonerAuthToken = FriendshipTestActions.CreateUser(container, "Loner");

        HttpResponseMessage friendProfileResponse = ProfileTestActions.GetMyProfile(container, friends.RequesterAuthToken);
        HttpResponseMessage lonerProfileResponse = ProfileTestActions.GetMyProfile(container, lonerAuthToken);

        Assert.Equal(1, friendProfileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("friendCount").GetInt32());
        Assert.Equal(0, lonerProfileResponse.ReadContentAsJsonDocument().RootElement.GetProperty("friendCount").GetInt32());
    }

    // Helpers

    private static JsonDocument ReadProfile(TestingMockProvidersContainer container, string authToken, string username) {
        HttpResponseMessage response = ProfileTestActions.GetPublicUserProfile(container, authToken, username);
        response.EnsureSuccessStatusCode();
        return response.ReadContentAsJsonDocument();
    }
}
