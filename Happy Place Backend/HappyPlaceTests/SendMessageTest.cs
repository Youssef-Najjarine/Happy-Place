using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class SendMessageTest {
    // Tests - Authentication Failures

    [Fact]
    public void SendEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), ClientMessageId = Guid.NewGuid(), Body = "hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void SendInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), ClientMessageId = Guid.NewGuid(), Body = "hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void SendMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { ChatGroupId = Guid.NewGuid(), ClientMessageId = Guid.NewGuid(), Body = "hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Membership Gates

    [Fact]
    public void StrangerCannotSendReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, strangerAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void PendingMemberCannotSendReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string requesterAuthToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "Private Group", false);
        AddPendingMember(groupId, ResolveUserAccountId(requesterAuthToken));

        JsonElement root = Send(testingMockProvidersContainer, requesterAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void LeftMemberCannotSendReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/leave", new { AuthToken = memberAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = Send(testingMockProvidersContainer, memberAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void RemovedMemberCannotSendReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        RemoveMembershipRow(groupId, memberUserAccountId);

        JsonElement root = Send(testingMockProvidersContainer, memberAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
    }

    [Fact]
    public void GuestActiveMemberCanSend() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(SeedUser("Owner", null), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));

        JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "hello from a guest");

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    // Tests - Group State Gates

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = Send(testingMockProvidersContainer, memberAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void ProvisionalGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateProvisionalGroup(ResolveUserAccountId(ownerAuthToken), "Waiting For Help", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = Send(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), Guid.NewGuid(), "hello");

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Body Validation

    [Fact]
    public void EmptyBodyReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "");

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void WhitespaceOnlyBodyReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "   \t  ");

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
    }

    [Fact]
    public void MissingClientMessageIdReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, Body = "hello" }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
    }

    [Fact]
    public void BodyAtCapIsAccepted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), new string('a', 4096));

        Assert.Equal("sent", root.GetProperty("status").GetString());
    }

    [Fact]
    public void BodyOverCapReturnsInvalidBody() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), new string('a', 4097));

        Assert.Equal("invalidBody", root.GetProperty("status").GetString());
        Assert.Equal(0, CountMessages(groupId));
    }

    [Fact]
    public void BodyIsTrimmedBeforeStorage() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "   hello there   ");

        Assert.Equal("hello there", root.GetProperty("message").GetProperty("body").GetString());
        Assert.Equal("hello there", MessageCipher.Decrypt(LoadSingleMessage(groupId).BodyCipher));
    }

    [Fact]
    public void EmojiUnicodeAndRtlBodyRoundTrips() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        string body = "caf\u00e9 \u4f60\u597d \u0645\u0631\u062d\u0628\u0627 e\u0301 \ud83d\ude0a\ud83c\udf89";

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), body);

        Assert.Equal(body, root.GetProperty("message").GetProperty("body").GetString());
        Assert.Equal(body, MessageCipher.Decrypt(LoadSingleMessage(groupId).BodyCipher));
    }

    // Tests - Idempotency

    [Fact]
    public void DuplicateClientMessageIdReturnsSameMessageAndStoresOneRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid clientMessageId = Guid.NewGuid();
        JsonElement firstRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");

        JsonElement secondRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");

        Assert.Equal("sent", firstRoot.GetProperty("status").GetString());
        Assert.Equal("duplicate", secondRoot.GetProperty("status").GetString());
        Assert.Equal(firstRoot.GetProperty("message").GetProperty("id").GetString(), secondRoot.GetProperty("message").GetProperty("id").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    [Fact]
    public void DuplicateClientMessageIdWithDifferentBodyReturnsOriginal() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid clientMessageId = Guid.NewGuid();
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "original body");

        JsonElement secondRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "different body");

        Assert.Equal("duplicate", secondRoot.GetProperty("status").GetString());
        Assert.Equal("original body", secondRoot.GetProperty("message").GetProperty("body").GetString());
        Assert.Equal(1, CountMessages(groupId));
    }

    [Fact]
    public void ConcurrentDuplicateClientMessageIdSendsCollapseToOneRow() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
            Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
            Guid clientMessageId = Guid.NewGuid();

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [
                new Thread(() => { try { Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello"); } catch (Exception error) { errors.Add(error); } }),
                new Thread(() => { try { Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello"); } catch (Exception error) { errors.Add(error); } })
            ];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.Equal(1, CountMessages(groupId));
        }
    }

    // Tests - Sequencing

    [Fact]
    public void SequenceIncrementsPerGroupIndependently() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);

        long firstGroupFirstSequence = Send(testingMockProvidersContainer, ownerAuthToken, firstGroupId, Guid.NewGuid(), "one").GetProperty("message").GetProperty("sequence").GetInt64();
        long secondGroupFirstSequence = Send(testingMockProvidersContainer, ownerAuthToken, secondGroupId, Guid.NewGuid(), "one").GetProperty("message").GetProperty("sequence").GetInt64();
        long firstGroupSecondSequence = Send(testingMockProvidersContainer, ownerAuthToken, firstGroupId, Guid.NewGuid(), "two").GetProperty("message").GetProperty("sequence").GetInt64();

        Assert.Equal(1, firstGroupFirstSequence);
        Assert.Equal(1, secondGroupFirstSequence);
        Assert.Equal(2, firstGroupSecondSequence);
    }

    [Fact]
    public void ConcurrentSendsProduceGapFreeSequences() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string firstAuthToken = CreateUser(testingMockProvidersContainer, "First Sender");
        string secondAuthToken = CreateUser(testingMockProvidersContainer, "Second Sender");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(firstAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(secondAuthToken));

        ConcurrentBag<Exception> errors = [];
        List<Thread> threads = [
            new Thread(() => { try { for (int send = 0; send < 5; send++) Send(testingMockProvidersContainer, firstAuthToken, groupId, Guid.NewGuid(), "from first " + send); } catch (Exception error) { errors.Add(error); } }),
            new Thread(() => { try { for (int send = 0; send < 5; send++) Send(testingMockProvidersContainer, secondAuthToken, groupId, Guid.NewGuid(), "from second " + send); } catch (Exception error) { errors.Add(error); } })
        ];
        foreach (Thread thread in threads)
            thread.Start();
        foreach (Thread thread in threads)
            thread.Join();

        Assert.Empty(errors);
        List<long> sequences = [.. LoadMessages(groupId).Select(message => message.Sequence).OrderBy(sequence => sequence)];
        Assert.Equal([.. Enumerable.Range(1, 10).Select(value => (long)value)], sequences);
    }

    [Fact]
    public void SendUpdatesGroupLastSeenAtUtc() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        DateTime staleLastSeenAtUtc = DateTime.UtcNow.AddMinutes(-10);
        SetGroupLastSeenAtUtc(groupId, staleLastSeenAtUtc);

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");

        Assert.True(GetGroupLastSeenAtUtc(groupId) > staleLastSeenAtUtc);
    }

    // Tests - Encryption At Rest

    [Fact]
    public void StoredBodyIsCiphertextNotPlaintext() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        string body = "a private support conversation " + Guid.NewGuid();

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), body);

        byte[] storedBodyCipher = LoadSingleMessage(groupId).BodyCipher;
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(body);
        Assert.NotNull(storedBodyCipher);
        Assert.False(storedBodyCipher.SequenceEqual(plaintextBytes));
        Assert.False(ContainsSubsequence(storedBodyCipher, plaintextBytes));
    }

    [Fact]
    public void SentMessageRowPersistsExpectedColumns() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid clientMessageId = Guid.NewGuid();

        Send(testingMockProvidersContainer, ownerAuthToken, groupId, clientMessageId, "hello");

        ChatMessage message = LoadSingleMessage(groupId);
        Assert.Equal(ownerUserAccountId, message.SenderUserAccountId);
        Assert.Equal(clientMessageId, message.ClientMessageId);
        Assert.Equal(ChatMessageKind.Text, message.Kind);
        Assert.False(message.IsDeleted);
        Assert.Equal(MessageCipher.CurrentVersion, message.CipherVersion);
        Assert.Equal(message.Sequence, message.ChangeSequence);
    }

    // Tests - Response Shape

    [Fact]
    public void SendResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello");
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["message", "status"];
        List<string> actualMessageProperties = [.. root.GetProperty("message").EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedMessageProperties = ["body", "createdAtUtc", "id", "isDeleted", "kind", "mediaDurationSeconds", "mediaHeight", "mediaUrl", "mediaWidth", "reactions", "senderUserAccountId", "sequence"];

        Assert.Equal(expectedProperties, actualProperties);
        Assert.Equal(expectedMessageProperties, actualMessageProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
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
        return CreateGroup(ownerUserAccountId, name, isPublic, ChatGroupStatus.Active);
    }

    private static Guid CreateProvisionalGroup(Guid ownerUserAccountId, string name, bool isPublic) {
        return CreateGroup(ownerUserAccountId, name, isPublic, ChatGroupStatus.Provisional);
    }

    private static Guid CreateGroup(Guid ownerUserAccountId, string name, bool isPublic, ChatGroupStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = status, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Active);
    }

    private static void AddPendingMember(Guid groupId, Guid userAccountId) {
        AddMember(groupId, userAccountId, ChatGroupMemberStatus.Pending);
    }

    private static void AddMember(Guid groupId, Guid userAccountId, ChatGroupMemberStatus status) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = status, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void RemoveMembershipRow(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId).ExecuteDelete();
    }

    private static void SetGroupLastSeenAtUtc(Guid groupId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteUpdate(setters => setters.SetProperty(field => field.LastSeenAtUtc, lastSeenAtUtc));
    }

    // Helpers - Reading

    private static int CountMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == groupId);
    }

    private static ChatMessage LoadSingleMessage(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Single(field => field.ChatGroupId == groupId);
    }

    private static List<ChatMessage> LoadMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return [.. dbContext.ChatMessages.Where(field => field.ChatGroupId == groupId)];
    }

    private static DateTime GetGroupLastSeenAtUtc(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).LastSeenAtUtc;
    }

    private static bool ContainsSubsequence(byte[] haystack, byte[] needle) {
        for (int start = 0; start + needle.Length <= haystack.Length; start++) {
            bool matches = true;
            for (int offset = 0; offset < needle.Length; offset++)
                if (haystack[start + offset] != needle[offset]) { matches = false; break; }
            if (matches)
                return true;
        }
        return false;
    }
}
