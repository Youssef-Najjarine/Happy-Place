namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupListModel(string AuthToken, string SortBy = null, string Search = null) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public List<ChatGroupSummaryResult> Load() {
        return ChatGroupManager.ListForUser(this.AuthToken, this.SortBy, this.Search);
    }
}
