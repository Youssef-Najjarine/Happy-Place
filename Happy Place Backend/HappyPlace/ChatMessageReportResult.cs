namespace HappyWorld.HappyPlace;

public record ChatMessageReportResult(string Status) {
    // Methods

    public static ChatMessageReportResult Reported() {
        return new ChatMessageReportResult("reported");
    }

    public static ChatMessageReportResult AlreadyReported() {
        return new ChatMessageReportResult("alreadyReported");
    }

    public static ChatMessageReportResult CannotReportOwn() {
        return new ChatMessageReportResult("cannotReportOwn");
    }

    public static ChatMessageReportResult InvalidReason() {
        return new ChatMessageReportResult("invalidReason");
    }

    public static ChatMessageReportResult MessageGone() {
        return new ChatMessageReportResult("messageGone");
    }

    public static ChatMessageReportResult NotMember() {
        return new ChatMessageReportResult("notMember");
    }

    public static ChatMessageReportResult GroupGone() {
        return new ChatMessageReportResult("groupGone");
    }
}
