namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageListPageModel(string AuthToken, Guid ChatGroupId, string Cursor) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageListPageResult ListPage() {
        return ChatMessageManager.ListPage(this.AuthToken, this.ChatGroupId, this.Cursor);
    }
}
