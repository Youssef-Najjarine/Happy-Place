namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageReactModel(string AuthToken, Guid ChatGroupId, Guid MessageId, string Emoji) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageReactResult React() {
        return ChatMessageManager.React(this.AuthToken, this.ChatGroupId, this.MessageId, this.Emoji);
    }
}
