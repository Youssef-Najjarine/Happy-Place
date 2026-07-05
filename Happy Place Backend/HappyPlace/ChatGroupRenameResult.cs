namespace HappyWorld.HappyPlace;

public record ChatGroupRenameResult(string Status, string Title) {
    // Methods

    public static ChatGroupRenameResult None() {
        return new ChatGroupRenameResult("none", null);
    }

    public static ChatGroupRenameResult InvalidName() {
        return new ChatGroupRenameResult("invalidName", null);
    }

    public static ChatGroupRenameResult Renamed(string title) {
        return new ChatGroupRenameResult("renamed", title);
    }
}
