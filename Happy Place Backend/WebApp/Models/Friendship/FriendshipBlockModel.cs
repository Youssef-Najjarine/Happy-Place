namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipBlockModel(string AuthToken, string Username) {
    // Methods

    public BlockUserResult Block() {
        return FriendshipManager.Block(this.AuthToken, this.Username);
    }
}
