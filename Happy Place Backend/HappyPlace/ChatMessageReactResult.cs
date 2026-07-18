namespace HappyWorld.HappyPlace;

public record ChatMessageReactResult(string Status) {
    // Methods

    public static ChatMessageReactResult Reacted() {
        return new ChatMessageReactResult("reacted");
    }

    public static ChatMessageReactResult Removed() {
        return new ChatMessageReactResult("removed");
    }

    public static ChatMessageReactResult InvalidEmoji() {
        return new ChatMessageReactResult("invalidEmoji");
    }

    public static ChatMessageReactResult MessageGone() {
        return new ChatMessageReactResult("messageGone");
    }

    public static ChatMessageReactResult NotMember() {
        return new ChatMessageReactResult("notMember");
    }

    public static ChatMessageReactResult NotFriends() {
        return new ChatMessageReactResult("notFriends");
    }

    public static ChatMessageReactResult GroupGone() {
        return new ChatMessageReactResult("groupGone");
    }
}
