using System.ComponentModel.DataAnnotations.Schema;

namespace HappyWorld.HappyPlace.Data;

[Table(nameof(ChatMediaAsset))]
public class ChatMediaAsset {
    // Properties
    public Guid Id { get; set; }
    public Guid ChatGroupId { get; set; }
    public Guid? UploaderUserAccountId { get; set; }
    public Guid? AttachedMessageId { get; set; }
    public ChatMessageKind Kind { get; set; }
    public ChatMediaStorageMode StorageMode { get; set; }
    public Byte[] ContentBytes { get; set; }
    public String FilePath { get; set; }
    public String ContentType { get; set; }
    public Int64 ByteCount { get; set; }
    public Int32? Width { get; set; }
    public Int32? Height { get; set; }
    public Int32? DurationSeconds { get; set; }
    public Byte CipherVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
