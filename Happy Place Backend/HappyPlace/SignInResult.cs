namespace HappyWorld.HappyPlace;

public class SignInResult {
    // Properties
    public string Status { get; private set; }
    public string AuthToken { get; private set; }
    public string Contact { get; private set; }
    public string ContactType { get; private set; }

    // Methods
    internal static SignInResult AsVerified(string authToken) {
        return new() { Status = "verified", AuthToken = authToken };
    }

    internal static SignInResult AsPending(string contact, string contactType) {
        return new() { Status = "pending", Contact = contact, ContactType = contactType };
    }
}
