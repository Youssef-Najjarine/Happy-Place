namespace HappyWorld.HappyPlace.Web.Models.HelpOffer;

public record HelpJoinModel(string AuthToken, Guid ChatGroupId) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpJoinResult Join() {
        return HelpOfferManager.JoinGroup(this.AuthToken, this.ChatGroupId);
    }
}
