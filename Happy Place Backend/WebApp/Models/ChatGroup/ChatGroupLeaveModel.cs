namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupLeaveModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupLeaveResult Leave() {
        return ChatGroupManager.Leave(this.AuthToken, this.ChatGroupId);
    }
}
