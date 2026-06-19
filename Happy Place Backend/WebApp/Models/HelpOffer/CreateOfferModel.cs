namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record CreateOfferModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpOfferResult Create() {
        return HelpOfferManager.CreateOffer(this.AuthToken, this.ChatGroupId);
    }
}
