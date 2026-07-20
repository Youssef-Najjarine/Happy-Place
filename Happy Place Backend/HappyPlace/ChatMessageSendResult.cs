namespace HappyWorld.HappyPlace;

public record ChatMessageSendResult(string Status, ChatMessageEntry Message, int? GuestMessagesRemaining) {
    // Methods

    public static ChatMessageSendResult Sent(ChatMessageEntry message, int? guestMessagesRemaining) {
        return new ChatMessageSendResult("sent", message, guestMessagesRemaining);
    }

    public static ChatMessageSendResult Duplicate(ChatMessageEntry message, int? guestMessagesRemaining) {
        return new ChatMessageSendResult("duplicate", message, guestMessagesRemaining);
    }

    public static ChatMessageSendResult NotMember() {
        return new ChatMessageSendResult("notMember", null, null);
    }

    public static ChatMessageSendResult NotFriends() {
        return new ChatMessageSendResult("notFriends", null, null);
    }

    public static ChatMessageSendResult GroupGone() {
        return new ChatMessageSendResult("groupGone", null, null);
    }

    public static ChatMessageSendResult InvalidBody() {
        return new ChatMessageSendResult("invalidBody", null, null);
    }

    public static ChatMessageSendResult InvalidMedia() {
        return new ChatMessageSendResult("invalidMedia", null, null);
    }

    public static ChatMessageSendResult InvalidReply() {
        return new ChatMessageSendResult("invalidReply", null, null);
    }

    public static ChatMessageSendResult GuestLimitReached() {
        return new ChatMessageSendResult("guestLimitReached", null, 0);
    }
}
