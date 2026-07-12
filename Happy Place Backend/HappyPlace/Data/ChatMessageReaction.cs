using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatMessageReaction))]
public class ChatMessageReaction {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatMessageId { get; set; }
    public Guid UserAccountId { get; set; }
    public string Emoji { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
