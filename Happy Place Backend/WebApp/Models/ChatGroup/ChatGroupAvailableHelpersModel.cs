namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupAvailableHelpersModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public List<AvailableHelperResult> Load() {
        return ChatGroupManager.ListAvailableHelpers(this.AuthToken);
    }
}
