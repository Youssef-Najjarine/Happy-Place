namespace HappyWorld.HappyPlace;

public record FriendRequestDeclineResult(string Status) {
    // Methods

    public static FriendRequestDeclineResult Declined() {
        return new FriendRequestDeclineResult("declined");
    }

    public static FriendRequestDeclineResult AccountRequired() {
        return new FriendRequestDeclineResult("accountRequired");
    }

    public static FriendRequestDeclineResult None() {
        return new FriendRequestDeclineResult("none");
    }
}
