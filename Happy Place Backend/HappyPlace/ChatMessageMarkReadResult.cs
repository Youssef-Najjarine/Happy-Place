namespace HappyWorld.HappyPlace;

public record ChatMessageMarkReadResult(string Status, long LastReadSequence) {
    // Methods

    public static ChatMessageMarkReadResult Ok(long lastReadSequence) {
        return new ChatMessageMarkReadResult("ok", lastReadSequence);
    }

    public static ChatMessageMarkReadResult NotMember() {
        return new ChatMessageMarkReadResult("notMember", 0);
    }

    public static ChatMessageMarkReadResult GroupGone() {
        return new ChatMessageMarkReadResult("groupGone", 0);
    }
}
