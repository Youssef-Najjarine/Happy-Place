namespace HappyWorld.HappyPlace;

public record ChatGroupOpenDirectResult(string Status, string ChatGroupId) {
    // Methods

    public static ChatGroupOpenDirectResult Opened(Guid chatGroupId) => new("opened", chatGroupId.ToString());

    public static ChatGroupOpenDirectResult NotFriends() => new("notFriends", null);

    public static ChatGroupOpenDirectResult AccountRequired() => new("accountRequired", null);

    public static ChatGroupOpenDirectResult None() => new("none", null);
}
