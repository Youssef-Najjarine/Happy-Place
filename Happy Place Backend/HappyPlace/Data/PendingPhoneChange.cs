using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(PendingPhoneChange))]
public class PendingPhoneChange {
    // Properties
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public String NewPhoneNumber { get; set; }
    public String VerificationCode { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
