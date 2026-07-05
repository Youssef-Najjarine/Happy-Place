namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupRemoveMemberModel(string AuthToken, Guid ChatGroupId, Guid MemberUserAccountId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupRemoveResult RemoveMember() {
        return ChatGroupManager.RemoveMember(this.AuthToken, this.ChatGroupId, this.MemberUserAccountId);
    }
}
