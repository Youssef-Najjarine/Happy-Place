namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupListModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public List<ChatGroupSummaryResult> Load() {
        return ChatGroupManager.ListForUser(this.AuthToken);
    }
}
