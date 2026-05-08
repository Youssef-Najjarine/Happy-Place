using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(PasswordResetRequest))]
public class PasswordResetRequest {
    // Properties
    public int Id { get; set; }
    public string EmailAddress { get; set; }
    public string PhoneNumber { get; set; }
    public string VerificationCode { get; set; }
    public string ResetToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? UsedAt { get; set; }
}
