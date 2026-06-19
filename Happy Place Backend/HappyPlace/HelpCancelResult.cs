namespace HappyWorld.HappyPlace;

public record HelpCancelResult(string Status) {
    // Methods

    public static HelpCancelResult None() {
        return new HelpCancelResult("none");
    }

    public static HelpCancelResult Cancelled() {
        return new HelpCancelResult("cancelled");
    }
}
