using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(Friendship))]
public class Friendship {
    // Properties
    public Guid Id { get; set; }
    public Guid RequesterUserAccountId { get; set; }
    public Guid AddresseeUserAccountId { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}
