namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipAcceptRequestModel(string AuthToken, string Username) {
    // Methods

    public FriendRequestAcceptResult Accept() {
        return FriendshipManager.AcceptRequest(this.AuthToken, this.Username);
    }
}
