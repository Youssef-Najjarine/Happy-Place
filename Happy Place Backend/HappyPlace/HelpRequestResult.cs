namespace HappyWorld.HappyPlace;

public record HelpRequestResult(string Status, string ChatGroupId, string ChatGroupName) {
    // Methods

    public static HelpRequestResult None() {
        return new HelpRequestResult("none", null, null);
    }

    public static HelpRequestResult RegistrationRequired() {
        return new HelpRequestResult("registrationRequired", null, null);
    }

    public static HelpRequestResult Waiting(Guid chatGroupId, string chatGroupName) {
        return new HelpRequestResult("waiting", chatGroupId.ToString(), chatGroupName);
    }
}
