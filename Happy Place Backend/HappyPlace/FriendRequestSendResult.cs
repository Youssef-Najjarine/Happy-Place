namespace HappyWorld.HappyPlace;

public record FriendRequestSendResult(string Status) {
    // Methods

    public static FriendRequestSendResult Requested() {
        return new FriendRequestSendResult("requested");
    }

    public static FriendRequestSendResult Accepted() {
        return new FriendRequestSendResult("accepted");
    }

    public static FriendRequestSendResult AlreadyRequested() {
        return new FriendRequestSendResult("alreadyRequested");
    }

    public static FriendRequestSendResult AlreadyFriends() {
        return new FriendRequestSendResult("alreadyFriends");
    }

    public static FriendRequestSendResult AccountRequired() {
        return new FriendRequestSendResult("accountRequired");
    }

    public static FriendRequestSendResult RateLimited() {
        return new FriendRequestSendResult("rateLimited");
    }

    public static FriendRequestSendResult None() {
        return new FriendRequestSendResult("none");
    }
}
