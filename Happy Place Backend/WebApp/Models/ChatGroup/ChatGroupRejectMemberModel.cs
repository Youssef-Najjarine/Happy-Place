namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupRejectMemberModel(string AuthToken, Guid ChatGroupId, Guid MemberUserAccountId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupRejectResult RejectMember() {
        return ChatGroupManager.RejectMember(this.AuthToken, this.ChatGroupId, this.MemberUserAccountId);
    }
}
