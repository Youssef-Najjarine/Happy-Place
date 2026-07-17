using System.Globalization;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ChatMessageTimestampTest {
    // Tests - Serialized Zone Marker

    [Fact]
    public void SendResponseTimestampIsUtcWithExplicitZone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        string serializedTimestamp = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello").GetProperty("message").GetProperty("createdAtUtc").GetString();

        Assert.EndsWith("Z", serializedTimestamp);
        Assert.Equal(TimeSpan.Zero, ParseInstant(serializedTimestamp).Offset);
    }

    [Fact]
    public void ListPageTimestampIsUtcWithExplicitZone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");

        JsonElement listRoot = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();
        string serializedTimestamp = listRoot.GetProperty("items")[0].GetProperty("createdAtUtc").GetString();

        Assert.EndsWith("Z", serializedTimestamp);
        Assert.Equal(TimeSpan.Zero, ParseInstant(serializedTimestamp).Offset);
    }

    [Fact]
    public void PollTimestampIsUtcWithExplicitZone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");

        string serializedTimestamp = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("changes")[0].GetProperty("createdAtUtc").GetString();

        Assert.EndsWith("Z", serializedTimestamp);
        Assert.Equal(TimeSpan.Zero, ParseInstant(serializedTimestamp).Offset);
    }

    [Fact]
    public void DuplicateSendTimestampIsUtcWithExplicitZone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid clientMessageId = Guid.NewGuid();
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");

        JsonElement duplicateRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");
        string serializedTimestamp = duplicateRoot.GetProperty("message").GetProperty("createdAtUtc").GetString();

        Assert.Equal("duplicate", duplicateRoot.GetProperty("status").GetString());
        Assert.EndsWith("Z", serializedTimestamp);
        Assert.Equal(TimeSpan.Zero, ParseInstant(serializedTimestamp).Offset);
    }

    // Tests - Round Trip Instants

    [Fact]
    public void SeededHistoricalTimestampRoundTripsAsSameUtcInstant() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        DateTime seededCreatedAtUtc = new(2026, 1, 15, 20, 5, 0, DateTimeKind.Utc);
        SeedMessageAt(groupId, ownerUserAccountId, 1, seededCreatedAtUtc);

        string serializedTimestamp = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("changes")[0].GetProperty("createdAtUtc").GetString();

        Assert.EndsWith("Z", serializedTimestamp);
        Assert.Equal(new DateTimeOffset(seededCreatedAtUtc), ParseInstant(serializedTimestamp));
    }

    [Fact]
    public void FreshSendRoundTripsWithinSecondsOfServerClock() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        DateTimeOffset lowerBound = DateTimeOffset.UtcNow.AddSeconds(-2);

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");
        string serializedTimestamp = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("changes")[0].GetProperty("createdAtUtc").GetString();
        DateTimeOffset parsedInstant = ParseInstant(serializedTimestamp);

        Assert.Equal(TimeSpan.Zero, parsedInstant.Offset);
        Assert.InRange(parsedInstant, lowerBound, DateTimeOffset.UtcNow.AddSeconds(2));
    }

    [Fact]
    public void SendAndPollAgreeOnTheSameInstant() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        DateTimeOffset sendInstant = ParseInstant(Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello").GetProperty("message").GetProperty("createdAtUtc").GetString());
        DateTimeOffset polledInstant = ParseInstant(Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 0).GetProperty("changes")[0].GetProperty("createdAtUtc").GetString());

        Assert.True((polledInstant - sendInstant).Duration() < TimeSpan.FromSeconds(2));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static JsonElement Poll(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long sinceChangeSequence) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = authToken, ChatGroupId = chatGroupId, SinceChangeSequence = sinceChangeSequence }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static void SeedMessageAt(Guid groupId, Guid senderUserAccountId, long sequence, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatMessages.Add(new ChatMessage { Id = Guid.NewGuid(), ChatGroupId = groupId, SenderUserAccountId = senderUserAccountId, ClientMessageId = Guid.NewGuid(), Kind = ChatMessageKind.Text, BodyCipher = MessageCipher.Encrypt("seeded message"), CipherVersion = MessageCipher.CurrentVersion, Sequence = sequence, ChangeSequence = sequence, IsDeleted = false, CreatedAtUtc = createdAtUtc });
        dbContext.SaveChanges();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastMessageSequence, sequence).SetProperty(field => field.LastChangeSequence, sequence));
    }

    // Helpers - Reading

    private static DateTimeOffset ParseInstant(string serializedTimestamp) {
        return DateTimeOffset.Parse(serializedTimestamp, CultureInfo.InvariantCulture);
    }
}
