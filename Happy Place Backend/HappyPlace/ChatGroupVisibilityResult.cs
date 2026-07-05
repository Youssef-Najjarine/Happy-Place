namespace HappyWorld.HappyPlace;

public record ChatGroupVisibilityResult(string Status, bool IsPublic) {
    // Methods

    public static ChatGroupVisibilityResult None() {
        return new ChatGroupVisibilityResult("none", false);
    }

    public static ChatGroupVisibilityResult Updated(bool isPublic) {
        return new ChatGroupVisibilityResult("updated", isPublic);
    }
}
