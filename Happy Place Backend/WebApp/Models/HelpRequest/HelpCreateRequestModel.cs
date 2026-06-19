namespace HappyWorld.HappyPlace.Web.Models.HelpRequest;

public record HelpCreateRequestModel(string AuthToken, string Topic) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpRequestResult Create() {
        return HelpRequestManager.CreateRequest(this.AuthToken, this.Topic);
    }
}
