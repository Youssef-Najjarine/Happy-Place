namespace HappyWorld.HappyPlace;

public record ChatGroupMuteResult(string Status) {
    // Methods

    public static ChatGroupMuteResult Muted() => new("muted");

    public static ChatGroupMuteResult Unmuted() => new("unmuted");

    public static ChatGroupMuteResult NotMember() => new("notMember");

    public static ChatGroupMuteResult None() => new("none");
}
