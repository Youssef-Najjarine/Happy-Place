namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipListOutgoingRequestsModel(string AuthToken) {
    // Methods

    public FriendRequestListResult List() {
        return FriendshipManager.ListOutgoingRequests(this.AuthToken);
    }
}
