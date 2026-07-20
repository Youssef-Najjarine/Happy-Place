using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Email;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class GuestMessageCapTest {
    // Tests - Counting

    [Fact]
    public void GuestSendsIncrementTheCounter() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);

        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "one");
        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "two");
        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "three");

        Assert.Equal(3, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void MediaSendsCountTowardTheCap() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        Guid mediaId = SeedImageAsset(groupId, guestUserAccountId);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = guestAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), MediaId = mediaId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(ChatMessageManager.GuestMessageCap - 1, root.GetProperty("guestMessagesRemaining").GetInt32());
        Assert.Equal(1, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void VerifiedSendsNeverTouchTheCounterAndCarryNullRemaining() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);

        JsonElement firstRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "one");
        JsonElement secondRoot = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "two");

        Assert.Equal(JsonValueKind.Null, firstRoot.GetProperty("guestMessagesRemaining").ValueKind);
        Assert.Equal(JsonValueKind.Null, secondRoot.GetProperty("guestMessagesRemaining").ValueKind);
        Assert.Equal(0, GetGuestMessageCount(ownerUserAccountId));
    }

    [Fact]
    public void ReactionsDoNotCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        Guid ownerMessageId = Guid.Parse(Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "react to me").GetProperty("message").GetProperty("id").GetString());
        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "my only send");

        for (int reactionNumber = 0; reactionNumber < 5; reactionNumber++)
            React(testingMockProvidersContainer, guestAuthToken, groupId, ownerMessageId, HeartEmoji);

        Assert.Equal(1, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void TypingAndMarkReadDoNotCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "hello guest");

        Typing(testingMockProvidersContainer, guestAuthToken, groupId);
        MarkRead(testingMockProvidersContainer, guestAuthToken, groupId, 1);

        Assert.Equal(0, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void DeletingOwnMessagesDoesNotRefundTheCounter() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        List<Guid> sentMessageIds = SendCapMessages(testingMockProvidersContainer, guestAuthToken, groupId);

        foreach (Guid sentMessageId in sentMessageIds)
            DeleteOwn(testingMockProvidersContainer, guestAuthToken, groupId, sentMessageId);
        JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "one more after deleting everything");

        Assert.Equal("guestLimitReached", root.GetProperty("status").GetString());
        Assert.Equal(ChatMessageManager.GuestMessageCap, GetGuestMessageCount(guestUserAccountId));
    }

    // Tests - The Wall

    [Fact]
    public void GuestCanSendExactlyTheCapWithRemainingCountingDown() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));

        for (int sendNumber = 1; sendNumber <= ChatMessageManager.GuestMessageCap; sendNumber++) {
            JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "message " + sendNumber);
            Assert.Equal("sent", root.GetProperty("status").GetString());
            Assert.Equal(ChatMessageManager.GuestMessageCap - sendNumber, root.GetProperty("guestMessagesRemaining").GetInt32());
        }

        Assert.Equal(ChatMessageManager.GuestMessageCap, CountMessages(groupId));
    }

    [Fact]
    public void SendPastTheCapReturnsGuestLimitReachedAndStoresNothing() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        SendCapMessages(testingMockProvidersContainer, guestAuthToken, groupId);

        JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "one too many");

        Assert.Equal("guestLimitReached", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("message").ValueKind);
        Assert.Equal(0, root.GetProperty("guestMessagesRemaining").GetInt32());
        Assert.Equal(ChatMessageManager.GuestMessageCap, CountMessages(groupId));
        Assert.Equal(ChatMessageManager.GuestMessageCap, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void WallRefusalClaimsNoSequence() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));
        SendCapMessages(testingMockProvidersContainer, guestAuthToken, groupId);

        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "refused at the wall");
        JsonElement root = Send(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "from the owner");

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(ChatMessageManager.GuestMessageCap + 1, root.GetProperty("message").GetProperty("sequence").GetInt64());
    }

    [Fact]
    public void ListPageCarriesRemainingForGuestsAndNullForVerified() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));
        for (int sendNumber = 1; sendNumber <= 4; sendNumber++)
            Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "message " + sendNumber);

        JsonElement guestRoot = ListPage(testingMockProvidersContainer, guestAuthToken, groupId);
        JsonElement ownerRoot = ListPage(testingMockProvidersContainer, ownerAuthToken, groupId);

        Assert.Equal(ChatMessageManager.GuestMessageCap - 4, guestRoot.GetProperty("guestMessagesRemaining").GetInt32());
        Assert.Equal(JsonValueKind.Null, ownerRoot.GetProperty("guestMessagesRemaining").ValueKind);
    }

    // Tests - Idempotency At The Boundary

    [Fact]
    public void DuplicateRetryAtTheCapReturnsDuplicateNotTheWall() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));
        for (int sendNumber = 1; sendNumber < ChatMessageManager.GuestMessageCap; sendNumber++)
            Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "message " + sendNumber);
        Guid finalClientMessageId = Guid.NewGuid();
        JsonElement finalRoot = Send(testingMockProvidersContainer, guestAuthToken, groupId, finalClientMessageId, "the final counted message");

        JsonElement retryRoot = Send(testingMockProvidersContainer, guestAuthToken, groupId, finalClientMessageId, "the final counted message");

        Assert.Equal("sent", finalRoot.GetProperty("status").GetString());
        Assert.Equal("duplicate", retryRoot.GetProperty("status").GetString());
        Assert.Equal(finalRoot.GetProperty("message").GetProperty("id").GetString(), retryRoot.GetProperty("message").GetProperty("id").GetString());
        Assert.Equal(0, retryRoot.GetProperty("guestMessagesRemaining").GetInt32());
        Assert.Equal(ChatMessageManager.GuestMessageCap, CountMessages(groupId));
    }

    [Fact]
    public void DuplicateRetriesNeverDoubleCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        Guid clientMessageId = Guid.NewGuid();

        Send(testingMockProvidersContainer, guestAuthToken, groupId, clientMessageId, "hello");
        Send(testingMockProvidersContainer, guestAuthToken, groupId, clientMessageId, "hello");

        Assert.Equal(1, GetGuestMessageCount(guestUserAccountId));
        Assert.Equal(1, CountMessages(groupId));
    }

    [Fact]
    public void InvalidSendsDoNotCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);

        JsonElement emptyBodyRoot = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "");
        JsonElement unknownParentRoot = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = guestAuthToken, ChatGroupId = groupId, ClientMessageId = Guid.NewGuid(), Body = "hello", ReplyToMessageId = Guid.NewGuid() }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("invalidBody", emptyBodyRoot.GetProperty("status").GetString());
        Assert.Equal("invalidReply", unknownParentRoot.GetProperty("status").GetString());
        Assert.Equal(0, GetGuestMessageCount(guestUserAccountId));
    }

    [Fact]
    public void ConcurrentGuestSendsNeverExceedTheCap() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
            Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
            Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
            AddActiveMember(groupId, guestUserAccountId);
            SetGuestMessageCount(guestUserAccountId, ChatMessageManager.GuestMessageCap - 1);

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [
                new Thread(() => { try { Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "racing for the last slot"); } catch (Exception error) { errors.Add(error); } }),
                new Thread(() => { try { Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "also racing for it"); } catch (Exception error) { errors.Add(error); } })
            ];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.Equal(1, CountMessages(groupId));
            Assert.Equal(ChatMessageManager.GuestMessageCap, GetGuestMessageCount(guestUserAccountId));
        }
    }

    // Tests - Upgrade

    [Fact]
    public void UpgradeLiftsTheCap() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));
        SendCapMessages(testingMockProvidersContainer, guestAuthToken, groupId);
        Assert.Equal("guestLimitReached", Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "blocked").GetProperty("status").GetString());

        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Casey", $"upgraded{Guid.NewGuid():N}@gmail.com", "Seven74!");
        JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "free at last");

        Assert.Equal("sent", root.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("guestMessagesRemaining").ValueKind);
    }

    [Fact]
    public void UpgradedGuestsMessagesKeepAttributionAndShowTheNewDisplayName() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid guestUserAccountId = ResolveUserAccountId(guestAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, guestUserAccountId);
        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "sent while a guest");
        Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "also sent while a guest");

        RegisterGuestWithEmail(testingMockProvidersContainer, guestAuthToken, "Casey Jordan", $"upgraded{Guid.NewGuid():N}@gmail.com", "Seven74!");
        JsonElement root = ListPage(testingMockProvidersContainer, guestAuthToken, groupId);

        foreach (JsonElement item in root.GetProperty("items").EnumerateArray())
            Assert.Equal(guestUserAccountId.ToString(), item.GetProperty("senderUserAccountId").GetString());
        Assert.Equal(1, root.GetProperty("senders").GetArrayLength());
        Assert.Equal("Casey Jordan", root.GetProperty("senders")[0].GetProperty("displayName").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("guestMessagesRemaining").ValueKind);
    }

    // Tests - Response Shape

    [Fact]
    public void GuestLimitReachedResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string guestAuthToken = TestUserFactory.CreateGuestUser(testingMockProvidersContainer);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(CreateUser(testingMockProvidersContainer, "Owner")), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(guestAuthToken));
        SendCapMessages(testingMockProvidersContainer, guestAuthToken, groupId);

        JsonElement root = Send(testingMockProvidersContainer, guestAuthToken, groupId, Guid.NewGuid(), "over the wall");
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["guestMessagesRemaining", "message", "status"];

        Assert.Equal(expectedProperties, actualProperties);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("message").ValueKind);
        Assert.Equal(0, root.GetProperty("guestMessagesRemaining").GetInt32());
    }

    // Helpers - Acting

    private static readonly string HeartEmoji = "\u2764\uFE0F";

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid clientMessageId, string body) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = clientMessageId, Body = body }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static List<Guid> SendCapMessages(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        List<Guid> sentMessageIds = [];
        for (int sendNumber = 1; sendNumber <= ChatMessageManager.GuestMessageCap; sendNumber++) {
            JsonElement root = Send(testingMockProvidersContainer, authToken, chatGroupId, Guid.NewGuid(), "counted message " + sendNumber);
            sentMessageIds.Add(Guid.Parse(root.GetProperty("message").GetProperty("id").GetString()));
        }
        return sentMessageIds;
    }

    private static JsonElement ListPage(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static void React(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId, string emoji) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId, Emoji = emoji }).EnsureSuccessStatusCode();
    }

    private static void Typing(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/typing", new { AuthToken = authToken, ChatGroupId = chatGroupId }).EnsureSuccessStatusCode();
    }

    private static void MarkRead(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, long upToSequence) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/markRead", new { AuthToken = authToken, ChatGroupId = chatGroupId, UpToSequence = upToSequence }).EnsureSuccessStatusCode();
    }

    private static void DeleteOwn(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId) {
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId }).EnsureSuccessStatusCode();
    }

    private static void RegisterGuestWithEmail(TestingMockProvidersContainer testingMockProvidersContainer, string guestAuthToken, string name, string email, string password) {
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/signUpWithEmail", new { AuthToken = guestAuthToken, Name = name, Email = email, Password = password }).EnsureSuccessStatusCode();
        MailMessage verificationEmail = testingMockProvidersContainer.EmailProvider.EmailMessages.Last();
        string verificationCode = EmailVerificationNotification.ExtractVerificationCode(verificationEmail);
        testingMockProvidersContainer.WebClient.PostJson("api/userAuthentication/verifyEmail", new { Email = email, VerificationCode = verificationCode }).EnsureSuccessStatusCode();
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

    private static Guid SeedImageAsset(Guid groupId, Guid uploaderUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid mediaId = Guid.NewGuid();
        byte[] contentBytes = [1, 2, 3, 4];
        dbContext.ChatMediaAssets.Add(new ChatMediaAsset { Id = mediaId, ChatGroupId = groupId, UploaderUserAccountId = uploaderUserAccountId, Kind = ChatMessageKind.Image, StorageMode = (ChatMediaStorageMode)1, ContentBytes = contentBytes, ContentType = "image/jpeg", ByteCount = contentBytes.Length, Width = 800, Height = 600, CipherVersion = 0, CreatedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
        return mediaId;
    }

    private static void SetGuestMessageCount(Guid userAccountId, int guestMessageCount) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.UserAccounts.Where(field => field.Id == userAccountId).ExecuteUpdate(setters => setters.SetProperty(field => field.GuestMessageCount, guestMessageCount));
    }

    // Helpers - Reading

    private static int GetGuestMessageCount(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.UserAccounts.Single(field => field.Id == userAccountId).GuestMessageCount;
    }

    private static int CountMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Count(field => field.ChatGroupId == groupId);
    }
}
