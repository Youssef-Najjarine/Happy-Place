namespace HappyWorld.HappyPlace;

public record ChatGroupDeleteResult(string Status) {
    // Methods

    public static ChatGroupDeleteResult None() {
        return new ChatGroupDeleteResult("none");
    }

    public static ChatGroupDeleteResult Deleted() {
        return new ChatGroupDeleteResult("deleted");
    }
}
