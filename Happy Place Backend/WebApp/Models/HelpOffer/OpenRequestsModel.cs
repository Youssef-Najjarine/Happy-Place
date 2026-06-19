namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record OpenRequestsModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public List<OpenHelpRequest> Load() {
        return HelpOfferManager.GetOpenRequestsForHelper(this.AuthToken);
    }
}
