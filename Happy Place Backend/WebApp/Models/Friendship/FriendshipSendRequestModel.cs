namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipSendRequestModel(string AuthToken, string Username) {
    // Methods

    public FriendRequestSendResult Send() {
        return FriendshipManager.SendRequest(this.AuthToken, this.Username);
    }
}
