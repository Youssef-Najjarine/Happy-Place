namespace HappyWorld.HappyPlace;

public record ChatMessageListPageResult(string Status, string CallerUserAccountId, List<ChatMessageEntry> Items, List<ChatMessageSenderEntry> Senders, List<ChatMessageReadPointerEntry> ReadPointers, List<string> Typing, string NextCursor, long ChangeSequence) {
    // Methods

    public static ChatMessageListPageResult Ok(string callerUserAccountId, List<ChatMessageEntry> items, List<ChatMessageSenderEntry> senders, List<ChatMessageReadPointerEntry> readPointers, List<string> typing, string nextCursor, long changeSequence) {
        return new ChatMessageListPageResult("ok", callerUserAccountId, items, senders, readPointers, typing, nextCursor, changeSequence);
    }

    public static ChatMessageListPageResult NotMember() {
        return new ChatMessageListPageResult("notMember", null, null, null, null, null, null, 0);
    }

    public static ChatMessageListPageResult GroupGone() {
        return new ChatMessageListPageResult("groupGone", null, null, null, null, null, null, 0);
    }
}
