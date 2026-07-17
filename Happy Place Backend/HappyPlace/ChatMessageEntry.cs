namespace HappyWorld.HappyPlace;

public record ChatMessageEntry(string Id, string ClientMessageId, long Sequence, string SenderUserAccountId, byte Kind, string Body, bool IsDeleted, List<ChatMessageReactionEntry> Reactions, string MediaUrl, int? MediaWidth, int? MediaHeight, int? MediaDurationSeconds, DateTime CreatedAtUtc);
