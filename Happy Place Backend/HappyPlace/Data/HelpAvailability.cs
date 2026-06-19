using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(HelpAvailability))]
public class HelpAvailability {
    // Properties
    public Guid Id { get; set; }
    public Guid HelperUserAccountId { get; set; }
    public Boolean IsAvailable { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
