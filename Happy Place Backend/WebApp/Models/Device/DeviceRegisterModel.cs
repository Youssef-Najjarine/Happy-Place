namespace HappyWorld.HappyPlace.Web.Models.Device;

public record DeviceRegisterModel(string AuthToken, string Token, string Platform, bool Fresh = false) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public bool Register() {
        return DeviceTokenManager.RegisterDevice(this.AuthToken, this.Token, this.Platform, this.Fresh);
    }
}
