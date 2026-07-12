namespace HappyWorld.HappyPlace;

public record ChatMediaUploadResult(string Status, string MediaId, string Url, int? Width, int? Height, int? DurationSeconds) {
    // Methods

    public static ChatMediaUploadResult Uploaded(Guid mediaId, string url, int? width, int? height, int? durationSeconds) {
        return new ChatMediaUploadResult("uploaded", mediaId.ToString(), url, width, height, durationSeconds);
    }

    public static ChatMediaUploadResult InvalidKind() {
        return new ChatMediaUploadResult("invalidKind", null, null, null, null, null);
    }

    public static ChatMediaUploadResult InvalidMedia() {
        return new ChatMediaUploadResult("invalidMedia", null, null, null, null, null);
    }

    public static ChatMediaUploadResult InvalidDuration() {
        return new ChatMediaUploadResult("invalidDuration", null, null, null, null, null);
    }

    public static ChatMediaUploadResult TooLarge() {
        return new ChatMediaUploadResult("tooLarge", null, null, null, null, null);
    }

    public static ChatMediaUploadResult NotMember() {
        return new ChatMediaUploadResult("notMember", null, null, null, null, null);
    }

    public static ChatMediaUploadResult GroupGone() {
        return new ChatMediaUploadResult("groupGone", null, null, null, null, null);
    }
}
