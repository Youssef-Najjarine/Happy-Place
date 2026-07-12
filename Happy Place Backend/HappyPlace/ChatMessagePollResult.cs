namespace HappyWorld.HappyPlace;

public record ChatMessagePollResult(string Status, List<ChatMessageEntry> Changes, List<ChatMessageSenderEntry> Senders, List<ChatMessageReadPointerEntry> ReadPointers, List<string> Typing, long ChangeSequence) {
    // Methods

    public static ChatMessagePollResult Ok(List<ChatMessageEntry> changes, List<ChatMessageSenderEntry> senders, List<ChatMessageReadPointerEntry> readPointers, List<string> typing, long changeSequence) {
        return new ChatMessagePollResult("ok", changes, senders, readPointers, typing, changeSequence);
    }

    public static ChatMessagePollResult NotMember() {
        return new ChatMessagePollResult("notMember", null, null, null, null, 0);
    }

    public static ChatMessagePollResult GroupGone() {
        return new ChatMessagePollResult("groupGone", null, null, null, null, 0);
    }
}
