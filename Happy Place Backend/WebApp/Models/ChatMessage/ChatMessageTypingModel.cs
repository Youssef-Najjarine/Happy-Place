namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageTypingModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageTypingResult Typing() {
        return ChatMessageManager.Typing(this.AuthToken, this.ChatGroupId);
    }
}
