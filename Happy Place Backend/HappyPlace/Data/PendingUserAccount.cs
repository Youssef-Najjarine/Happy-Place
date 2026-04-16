using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(PendingUserAccount))]
public class PendingUserAccount {
    // Properties
    public Guid Id { get; set; }
    public String EmailAddress { get; set; }
    public String PhoneNumber { get; set; }
    public String DisplayName { get; set; }
    public String Username { get; set; }
    public String HashedPassword { get; set; }
    public String VerificationCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}