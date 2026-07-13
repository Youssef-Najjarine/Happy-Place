namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipListIncomingRequestsModel(string AuthToken) {
    // Methods

    public FriendRequestListResult List() {
        return FriendshipManager.ListIncomingRequests(this.AuthToken);
    }
}
