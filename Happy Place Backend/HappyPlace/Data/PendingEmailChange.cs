using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(PendingEmailChange))]
public class PendingEmailChange {
    // Properties
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public String NewEmailAddress { get; set; }
    public String VerificationCode { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
