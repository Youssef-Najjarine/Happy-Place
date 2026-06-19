namespace HappyWorld.HappyPlace.Web.Models.HelpRequest;

public record HelpConnectModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpConnectResult Connect() {
        return HelpRequestManager.Connect(this.AuthToken, this.ChatGroupId);
    }
}
