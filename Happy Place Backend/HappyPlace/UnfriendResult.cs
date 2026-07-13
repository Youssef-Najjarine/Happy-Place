namespace HappyWorld.HappyPlace;

public record UnfriendResult(string Status) {
    // Methods

    public static UnfriendResult Unfriended() {
        return new UnfriendResult("unfriended");
    }

    public static UnfriendResult AccountRequired() {
        return new UnfriendResult("accountRequired");
    }

    public static UnfriendResult None() {
        return new UnfriendResult("none");
    }
}
