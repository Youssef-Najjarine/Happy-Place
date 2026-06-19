namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record DeclineOfferModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpOfferResult Decline() {
        return HelpOfferManager.DeclineOffer(this.AuthToken, this.ChatGroupId);
    }
}
