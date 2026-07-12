namespace HappyWorld.HappyPlace;

public record ChatMessageSendResult(string Status, ChatMessageEntry Message) {
    // Methods

    public static ChatMessageSendResult Sent(ChatMessageEntry message) {
        return new ChatMessageSendResult("sent", message);
    }

    public static ChatMessageSendResult Duplicate(ChatMessageEntry message) {
        return new ChatMessageSendResult("duplicate", message);
    }

    public static ChatMessageSendResult NotMember() {
        return new ChatMessageSendResult("notMember", null);
    }

    public static ChatMessageSendResult GroupGone() {
        return new ChatMessageSendResult("groupGone", null);
    }

    public static ChatMessageSendResult InvalidBody() {
        return new ChatMessageSendResult("invalidBody", null);
    }

    public static ChatMessageSendResult InvalidMedia() {
        return new ChatMessageSendResult("invalidMedia", null);
    }
}
