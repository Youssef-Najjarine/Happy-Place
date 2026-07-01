namespace HappyWorld.HappyPlace;

public record HelpJoinResult(string Status, string ChatGroupId, string ChatGroupName) {
    // Methods

    public static HelpJoinResult None() {
        return new HelpJoinResult("none", null, null);
    }

    public static HelpJoinResult Unavailable() {
        return new HelpJoinResult("unavailable", null, null);
    }

    public static HelpJoinResult Joined(Guid chatGroupId, string chatGroupName) {
        return new HelpJoinResult("joined", chatGroupId.ToString(), chatGroupName);
    }
}
