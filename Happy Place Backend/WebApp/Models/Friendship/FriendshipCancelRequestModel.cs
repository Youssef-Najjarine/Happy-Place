namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipCancelRequestModel(string AuthToken, string Username) {
    // Methods

    public FriendRequestCancelResult Cancel() {
        return FriendshipManager.CancelRequest(this.AuthToken, this.Username);
    }
}
