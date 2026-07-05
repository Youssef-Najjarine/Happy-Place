namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupMembersModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupMembersResult Load() {
        return ChatGroupManager.ListMembers(this.AuthToken, this.ChatGroupId);
    }
}
