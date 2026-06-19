using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(HelpOffer))]
public class HelpOffer {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatGroupId { get; set; }
    public Guid HelperUserAccountId { get; set; }
    public HelpOfferStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
