namespace HappyWorld.HappyPlace;

public record HelpOfferStatusResult(string Status, string ChatGroupId, string ChatGroupName) {
    // Methods

    public static HelpOfferStatusResult None() {
        return new HelpOfferStatusResult("none", null, null);
    }

    public static HelpOfferStatusResult Offered() {
        return new HelpOfferStatusResult("offered", null, null);
    }

    public static HelpOfferStatusResult Connected(Guid chatGroupId, string chatGroupName) {
        return new HelpOfferStatusResult("connected", chatGroupId.ToString(), chatGroupName);
    }
}
