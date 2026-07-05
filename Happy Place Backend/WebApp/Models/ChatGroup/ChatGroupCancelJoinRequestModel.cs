namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupCancelJoinRequestModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupCancelRequestResult CancelJoinRequest() {
        return ChatGroupManager.CancelJoinRequest(this.AuthToken, this.ChatGroupId);
    }
}
