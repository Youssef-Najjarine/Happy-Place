namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupRequestToJoinModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupJoinRequestResult RequestToJoin() {
        return ChatGroupManager.RequestToJoin(this.AuthToken, this.ChatGroupId);
    }
}
