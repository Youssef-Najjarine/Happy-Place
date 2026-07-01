using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(DeviceToken))]
public class DeviceToken {
    // Properties
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public string Token { get; set; }
    public string Platform { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
