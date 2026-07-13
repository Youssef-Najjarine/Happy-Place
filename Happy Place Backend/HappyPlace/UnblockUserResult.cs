namespace HappyWorld.HappyPlace;

public record UnblockUserResult(string Status) {
    // Methods

    public static UnblockUserResult Unblocked() {
        return new UnblockUserResult("unblocked");
    }

    public static UnblockUserResult AccountRequired() {
        return new UnblockUserResult("accountRequired");
    }

    public static UnblockUserResult None() {
        return new UnblockUserResult("none");
    }
}
