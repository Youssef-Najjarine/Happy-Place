namespace HappyWorld.HappyPlace.Web.Models.Friendship;

public record FriendshipSearchUsersModel(string AuthToken, string Query) {
    // Methods

    public UserSearchResult Search() {
        return FriendshipManager.SearchUsers(this.AuthToken, this.Query);
    }
}
