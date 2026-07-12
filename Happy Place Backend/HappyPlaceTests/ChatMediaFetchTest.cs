using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatMediaFetchTest {
    // Tests - Missing Assets

    [Fact]
    public void UnknownAssetReturnsNotFound() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get("/api/chatMedia/" + Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Tests - Serving

    [Fact]
    public void ImageFetchServesDecryptedJpegWithPrivateImmutableCache() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        string url = UploadAndGetUrl(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(300, 200), "photo.jpg", "image/jpeg");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType.MediaType);
        byte[] servedBytes = response.ReadContentAsByteArray();
        Assert.Equal(0xFF, servedBytes[0]);
        Assert.Equal(0xD8, servedBytes[1]);
        string cacheControl = response.Headers.CacheControl.ToString();
        Assert.Contains("private", cacheControl);
        Assert.Contains("immutable", cacheControl);
        Assert.DoesNotContain("public", cacheControl);
    }

    [Fact]
    public void VoiceFetchServesOriginalBytes() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] voiceBytes = CreateMp4ContainerBytes(6000);
        string url = UploadAndGetUrl(testingMockProvidersContainer, ownerAuthToken, groupId, 4, 30, voiceBytes, "note.m4a", "audio/mp4");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("audio/mp4", response.Content.Headers.ContentType.MediaType);
        byte[] servedBytes = response.ReadContentAsByteArray();
        Assert.True(servedBytes.SequenceEqual(voiceBytes));
    }

    [Fact]
    public void VideoFetchServesFullFile() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] videoBytes = CreateMp4ContainerBytes(9000);
        string url = UploadAndGetUrl(testingMockProvidersContainer, ownerAuthToken, groupId, 3, 60, videoBytes, "clip.mp4", "video/mp4");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("video/mp4", response.Content.Headers.ContentType.MediaType);
        byte[] servedBytes = response.ReadContentAsByteArray();
        Assert.True(servedBytes.SequenceEqual(videoBytes));
    }

    [Fact]
    public void VideoFetchSupportsRangeRequests() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        byte[] videoBytes = CreateMp4ContainerBytes(9000);
        string url = UploadAndGetUrl(testingMockProvidersContainer, ownerAuthToken, groupId, 3, 60, videoBytes, "clip.mp4", "video/mp4");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.GetWithRange(url, 0, 3);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        byte[] servedBytes = response.ReadContentAsByteArray();
        Assert.Equal(4, servedBytes.Length);
        Assert.True(videoBytes.AsSpan(0, 4).SequenceEqual(servedBytes));
    }

    [Fact]
    public void UnattachedAssetIsFetchableByItsUploader() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        string url = UploadAndGetUrl(testingMockProvidersContainer, ownerAuthToken, groupId, 2, 0, TestImageGenerator.CreateJpeg(200, 200), "photo.jpg", "image/jpeg");

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.Get(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static string UploadAndGetUrl(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, int kind, int durationSeconds, byte[] mediaBytes, string fileName, string contentType) {
        JsonElement root = testingMockProvidersContainer.WebClient.UploadMultipart("api/chatMedia/upload", new Dictionary<string, string> { ["AuthToken"] = authToken, ["ChatGroupId"] = chatGroupId.ToString(), ["Kind"] = kind.ToString(), ["DurationSeconds"] = durationSeconds.ToString() }, ("Media", mediaBytes, fileName, contentType)).ReadContentAsJsonDocument().RootElement;
        return root.GetProperty("url").GetString();
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
}
