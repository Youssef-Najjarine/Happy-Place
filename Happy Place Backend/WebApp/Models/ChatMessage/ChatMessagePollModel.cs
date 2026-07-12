namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessagePollModel(string AuthToken, Guid ChatGroupId, long SinceChangeSequence) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessagePollResult Poll() {
        return ChatMessageManager.Poll(this.AuthToken, this.ChatGroupId, this.SinceChangeSequence);
    }
}
