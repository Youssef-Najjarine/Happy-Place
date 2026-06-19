namespace HappyWorld.HappyPlace.Web.Models.HelpRequest;

public record HelpCancelModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpCancelResult Cancel() {
        return HelpRequestManager.CancelRequest(this.AuthToken, this.ChatGroupId);
    }
}
