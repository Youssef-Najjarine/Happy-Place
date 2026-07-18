namespace HappyWorld.HappyPlace;

public record ChatGroupCreateWithFriendsResult(string Status, string ChatGroupId) {
    // Methods

    public static ChatGroupCreateWithFriendsResult Created(Guid chatGroupId) => new("created", chatGroupId.ToString());

    public static ChatGroupCreateWithFriendsResult AccountRequired() => new("accountRequired", null);

    public static ChatGroupCreateWithFriendsResult InvalidName() => new("invalidName", null);

    public static ChatGroupCreateWithFriendsResult NotFriends() => new("notFriends", null);

    public static ChatGroupCreateWithFriendsResult None() => new("none", null);
}
