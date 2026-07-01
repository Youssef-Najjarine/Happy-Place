namespace HappyWorld.HappyPlace.Web.Models.Device;

public record DeviceUnregisterModel(string AuthToken, string Token) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public bool Unregister() {
        return DeviceTokenManager.UnregisterDevice(this.AuthToken, this.Token);
    }
}
