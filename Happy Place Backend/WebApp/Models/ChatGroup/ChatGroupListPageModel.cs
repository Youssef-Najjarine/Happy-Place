namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupListPageModel(string AuthToken, string SortBy = null, string Search = null, string Cursor = null) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupPageResult Load() {
        return ChatGroupManager.ListPageForUser(this.AuthToken, this.SortBy, this.Search, this.Cursor);
    }
}
