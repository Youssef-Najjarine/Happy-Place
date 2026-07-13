namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipUnblockModel(string AuthToken, string Username) {
    // Methods

    public UnblockUserResult Unblock() {
        return FriendshipManager.Unblock(this.AuthToken, this.Username);
    }
}
