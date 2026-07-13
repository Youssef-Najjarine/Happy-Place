namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipUnfriendModel(string AuthToken, string Username) {
    // Methods

    public UnfriendResult Unfriend() {
        return FriendshipManager.Unfriend(this.AuthToken, this.Username);
    }
}
