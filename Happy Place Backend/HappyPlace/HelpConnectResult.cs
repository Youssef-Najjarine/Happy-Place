namespace HappyWorld.HappyPlace;

public record HelpConnectResult(string Status, string ChatGroupId, string ChatGroupName) {
    // Methods

    public static HelpConnectResult None() {
        return new HelpConnectResult("none", null, null);
    }

    public static HelpConnectResult NoOffers(Guid chatGroupId, string chatGroupName) {
        return new HelpConnectResult("noOffers", chatGroupId.ToString(), chatGroupName);
    }

    public static HelpConnectResult Connected(Guid chatGroupId, string chatGroupName) {
        return new HelpConnectResult("connected", chatGroupId.ToString(), chatGroupName);
    }
}
