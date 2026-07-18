namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupHideModel(string AuthToken, Guid ChatGroupId) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupHideResult Hide() {
        return ChatGroupManager.Hide(this.AuthToken, this.ChatGroupId);
    }
}
