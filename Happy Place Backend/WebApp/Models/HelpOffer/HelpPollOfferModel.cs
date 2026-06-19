namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record HelpPollOfferModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpOfferStatusResult Poll() {
        return HelpOfferManager.GetConnectionStatus(this.AuthToken);
    }
}
