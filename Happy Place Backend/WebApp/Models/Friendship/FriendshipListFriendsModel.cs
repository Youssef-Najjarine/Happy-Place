namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipListFriendsModel(string AuthToken, string Username, string Search, string Cursor) {
    // Methods

    public FriendListPageResult List() {
        return FriendshipManager.ListFriends(this.AuthToken, this.Username, this.Search, this.Cursor);
    }
}
