using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatGroup))]
public class ChatGroup {
    // Properties

    public Guid Id { get; set; }
    public String Name { get; set; }
    public Guid OwnerUserAccountId { get; set; }
    public Boolean IsPublic { get; set; }
    public ChatGroupStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
}
