namespace HappyWorld.HappyPlace;

public record ChatGroupUnreadTotalResult(string Status, int Total) {
    // Methods

    public static ChatGroupUnreadTotalResult Ok(int total) => new("ok", total);

    public static ChatGroupUnreadTotalResult None() => new("none", 0);
}
