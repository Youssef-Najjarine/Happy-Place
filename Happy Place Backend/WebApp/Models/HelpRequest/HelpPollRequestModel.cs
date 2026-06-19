namespace HappyWorld.HappyPlace.Web.Models.HelpRequest;

public record HelpPollRequestModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpRequestStatusResult Poll() {
        return HelpRequestManager.GetRequestStatus(this.AuthToken, this.ChatGroupId);
    }
}
