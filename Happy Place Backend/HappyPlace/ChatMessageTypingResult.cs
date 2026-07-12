namespace HappyWorld.HappyPlace;

public record ChatMessageTypingResult(string Status) {
    // Methods

    public static ChatMessageTypingResult Ok() {
        return new ChatMessageTypingResult("ok");
    }

    public static ChatMessageTypingResult NotMember() {
        return new ChatMessageTypingResult("notMember");
    }

    public static ChatMessageTypingResult GroupGone() {
        return new ChatMessageTypingResult("groupGone");
    }
}
