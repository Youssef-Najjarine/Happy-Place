namespace HappyWorld.HappyPlace;

public record BlockUserResult(string Status) {
    // Methods

    public static BlockUserResult Blocked() {
        return new BlockUserResult("blocked");
    }

    public static BlockUserResult AccountRequired() {
        return new BlockUserResult("accountRequired");
    }

    public static BlockUserResult None() {
        return new BlockUserResult("none");
    }
}
