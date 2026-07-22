namespace HappyWorld.HappyPlace.Web.Models.HelpAvailability;

public record SetAvailabilityModel(string AuthToken, bool IsAvailable) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpAvailabilityStatusResult Apply() {
        return HelpAvailabilityManager.SetAvailability(this.AuthToken, this.IsAvailable);
    }
}
