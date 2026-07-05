namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupDeleteModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupDeleteResult Delete() {
        return ChatGroupManager.Delete(this.AuthToken, this.ChatGroupId);
    }
}
