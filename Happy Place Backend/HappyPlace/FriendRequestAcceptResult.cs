namespace HappyWorld.HappyPlace;

public record FriendRequestAcceptResult(string Status) {
    // Methods

    public static FriendRequestAcceptResult Accepted() {
        return new FriendRequestAcceptResult("accepted");
    }

    public static FriendRequestAcceptResult AlreadyFriends() {
        return new FriendRequestAcceptResult("alreadyFriends");
    }

    public static FriendRequestAcceptResult AccountRequired() {
        return new FriendRequestAcceptResult("accountRequired");
    }

    public static FriendRequestAcceptResult None() {
        return new FriendRequestAcceptResult("none");
    }
}
