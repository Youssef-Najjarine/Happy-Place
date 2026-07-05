namespace HappyWorld.HappyPlace;

public record ChatGroupRejectResult(string Status) {
    // Methods

    public static ChatGroupRejectResult Rejected() {
        return new ChatGroupRejectResult("rejected");
    }

    public static ChatGroupRejectResult NotPending() {
        return new ChatGroupRejectResult("notPending");
    }

    public static ChatGroupRejectResult None() {
        return new ChatGroupRejectResult("none");
    }
}
