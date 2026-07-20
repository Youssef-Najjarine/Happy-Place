using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatMessage))]
public class ChatMessage {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatGroupId { get; set; }
    public Guid? SenderUserAccountId { get; set; }
    public Guid ClientMessageId { get; set; }
    public Guid? ReplyToChatMessageId { get; set; }
    public ChatMessageKind Kind { get; set; }
    public Byte[] BodyCipher { get; set; }
    public Byte CipherVersion { get; set; }
    public Int64 Sequence { get; set; }
    public Int64 ChangeSequence { get; set; }
    public Boolean IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
