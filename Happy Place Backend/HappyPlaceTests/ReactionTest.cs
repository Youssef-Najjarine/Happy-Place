using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ReactionTest {
    // Emoji Constants (escaped to survive any encoding round-trip)

    private const string HeartEmoji = "\u2764\uFE0F";
    private const string ThumbsUpEmoji = "\uD83D\uDC4D";
    private const string HappyEmoji = "\uD83D\uDE0A";
    private const string SadEmoji = "\uD83D\uDE22";
    private const string ExclamationEmoji = "\u203C\uFE0F";
    private const string QuestionEmoji = "\u2753";
    private const string SkinToneThumbsUpEmoji = "\uD83D\uDC4D\uD83C\uDFFD";
    private const string FamilyEmoji = "\uD83D\uDC68\u200D\uD83D\uDC69\u200D\uD83D\uDC67\u200D\uD83D\uDC66";
    private const string KeycapOneEmoji = "1\uFE0F\u20E3";

    // Tests - Authentication Failures

    [Fact]
    public void ReactEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Emoji = HeartEmoji });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ReactInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Emoji = HeartEmoji });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ReactMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Emoji = HeartEmoji });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Access Gates

    [Fact]
    public void StrangerReturnsNotMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string strangerAuthToken = CreateUser(testingMockProvidersContainer, "Stranger");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, strangerAuthToken, groupId, messageId, HeartEmoji);

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void UnknownGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");

        JsonElement root = React(testingMockProvidersContainer, memberAuthToken, Guid.NewGuid(), Guid.NewGuid(), HeartEmoji);

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Message Gates

    [Fact]
    public void UnknownMessageReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), HeartEmoji);

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void MessageFromOtherGroupReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "First Group", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Second Group", true);
        Guid foreignMessageId = Send(testingMockProvidersContainer, ownerAuthToken, secondGroupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, firstGroupId, foreignMessageId, HeartEmoji);

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(foreignMessageId));
    }

    [Fact]
    public void DeletedMessageReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        MarkMessageDeleted(messageId);

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    // Tests - Emoji Validation

    [Fact]
    public void AsciiTextReturnsInvalidEmoji() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "nice");

        Assert.Equal("invalidEmoji", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void MixedEmojiAndLettersReturnsInvalidEmoji() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji + "x");

        Assert.Equal("invalidEmoji", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void TooLongEmojiSequenceReturnsInvalidEmoji() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, new string('#', 21));

        Assert.Equal("invalidEmoji", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void KeycapEmojiIsAccepted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, KeycapOneEmoji);

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(KeycapOneEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    [Fact]
    public void SkinToneEmojiIsAccepted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, SkinToneThumbsUpEmoji);

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(SkinToneThumbsUpEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    [Fact]
    public void ZwjFamilyEmojiIsAccepted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, FamilyEmoji);

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(FamilyEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    [Fact]
    public void PaddedEmojiIsTrimmedAndStored() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, " " + HeartEmoji + " ");

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(HeartEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    // Tests - Set Replace Remove

    [Fact]
    public void ReactStoresRowAndReturnsReacted() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(HeartEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    [Fact]
    public void ReplaceChangesEmojiKeepsSingleRow() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HappyEmoji);

        Assert.Equal("reacted", root.GetProperty("status").GetString());
        Assert.Equal(1, CountReactions(messageId));
        Assert.Equal(HappyEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
    }

    [Fact]
    public void RemoveDeletesRowAndReturnsRemoved() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, ThumbsUpEmoji);

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "");

        Assert.Equal("removed", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void RemoveWhenNothingExistsStillReturnsRemoved() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "");

        Assert.Equal("removed", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void WhitespaceOnlyActsAsRemove() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, ThumbsUpEmoji);

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "   ");

        Assert.Equal("removed", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReactions(messageId));
    }

    [Fact]
    public void TwoUsersReactIndependently() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);
        React(testingMockProvidersContainer, memberAuthToken, groupId, messageId, SadEmoji);

        Assert.Equal(2, CountReactions(messageId));
        Assert.Equal(HeartEmoji, GetReactionEmoji(messageId, ownerUserAccountId));
        Assert.Equal(SadEmoji, GetReactionEmoji(messageId, memberUserAccountId));
    }

    // Tests - Change Propagation

    [Fact]
    public void ReactionBumpsChangeSequenceAndSurfacesInPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        JsonElement quietPoll = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 1);
        Assert.Equal(0, quietPoll.GetProperty("changes").GetArrayLength());

        React(testingMockProvidersContainer, memberAuthToken, groupId, messageId, ExclamationEmoji);
        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 1);

        Assert.Equal(1, root.GetProperty("changes").GetArrayLength());
        JsonElement change = root.GetProperty("changes")[0];
        Assert.Equal(messageId.ToString(), change.GetProperty("id").GetString());
        Assert.Equal(1, change.GetProperty("reactions").GetArrayLength());
        Assert.Equal(memberUserAccountId.ToString(), change.GetProperty("reactions")[0].GetProperty("userAccountId").GetString());
        Assert.Equal(ExclamationEmoji, change.GetProperty("reactions")[0].GetProperty("emoji").GetString());
        Assert.Equal(2, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void RemovalAlsoSurfacesInPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        React(testingMockProvidersContainer, memberAuthToken, groupId, messageId, ExclamationEmoji);

        React(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "");
        JsonElement root = Poll(testingMockProvidersContainer, ownerAuthToken, groupId, 2);

        Assert.Equal(1, root.GetProperty("changes").GetArrayLength());
        Assert.Equal(0, root.GetProperty("changes")[0].GetProperty("reactions").GetArrayLength());
        Assert.Equal(3, root.GetProperty("changeSequence").GetInt64());
    }

    [Fact]
    public void ReactionsAppearInListPageItems() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, ThumbsUpEmoji);
        React(testingMockProvidersContainer, memberAuthToken, groupId, messageId, QuestionEmoji);

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/listPage", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).ReadContentAsJsonDocument().RootElement.Clone();

        JsonElement reactions = root.GetProperty("items")[0].GetProperty("reactions");
        Assert.Equal(2, reactions.GetArrayLength());
        List<string> reactorIds = [.. reactions.EnumerateArray().Select(reaction => reaction.GetProperty("userAccountId").GetString())];
        List<string> expectedReactorIds = [.. new[] { ownerUserAccountId, memberUserAccountId }.OrderBy(id => id).Select(id => id.ToString())];
        Assert.Equal(expectedReactorIds, reactorIds);
    }

    [Fact]
    public void SendsAndReactionsKeepMessageSequencesGapFree() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        Guid firstMessageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "first");
        React(testingMockProvidersContainer, ownerAuthToken, groupId, firstMessageId, HeartEmoji);
        Send(testingMockProvidersContainer, ownerAuthToken, groupId, "second");

        List<ChatMessage> messages = LoadMessages(groupId);
        List<long> sequences = [.. messages.Select(message => message.Sequence).OrderBy(sequence => sequence)];
        List<long> changeSequences = [.. messages.OrderBy(message => message.Sequence).Select(message => message.ChangeSequence)];

        Assert.Equal([1, 2], sequences);
        Assert.Equal([2, 3], changeSequences);
    }

    // Tests - Response Shape

    [Fact]
    public void ReactResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = React(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, HeartEmoji);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["status"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static Guid Send(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, string body) {
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/send", new { AuthToken = authToken, ChatGroupId = chatGroupId, ClientMessageId = Guid.NewGuid(), Body = body }).ReadContentAsJsonDocument().RootElement;
        return Guid.Parse(root.GetProperty("message").GetProperty("id").GetString());
    }

    private static JsonElement React(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId, string emoji) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/react", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId, Emoji = emoji }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static void AddActiveMember(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = userAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = DateTime.UtcNow });
        dbContext.SaveChanges();
    }

    private static void MarkMessageDeleted(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatMessages.Where(field => field.Id == messageId).ExecuteUpdate(setters => setters.SetProperty(field => field.IsDeleted, true));
    }

    // Helpers - Reading

    private static int CountReactions(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReactions.Count(field => field.ChatMessageId == messageId);
    }

    private static string GetReactionEmoji(Guid messageId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReactions.Single(field => field.ChatMessageId == messageId && field.UserAccountId == userAccountId).Emoji;
    }

    private static List<ChatMessage> LoadMessages(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return [.. dbContext.ChatMessages.Where(field => field.ChatGroupId == groupId)];
    }
}
