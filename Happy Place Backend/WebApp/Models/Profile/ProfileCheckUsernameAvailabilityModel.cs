namespace HappyWorld.HappyPlace.Web.Models.Profile;

public record ProfileCheckUsernameAvailabilityModel(string AuthToken, string Username) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public UsernameAvailabilityResult Check() {
        return UserProfileManager.CheckUsernameAvailability(this.AuthToken, this.Username);
    }
}
