namespace HappyWorld.HappyPlace;

public record ChatGroupHideResult(string Status) {
    // Methods

    public static ChatGroupHideResult Hidden() => new("hidden");

    public static ChatGroupHideResult NotAllowed() => new("notAllowed");

    public static ChatGroupHideResult NotMember() => new("notMember");

    public static ChatGroupHideResult None() => new("none");
}
