namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipListBlockedModel(string AuthToken) {
    // Methods

    public UserBlockListResult List() {
        return FriendshipManager.ListBlocked(this.AuthToken);
    }
}
