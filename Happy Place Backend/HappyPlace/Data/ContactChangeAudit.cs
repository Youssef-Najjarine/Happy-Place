using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ContactChangeAudit))]
public class ContactChangeAudit {
    // Properties
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public String EventType { get; set; }
    public String OldValue { get; set; }
    public String NewValue { get; set; }
    public DateTime EventAtUtc { get; set; }
}
