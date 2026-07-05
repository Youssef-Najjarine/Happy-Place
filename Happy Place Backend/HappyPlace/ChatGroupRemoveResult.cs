namespace HappyWorld.HappyPlace;

public record ChatGroupRemoveResult(string Status) {
    // Methods

    public static ChatGroupRemoveResult Removed() {
        return new ChatGroupRemoveResult("removed");
    }

    public static ChatGroupRemoveResult CannotRemoveOwner() {
        return new ChatGroupRemoveResult("cannotRemoveOwner");
    }

    public static ChatGroupRemoveResult NotMember() {
        return new ChatGroupRemoveResult("notMember");
    }

    public static ChatGroupRemoveResult None() {
        return new ChatGroupRemoveResult("none");
    }
}
