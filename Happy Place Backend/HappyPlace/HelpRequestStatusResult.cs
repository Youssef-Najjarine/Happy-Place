namespace HappyWorld.HappyPlace;

public record HelpRequestStatusResult(string Status, int ReadyHelperCount, string ChatGroupId, string ChatGroupName) {
    // Methods

    public static HelpRequestStatusResult None() {
        return new HelpRequestStatusResult("none", 0, null, null);
    }

    public static HelpRequestStatusResult Waiting(Guid chatGroupId, string chatGroupName, int readyHelperCount) {
        return new HelpRequestStatusResult("waiting", readyHelperCount, chatGroupId.ToString(), chatGroupName);
    }

    public static HelpRequestStatusResult Connected(Guid chatGroupId, string chatGroupName) {
        return new HelpRequestStatusResult("connected", 0, chatGroupId.ToString(), chatGroupName);
    }
}
