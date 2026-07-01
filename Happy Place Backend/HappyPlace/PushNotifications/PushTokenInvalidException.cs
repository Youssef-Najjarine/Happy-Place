namespace HappyWorld.HappyPlace.PushNotifications;

public class PushTokenInvalidException : Exception {
    // Constructors
    public PushTokenInvalidException(string token) : base($"Push token is no longer valid: {token}") {
        this.Token = token;
    }

    // Properties
    public string Token { get; }
}
