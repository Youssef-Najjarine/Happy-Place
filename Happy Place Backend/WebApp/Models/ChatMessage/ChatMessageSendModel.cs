namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageSendModel(string AuthToken, Guid ChatGroupId, Guid ClientMessageId, string Body, Guid MediaId, Guid ReplyToMessageId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageSendResult Send() {
        return ChatMessageManager.Send(this.AuthToken, this.ChatGroupId, this.ClientMessageId, this.Body, this.MediaId, this.ReplyToMessageId);
    }
}
