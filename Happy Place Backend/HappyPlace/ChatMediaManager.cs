using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HappyWorld.HappyPlace;

public static class ChatMediaManager {
    // Fields

    private static readonly long MaxImageUploadBytes = 26_214_400;
    private static readonly long MaxVoiceUploadBytes = 10_485_760;
    private static readonly long MaxVideoUploadBytes = 104_857_600;
    private static readonly int MaxVoiceDurationSeconds = 300;
    private static readonly int MaxVideoDurationSeconds = 180;
    private static readonly int ChatImageMaxDimensionPixels = 2048;
    private static readonly int JpegQuality = 85;
    private static readonly int OrphanSweepHours = 24;

    // Methods

    public static ChatMediaUploadResult Upload(string authToken, Guid chatGroupId, byte kind, int durationSeconds, byte[] mediaBytes) {
        Guid? uploaderUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (uploaderUserAccountId == null)
            return ChatMediaUploadResult.NotMember();
        if (kind != (byte)ChatMessageKind.Image && kind != (byte)ChatMessageKind.Video && kind != (byte)ChatMessageKind.Voice)
            return ChatMediaUploadResult.InvalidKind();
        if (mediaBytes == null || mediaBytes.Length == 0)
            return ChatMediaUploadResult.InvalidMedia();
        if (IsOverUploadCap(kind, mediaBytes.LongLength))
            return ChatMediaUploadResult.TooLarge();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMediaUploadResult.GroupGone();
        bool uploaderIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == uploaderUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!uploaderIsActiveMember)
            return ChatMediaUploadResult.NotMember();
        SweepOrphanedMediaAssets(dbContext);
        if (kind == (byte)ChatMessageKind.Image)
            return UploadImage(dbContext, chatGroupId, uploaderUserAccountId.Value, mediaBytes);
        if (kind == (byte)ChatMessageKind.Voice)
            return UploadVoice(dbContext, chatGroupId, uploaderUserAccountId.Value, durationSeconds, mediaBytes);
        return UploadVideo(dbContext, chatGroupId, uploaderUserAccountId.Value, durationSeconds, mediaBytes);
    }

    public static ChatMediaContent Fetch(Guid assetId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatMediaAsset asset = dbContext.ChatMediaAssets.SingleOrDefault(field => field.Id == assetId);
        if (asset == null)
            return null;
        if (asset.StorageMode == ChatMediaStorageMode.Database)
            return new ChatMediaContent(asset.ContentType, MessageCipher.DecryptBytes(asset.ContentBytes), null);
        if (asset.CipherVersion == 0) {
            if (!ChatMediaStorage.FileExists(asset.FilePath))
                return null;
            return new ChatMediaContent(asset.ContentType, null, ChatMediaStorage.FullPath(asset.FilePath));
        }
        byte[] envelope = ChatMediaStorage.ReadFile(asset.FilePath);
        if (envelope == null)
            return null;
        return new ChatMediaContent(asset.ContentType, MessageCipher.DecryptBytes(envelope), null);
    }

    public static string BuildMediaUrl(Guid assetId) {
        return "/api/chatMedia/" + assetId;
    }

    // Helpers - Upload

    private static ChatMediaUploadResult UploadImage(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid uploaderUserAccountId, byte[] mediaBytes) {
        if (!HasAcceptedImageMagicBytes(mediaBytes))
            return ChatMediaUploadResult.InvalidMedia();
        byte[] jpegBytes;
        int width;
        int height;
        try {
            (jpegBytes, width, height) = ProcessChatImage(mediaBytes);
        }
        catch (Exception) {
            return ChatMediaUploadResult.InvalidMedia();
        }
        ChatMediaAsset asset = new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UploaderUserAccountId = uploaderUserAccountId, AttachedMessageId = null, Kind = ChatMessageKind.Image, StorageMode = ChatMediaStorageMode.Database, ContentBytes = MessageCipher.EncryptBytes(jpegBytes), FilePath = null, ContentType = "image/jpeg", ByteCount = jpegBytes.LongLength, Width = width, Height = height, DurationSeconds = null, CipherVersion = MessageCipher.CurrentVersion, CreatedAtUtc = DateTime.UtcNow };
        dbContext.ChatMediaAssets.Add(asset);
        dbContext.SaveChanges();
        return ChatMediaUploadResult.Uploaded(asset.Id, BuildMediaUrl(asset.Id), width, height, null);
    }

    private static ChatMediaUploadResult UploadVoice(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid uploaderUserAccountId, int durationSeconds, byte[] mediaBytes) {
        if (!HasMp4ContainerMagicBytes(mediaBytes))
            return ChatMediaUploadResult.InvalidMedia();
        if (durationSeconds < 1 || durationSeconds > MaxVoiceDurationSeconds)
            return ChatMediaUploadResult.InvalidDuration();
        Guid assetId = Guid.NewGuid();
        string fileName = ChatMediaStorage.SaveFile(assetId, MessageCipher.EncryptBytes(mediaBytes));
        ChatMediaAsset asset = new() { Id = assetId, ChatGroupId = chatGroupId, UploaderUserAccountId = uploaderUserAccountId, AttachedMessageId = null, Kind = ChatMessageKind.Voice, StorageMode = ChatMediaStorageMode.FileSystem, ContentBytes = null, FilePath = fileName, ContentType = "audio/mp4", ByteCount = mediaBytes.LongLength, Width = null, Height = null, DurationSeconds = durationSeconds, CipherVersion = MessageCipher.CurrentVersion, CreatedAtUtc = DateTime.UtcNow };
        dbContext.ChatMediaAssets.Add(asset);
        dbContext.SaveChanges();
        return ChatMediaUploadResult.Uploaded(asset.Id, BuildMediaUrl(asset.Id), null, null, durationSeconds);
    }

    private static ChatMediaUploadResult UploadVideo(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid uploaderUserAccountId, int durationSeconds, byte[] mediaBytes) {
        if (!HasMp4ContainerMagicBytes(mediaBytes))
            return ChatMediaUploadResult.InvalidMedia();
        if (durationSeconds < 1 || durationSeconds > MaxVideoDurationSeconds)
            return ChatMediaUploadResult.InvalidDuration();
        Guid assetId = Guid.NewGuid();
        string fileName = ChatMediaStorage.SaveFile(assetId, mediaBytes);
        ChatMediaAsset asset = new() { Id = assetId, ChatGroupId = chatGroupId, UploaderUserAccountId = uploaderUserAccountId, AttachedMessageId = null, Kind = ChatMessageKind.Video, StorageMode = ChatMediaStorageMode.FileSystem, ContentBytes = null, FilePath = fileName, ContentType = "video/mp4", ByteCount = mediaBytes.LongLength, Width = null, Height = null, DurationSeconds = durationSeconds, CipherVersion = 0, CreatedAtUtc = DateTime.UtcNow };
        dbContext.ChatMediaAssets.Add(asset);
        dbContext.SaveChanges();
        return ChatMediaUploadResult.Uploaded(asset.Id, BuildMediaUrl(asset.Id), null, null, durationSeconds);
    }

    private static bool IsOverUploadCap(byte kind, long byteCount) {
        if (kind == (byte)ChatMessageKind.Image)
            return byteCount > MaxImageUploadBytes;
        if (kind == (byte)ChatMessageKind.Voice)
            return byteCount > MaxVoiceUploadBytes;
        return byteCount > MaxVideoUploadBytes;
    }

    private static void SweepOrphanedMediaAssets(HappyPlaceDbContext dbContext) {
        DateTime orphanCutoffUtc = DateTime.UtcNow.AddHours(-OrphanSweepHours);
        List<ChatMediaAsset> orphans = [.. dbContext.ChatMediaAssets
            .Where(field => field.AttachedMessageId == null && field.CreatedAtUtc < orphanCutoffUtc)];
        if (orphans.Count == 0)
            return;
        List<Guid> orphanIds = [.. orphans.Select(field => field.Id)];
        dbContext.ChatMediaAssets.Where(field => orphanIds.Contains(field.Id)).ExecuteDelete();
        foreach (ChatMediaAsset orphan in orphans)
            if (orphan.StorageMode == ChatMediaStorageMode.FileSystem)
                ChatMediaStorage.DeleteFile(orphan.FilePath);
    }

    // Helpers - Content Validation

    private static bool HasAcceptedImageMagicBytes(byte[] bytes) {
        if (bytes == null || bytes.Length < 12)
            return false;
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return true;
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return true;
        return false;
    }

    private static bool HasMp4ContainerMagicBytes(byte[] bytes) {
        if (bytes == null || bytes.Length < 12)
            return false;
        return bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70;
    }

    private static (byte[] JpegBytes, int Width, int Height) ProcessChatImage(byte[] originalBytes) {
        using var image = Image.Load<Rgba32>(originalBytes);
        image.Metadata.ExifProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IccProfile = null;
        image.Mutate(x => x.BackgroundColor(Color.White));
        if (image.Width > ChatImageMaxDimensionPixels || image.Height > ChatImageMaxDimensionPixels)
            image.Mutate(x => x.Resize(new ResizeOptions {
                Size = new Size(ChatImageMaxDimensionPixels, ChatImageMaxDimensionPixels),
                Mode = ResizeMode.Max
            }));
        using var outputStream = new MemoryStream();
        image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = JpegQuality });
        return (outputStream.ToArray(), image.Width, image.Height);
    }
}
