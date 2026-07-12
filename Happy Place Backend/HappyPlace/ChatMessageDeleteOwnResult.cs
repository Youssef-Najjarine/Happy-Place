namespace HappyWorld.HappyPlace;

public record ChatMessageDeleteOwnResult(string Status) {
    // Methods

    public static ChatMessageDeleteOwnResult Deleted() {
        return new ChatMessageDeleteOwnResult("deleted");
    }

    public static ChatMessageDeleteOwnResult NotYours() {
        return new ChatMessageDeleteOwnResult("notYours");
    }

    public static ChatMessageDeleteOwnResult MessageGone() {
        return new ChatMessageDeleteOwnResult("messageGone");
    }

    public static ChatMessageDeleteOwnResult NotMember() {
        return new ChatMessageDeleteOwnResult("notMember");
    }

    public static ChatMessageDeleteOwnResult GroupGone() {
        return new ChatMessageDeleteOwnResult("groupGone");
    }
}
