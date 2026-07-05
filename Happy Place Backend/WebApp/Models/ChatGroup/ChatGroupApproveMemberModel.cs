namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupApproveMemberModel(string AuthToken, Guid ChatGroupId, Guid MemberUserAccountId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupApproveResult ApproveMember() {
        return ChatGroupManager.ApproveMember(this.AuthToken, this.ChatGroupId, this.MemberUserAccountId);
    }
}
