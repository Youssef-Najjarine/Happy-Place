using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatGroupMember))]
public class ChatGroupMember {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatGroupId { get; set; }
    public Guid UserAccountId { get; set; }
    public ChatGroupMemberRole MemberRole { get; set; }
    public ChatGroupMemberStatus Status { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public Int64 LastReadSequence { get; set; }
    public DateTime? LastTypingAtUtc { get; set; }
}
