namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageMarkReadModel(string AuthToken, Guid ChatGroupId, long UpToSequence) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageMarkReadResult MarkRead() {
        return ChatMessageManager.MarkRead(this.AuthToken, this.ChatGroupId, this.UpToSequence);
    }
}
