using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(FriendRequestAudit))]
public class FriendRequestAudit {
    // Properties
    public Guid Id { get; set; }
    public Guid RequesterUserAccountId { get; set; }
    public Guid AddresseeUserAccountId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
