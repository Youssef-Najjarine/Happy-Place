namespace HappyWorld.HappyPlace.Web.Models.HelpRequest;

public record HelpMyOpenRequestModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpRequestResult Load() {
        return HelpRequestManager.GetMyOpenRequest(this.AuthToken);
    }
}
