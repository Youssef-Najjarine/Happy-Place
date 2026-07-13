namespace HappyWorld.HappyPlace;

public record FriendRequestCancelResult(string Status) {
    // Methods

    public static FriendRequestCancelResult Canceled() {
        return new FriendRequestCancelResult("canceled");
    }

    public static FriendRequestCancelResult AccountRequired() {
        return new FriendRequestCancelResult("accountRequired");
    }

    public static FriendRequestCancelResult None() {
        return new FriendRequestCancelResult("none");
    }
}
