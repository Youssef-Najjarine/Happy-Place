using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatMessageReport))]
public class ChatMessageReport {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatMessageId { get; set; }
    public Guid ReporterUserAccountId { get; set; }
    public Guid? ReportedUserAccountId { get; set; }
    public ChatMessageKind Kind { get; set; }
    public Byte[] BodySnapshotCipher { get; set; }
    public Byte[] ReasonCipher { get; set; }
    public ChatMessageReportStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
