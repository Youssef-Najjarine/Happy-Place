using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(UserProfilePhoto))]
public class UserProfilePhoto {
    // Properties
    public Guid Id { get; set; }
    public Guid UserAccountId { get; set; }
    public Byte PhotoType { get; set; }
    public Byte[] ImageBytes { get; set; }
    public String ContentType { get; set; }
    public Int64 FileSizeBytes { get; set; }
    public Int32 WidthPixels { get; set; }
    public Int32 HeightPixels { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
