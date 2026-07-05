namespace HappyWorld.HappyPlace;

public record ChatGroupCancelRequestResult(string Status) {
    // Methods

    public static ChatGroupCancelRequestResult Cancelled() {
        return new ChatGroupCancelRequestResult("cancelled");
    }

    public static ChatGroupCancelRequestResult NotRequested() {
        return new ChatGroupCancelRequestResult("notRequested");
    }
}
