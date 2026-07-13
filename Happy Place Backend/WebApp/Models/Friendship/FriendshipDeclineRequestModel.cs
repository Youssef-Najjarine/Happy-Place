namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipDeclineRequestModel(string AuthToken, string Username) {
    // Methods

    public FriendRequestDeclineResult Decline() {
        return FriendshipManager.DeclineRequest(this.AuthToken, this.Username);
    }
}
