namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupSetMutedModel(string AuthToken, Guid ChatGroupId, bool IsMuted) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupMuteResult SetMuted() {
        return ChatGroupManager.SetMuted(this.AuthToken, this.ChatGroupId, this.IsMuted);
    }
}
