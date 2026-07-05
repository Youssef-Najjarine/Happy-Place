namespace HappyWorld.HappyPlace;

public record ChatGroupApproveResult(string Status) {
    // Methods

    public static ChatGroupApproveResult Approved() {
        return new ChatGroupApproveResult("approved");
    }

    public static ChatGroupApproveResult AlreadyMember() {
        return new ChatGroupApproveResult("alreadyMember");
    }

    public static ChatGroupApproveResult NotPending() {
        return new ChatGroupApproveResult("notPending");
    }

    public static ChatGroupApproveResult None() {
        return new ChatGroupApproveResult("none");
    }
}
