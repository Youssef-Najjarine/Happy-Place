using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(NotificationChannel))]
public class NotificationChannel {
    // Properties
    public Guid Id { get; set; }
    public Guid RecipientUserAccountId { get; set; }
    public NotificationChannelKind Kind { get; set; }
    public Guid? ScopeChatGroupId { get; set; }
    public int LastSentCount { get; set; }
    public Boolean IsLive { get; set; }
    public DateTime? FirstDirtyAtUtc { get; set; }
    public DateTime? LastEventAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public DateTime? LastSentAtUtc { get; set; }
    public Guid? ClaimToken { get; set; }
    public DateTime? ClaimExpiresAtUtc { get; set; }
}
