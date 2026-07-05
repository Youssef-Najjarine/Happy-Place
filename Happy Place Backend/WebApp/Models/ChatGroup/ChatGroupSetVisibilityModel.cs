namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupSetVisibilityModel(string AuthToken, Guid ChatGroupId, bool IsPublic) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupVisibilityResult SetVisibility() {
        return ChatGroupManager.SetVisibility(this.AuthToken, this.ChatGroupId, this.IsPublic);
    }
}
