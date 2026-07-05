namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupRenameModel(string AuthToken, Guid ChatGroupId, string Name) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupRenameResult Rename() {
        return ChatGroupManager.Rename(this.AuthToken, this.ChatGroupId, this.Name);
    }
}
