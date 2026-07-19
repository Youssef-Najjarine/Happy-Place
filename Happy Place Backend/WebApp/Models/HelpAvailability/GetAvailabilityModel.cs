namespace HappyWorld.HappyPlace.Web.Models.HelpAvailability;

public record GetAvailabilityModel(string AuthToken) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public HelpAvailabilityStatusResult Read() {
        return HelpAvailabilityManager.GetAvailability(this.AuthToken);
    }
}
