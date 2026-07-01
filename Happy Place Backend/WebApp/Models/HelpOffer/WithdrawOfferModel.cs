namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record WithdrawOfferModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpOfferResult Withdraw() {
        return HelpOfferManager.WithdrawOffer(this.AuthToken, this.ChatGroupId);
    }
}
