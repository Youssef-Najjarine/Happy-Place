namespace HappyWorld.HappyPlace;

public record ChatMessageReplyContextEntry(string MessageId, string SenderUserAccountId, string SenderDisplayName, byte Kind, string Preview, bool IsDeleted);
