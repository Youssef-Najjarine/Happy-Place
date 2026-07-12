using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatMediaUploadTest {
    // Tests - Authentication Failures

    [Fact]
    public void UploadEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = Upload(testingMockProvidersContainer, "", Guid.NewGuid(), 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void UploadInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = Upload(testingMockProvidersContainer, "not-a-real-token-at-all", Guid.NewGuid(), 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, strangerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(0, CountAssets(groupId));
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void TextKindReturnsInvalidKind() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 1, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.Equal("invalidKind", root.GetProperty("status").GetString());
    }

    // Tests - Images

    [Fact]
    public void ImageUploadProcessesResizesAndEncrypts() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] pngBytes = TestImageGenerator.CreatePng(3000, 1500);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, pngBytes, "photo.png", "image/png");

        Assert.Equal("uploaded", root.GetProperty("status").GetString());
        Assert.Equal(2048, root.GetProperty("width").GetInt32());
        Assert.Equal(1024, root.GetProperty("height").GetInt32());
        ChatMediaAsset asset = LoadAsset(Guid.Parse(root.GetProperty("mediaId").GetString()));
        Assert.Equal(ChatMediaStorageMode.Database, asset.StorageMode);
        Assert.Equal("image/jpeg", asset.ContentType);
        Assert.Equal(MessageCipher.CurrentVersion, asset.CipherVersion);
        byte[] storedJpeg = MessageCipher.DecryptBytes(asset.ContentBytes);
        Assert.Equal(0xFF, storedJpeg[0]);
        Assert.Equal(0xD8, storedJpeg[1]);
        Assert.Equal("/api/chatMedia/" + asset.Id, root.GetProperty("url").GetString());
    }

    [Fact]
    public void SmallImageIsNotUpscaled() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(400, 300), "photo.jpg", "image/jpeg");

        Assert.Equal(400, root.GetProperty("width").GetInt32());
        Assert.Equal(300, root.GetProperty("height").GetInt32());
    }

    [Fact]
    public void NonImageBytesWithImageKindReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes("this is definitely not an image at all");

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, textBytes, "notes.txt", "image/jpeg");

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
        Assert.Equal(0, CountAssets(groupId));
    }

    [Fact]
    public void ImageOverCapReturnsTooLarge() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] oversizeBytes = new byte[26_214_401];
        oversizeBytes[0] = 0xFF;
        oversizeBytes[1] = 0xD8;
        oversizeBytes[2] = 0xFF;

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, oversizeBytes, "big.jpg", "image/jpeg");

        Assert.Equal("tooLarge", root.GetProperty("status").GetString());
        Assert.Equal(0, CountAssets(groupId));
    }

    // Tests - Voice

    [Fact]
    public void VoiceUploadEncryptsOnDisk() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] voiceBytes = CreateMp4ContainerBytes(5000);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 30, voiceBytes, "note.m4a", "audio/mp4");

        Assert.Equal("uploaded", root.GetProperty("status").GetString());
        Assert.Equal(30, root.GetProperty("durationSeconds").GetInt32());
        ChatMediaAsset asset = LoadAsset(Guid.Parse(root.GetProperty("mediaId").GetString()));
        Assert.Equal(ChatMediaStorageMode.FileSystem, asset.StorageMode);
        Assert.Equal("audio/mp4", asset.ContentType);
        Assert.Equal(MessageCipher.CurrentVersion, asset.CipherVersion);
        byte[] fileBytes = ChatMediaStorage.ReadFile(asset.FilePath);
        Assert.False(fileBytes.SequenceEqual(voiceBytes));
        Assert.True(MessageCipher.DecryptBytes(fileBytes).SequenceEqual(voiceBytes));
    }

    [Fact]
    public void VoiceDurationOverCapReturnsInvalidDuration() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 301, CreateMp4ContainerBytes(1000), "note.m4a", "audio/mp4");

        Assert.Equal("invalidDuration", root.GetProperty("status").GetString());
        Assert.Equal(0, CountAssets(groupId));
    }

    [Fact]
    public void VoiceMissingDurationReturnsInvalidDuration() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 0, CreateMp4ContainerBytes(1000), "note.m4a", "audio/mp4");

        Assert.Equal("invalidDuration", root.GetProperty("status").GetString());
    }

    [Fact]
    public void VoiceOverCapReturnsTooLarge() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 30, CreateMp4ContainerBytes(10_485_761 - 8), "note.m4a", "audio/mp4");

        Assert.Equal("tooLarge", root.GetProperty("status").GetString());
    }

    [Fact]
    public void VoiceBadMagicBytesReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] junkBytes = new byte[1000];

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 30, junkBytes, "note.m4a", "audio/mp4");

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
    }

    // Tests - Video

    [Fact]
    public void VideoUploadStoresPlainStreamableFile() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] videoBytes = CreateMp4ContainerBytes(20000);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 3, 60, videoBytes, "clip.mp4", "video/mp4");

        Assert.Equal("uploaded", root.GetProperty("status").GetString());
        ChatMediaAsset asset = LoadAsset(Guid.Parse(root.GetProperty("mediaId").GetString()));
        Assert.Equal(ChatMediaStorageMode.FileSystem, asset.StorageMode);
        Assert.Equal("video/mp4", asset.ContentType);
        Assert.Equal(0, asset.CipherVersion);
        Assert.Equal(60, asset.DurationSeconds);
        byte[] fileBytes = ChatMediaStorage.ReadFile(asset.FilePath);
        Assert.True(fileBytes.SequenceEqual(videoBytes));
    }

    [Fact]
    public void VideoDurationOverCapReturnsInvalidDuration() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 3, 181, CreateMp4ContainerBytes(1000), "clip.mp4", "video/mp4");

        Assert.Equal("invalidDuration", root.GetProperty("status").GetString());
    }

    // Tests - Orphan Sweep

    [Fact]
    public void OrphanSweepRemovesStaleUnattachedAssetsAndFiles() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid staleAssetId = SeedFileAsset(groupId, ownerUserAccountId, DateTime.UtcNow.AddHours(-30));
        Guid freshAssetId = SeedFileAsset(groupId, ownerUserAccountId, DateTime.UtcNow.AddHours(-1));
        string staleFileName = LoadAsset(staleAssetId).FilePath;

        UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");

        Assert.False(AssetExists(staleAssetId));
        Assert.False(ChatMediaStorage.FileExists(staleFileName));
        Assert.True(AssetExists(freshAssetId));
    }

    // Tests - Response Shape

    [Fact]
    public void UploadResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = UploadJson(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(100, 100), "photo.jpg", "image/jpeg");
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["durationSeconds", "height", "mediaId", "status", "url", "width"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static HttpResponseMessage Upload(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int kind, int durationSeconds, byte[] mediaBytes, string fileName, string contentType) {
        return testingMockProvidersContainer.WebClient.UploadMultipart("api/chatMedia/upload", new Dictionary<string, string> { ["AuthToken"] = authToken, ["ChatGroupId"] = chatGroupId.ToString(), ["Kind"] = kind.ToString(), ["DurationSeconds"] = durationSeconds.ToString() }, ("Media", mediaBytes, fileName, contentType));
    }

    private static JsonElement UploadJson(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int kind, int durationSeconds, byte[] mediaBytes, string fileName, string contentType) {
        return Upload(testingMockProvidersContainer, authToken, chatGroupId, kind, durationSeconds, mediaBytes, fileName, contentType).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static byte[] CreateMp4ContainerBytes(int payloadLength) {
        byte[] bytes = new byte[8 + payloadLength];
        bytes[3] = 0x18;
        bytes[4] = 0x66;
        bytes[5] = 0x74;
        bytes[6] = 0x79;
        bytes[7] = 0x70;
        for (int index = 8; index < bytes.Length; index++)
            bytes[index] = (byte)(index % 251);
        return bytes;
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static Guid SeedUser(string displayName, string profilePhotoUrl) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid userAccountId = Guid.NewGuid();
        dbContext.UserAccounts.Add(new UserAccount { Id = userAccountId, DisplayName = displayName, IsAnonymous = false, CreatedAtUtc = DateTime.UtcNow, ProfilePhotoUrl = profilePhotoUrl });
        dbContext.SaveChanges();
        return userAccountId;
    }

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static Guid SeedFileAsset(Guid groupId, Guid uploaderUserAccountId, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid assetId = Guid.NewGuid();
        string fileName = ChatMediaStorage.SaveFile(assetId, CreateMp4ContainerBytes(100));
        dbContext.ChatMediaAssets.Add(new ChatMediaAsset { Id = assetId, ChatGroupId = groupId, UploaderUserAccountId = uploaderUserAccountId, AttachedMessageId = null, Kind = ChatMessageKind.Voice, StorageMode = ChatMediaStorageMode.FileSystem, ContentBytes = null, FilePath = fileName, ContentType = "audio/mp4", ByteCount = 108, Width = null, Height = null, DurationSeconds = 10, CipherVersion = 1, CreatedAtUtc = createdAtUtc });
        dbContext.SaveChanges();
        return assetId;
    }

    // Helpers - Reading

    private static ChatMediaAsset LoadAsset(Guid assetId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMediaAssets.Single(field => field.Id == assetId);
    }

    private static bool AssetExists(Guid assetId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMediaAssets.Any(field => field.Id == assetId);
    }

    private static int CountAssets(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMediaAssets.Count(field => field.ChatGroupId == groupId);
    }
}
