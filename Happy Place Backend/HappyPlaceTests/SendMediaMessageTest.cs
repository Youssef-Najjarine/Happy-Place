using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SendMediaMessageTest {
    // Tests - Media Sends

    [Fact]
    public void ImageMessageSendsWithKindAndMediaFields() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        JsonElement message = root.GetProperty("message");
        Assert.Equal(2, message.GetProperty("kind").GetInt32());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("body").ValueKind);
        Assert.Equal("/api/chatMedia/" + mediaId, message.GetProperty("mediaUrl").GetString());
        Assert.Equal(2048, message.GetProperty("mediaWidth").GetInt32());
        Assert.Equal(1024, message.GetProperty("mediaHeight").GetInt32());
        Assert.Equal(Guid.Parse(message.GetProperty("id").GetString()), LoadAsset(mediaId).AttachedMessageId);
    }

    [Fact]
    public void VoiceMessageCarriesDuration() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadVoice(testingMockProvidersContainer, ownerAuthToken, groupId, 45);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        JsonElement message = root.GetProperty("message");
        Assert.Equal(4, message.GetProperty("kind").GetInt32());
        Assert.Equal(45, message.GetProperty("mediaDurationSeconds").GetInt32());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("mediaWidth").ValueKind);
    }

    [Fact]
    public void VideoMessageSendsWithKindThree() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadVideo(testingMockProvidersContainer, ownerAuthToken, groupId, 60);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(3, root.GetProperty("message").GetProperty("kind").GetInt32());
    }

    // Tests - Attachment Rules

    [Fact]
    public void UnknownMediaIdReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid());

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void SomeoneElsesUploadReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = SendMedia(testingMockProvidersContainer, memberAuthToken, groupId, mediaId);

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
        Assert.Null(LoadAsset(mediaId).AttachedMessageId);
    }

    [Fact]
    public void AssetFromAnotherGroupReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, secondGroupId);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, firstGroupId, mediaId);

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
    }

    [Fact]
    public void AlreadyAttachedAssetReturnsInvalidMedia() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);
        SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        JsonElement root = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        Assert.Equal("invalidMedia", root.GetProperty("status").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    [Fact]
    public void BodyAlongsideMediaReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), Body = "a caption", MediaId = mediaId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
        Assert.Null(LoadAsset(mediaId).AttachedMessageId);
    }

    [Fact]
    public void DuplicateClientMessageIdMediaSendCollapsesToOneAttachment() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);
        Guid clientMessageId = Guid.NewGuid();
        JsonElement firstRoot = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId, clientMessageId);

        JsonElement secondRoot = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId, clientMessageId);

        Assert.Equal("sent", firstRoot.GetProperty("status").GetString());
        Assert.Equal("duplicate", secondRoot.GetProperty("status").GetString());
        Assert.Equal(firstRoot.GetProperty("message").GetProperty("id").GetString(), secondRoot.GetProperty("message").GetProperty("id").GetString());
        Assert.Equal("/api/chatMedia/" + mediaId, secondRoot.GetProperty("message").GetProperty("mediaUrl").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    // Tests - Propagation

    [Fact]
    public void MediaMessageSurfacesInPollWithMediaFields() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid mediaId = UploadVoice(testingMockProvidersContainer, ownerAuthToken, groupId, 20);
        SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = memberAuthToken, ChatGroupId = groupId, SinceChangeSequence = 0 }).ReadContentAsJsonDocument().RootElement.Clone();

        JsonElement change = root.GetProperty("changes")[0];
        Assert.Equal(4, change.GetProperty("kind").GetInt32());
        Assert.Equal("/api/chatMedia/" + mediaId, change.GetProperty("mediaUrl").GetString());
        Assert.Equal(20, change.GetProperty("mediaDurationSeconds").GetInt32());
    }

    [Fact]
    public void DeleteOwnMediaMessageTombstonesMediaFields() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid mediaId = UploadImage(testingMockProvidersContainer, ownerAuthToken, groupId);
        JsonElement sendRoot = SendMedia(testingMockProvidersContainer, ownerAuthToken, groupId, mediaId);
        Guid messageId = Guid.Parse(sendRoot.GetProperty("message").GetProperty("id").GetString());

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MessageId = messageId }).EnsureSuccessStatusCode();
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        JsonElement item = root.GetProperty("items")[0];
        Assert.True(item.GetProperty("isDeleted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("mediaUrl").ValueKind);
        Assert.True(AssetExists(mediaId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static Guid UploadImage(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return UploadMedia(testingMockProvidersContainer, authToken, chatGroupId, 2, 0, TestImageGenerator.CreatePng(3000, 1500), "photo.png", "image/png");
    }

    private static Guid UploadVoice(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int durationSeconds) {
        return UploadMedia(testingMockProvidersContainer, authToken, chatGroupId, 4, durationSeconds, CreateMp4ContainerBytes(4000), "note.m4a", "audio/mp4");
    }

    private static Guid UploadVideo(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int durationSeconds) {
        return UploadMedia(testingMockProvidersContainer, authToken, chatGroupId, 3, durationSeconds, CreateMp4ContainerBytes(8000), "clip.mp4", "video/mp4");
    }

    private static Guid UploadMedia(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int kind, int durationSeconds, byte[] mediaBytes, string fileName, string contentType) {
        JsonElement root = testingMockProvidersContainer.WebClient.UploadMultipart("api/chatMedia/upload", new Dictionary<string, string> { ["AuthToken"] = authToken, ["ChatGroupId"] = chatGroupId.ToString(), ["Kind"] = kind.ToString(), ["DurationSeconds"] = durationSeconds.ToString() }, ("Media", mediaBytes, fileName, contentType)).ReadContentAsJsonDocument().RootElement;
        return Guid.Parse(root.GetProperty("mediaId").GetString());
    }

    private static JsonElement SendMedia(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid mediaId) {
        return SendMedia(testingMockProvidersContainer, authToken, chatGroupId, mediaId, Guid.NewGuid());
    }

    private static JsonElement SendMedia(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid mediaId, Guid clientMessageId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, MediaId = mediaId }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
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

    private static int CountMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == groupId);
    }
}
