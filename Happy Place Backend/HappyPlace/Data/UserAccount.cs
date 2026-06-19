using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(UserAccount))]
public class UserAccount {
    // Properties
    public Guid Id { get; set; }
    public String Username { get; set; }
    public String HashedPassword { get; set; }
    public String DisplayName { get; set; }
    public String EmailAddress { get; set; }
    public String PhoneNumber { get; set; }
    public String Bio { get; set; }
    public String ProfilePhotoUrl { get; set; }
    public String BackgroundPhotoUrl { get; set; }
    public Boolean IsAnonymous { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
