namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record DeclineInviteModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpOfferResult DeclineInvite() {
        return HelpOfferManager.DeclineInvite(this.AuthToken, this.ChatGroupId);
    }
}
