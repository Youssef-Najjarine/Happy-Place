using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(UserBlock))]
public class UserBlock {
    // Properties
    public Guid Id { get; set; }
    public Guid BlockerUserAccountId { get; set; }
    public Guid BlockedUserAccountId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
