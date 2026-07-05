namespace HappyWorld.HappyPlace;

public record ChatGroupJoinRequestResult(string Status) {
    // Methods

    public static ChatGroupJoinRequestResult Requested() {
        return new ChatGroupJoinRequestResult("requested");
    }

    public static ChatGroupJoinRequestResult AlreadyMember() {
        return new ChatGroupJoinRequestResult("alreadyMember");
    }

    public static ChatGroupJoinRequestResult AlreadyRequested() {
        return new ChatGroupJoinRequestResult("alreadyRequested");
    }

    public static ChatGroupJoinRequestResult None() {
        return new ChatGroupJoinRequestResult("none");
    }
}
