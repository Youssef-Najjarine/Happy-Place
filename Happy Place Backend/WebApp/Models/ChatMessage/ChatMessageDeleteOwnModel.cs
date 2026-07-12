namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageDeleteOwnModel(string AuthToken, Guid ChatGroupId, Guid MessageId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageDeleteOwnResult DeleteOwn() {
        return ChatMessageManager.DeleteOwn(this.AuthToken, this.ChatGroupId, this.MessageId);
    }
}
