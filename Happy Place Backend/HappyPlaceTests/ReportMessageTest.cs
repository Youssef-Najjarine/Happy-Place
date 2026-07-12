using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ReportMessageTest {
    // Tests - Authentication Failures

    [Fact]
    public void ReportEmptyTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { AuthToken = "", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Reason = "spam" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ReportInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { AuthToken = "not-a-real-token-at-all", ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Reason = "spam" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ReportMissingAuthTokenFieldReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { ChatGroupId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Reason = "spam" });

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

        JsonElement root = Report(testingMockProvidersContainer, strangerAuthToken, groupId, messageId, "spam");

        Assert.Equal("notMember", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReports(messageId));
    }

    [Fact]
    public void SoftDeletedGroupReturnsGroupGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/delete", new { AuthToken = ownerAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "spam");

        Assert.Equal("groupGone", root.GetProperty("status").GetString());
    }

    // Tests - Message Gates

    [Fact]
    public void UnknownMessageReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);

        JsonElement root = Report(testingMockProvidersContainer, ownerAuthToken, groupId, Guid.NewGuid(), "spam");

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
    }

    [Fact]
    public void DeletedMessageReturnsMessageGone() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MessageId = messageId }).EnsureSuccessStatusCode();

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "spam");

        Assert.Equal("messageGone", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReports(messageId));
    }

    [Fact]
    public void OwnMessageReturnsCannotReportOwn() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = Report(testingMockProvidersContainer, ownerAuthToken, groupId, messageId, "spam");

        Assert.Equal("cannotReportOwn", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReports(messageId));
    }

    // Tests - Snapshot Semantics

    [Fact]
    public void ReportStoresEncryptedSnapshot() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "My Group", true);
        AddActiveMember(groupId, memberUserAccountId);
        string body = "hurtful content " + Guid.NewGuid();
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, body);

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "this was unkind");

        Assert.Equal("reported", root.GetProperty("status").GetString());
        ChatMessageReport report = LoadSingleReport(messageId);
        Assert.Equal(memberUserAccountId, report.ReporterUserAccountId);
        Assert.Equal(ownerUserAccountId, report.ReportedUserAccountId);
        Assert.Equal(ChatMessageKind.Text, report.Kind);
        Assert.Equal(ChatMessageReportStatus.Open, report.Status);
        Assert.Equal(body, MessageCipher.Decrypt(report.BodySnapshotCipher));
        Assert.Equal("this was unkind", MessageCipher.Decrypt(report.ReasonCipher));
    }

    [Fact]
    public void ReportWithoutReasonStoresNullReason() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { AuthToken = memberAuthToken, ChatGroupId = groupId, MessageId = messageId }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal("reported", root.GetProperty("status").GetString());
        Assert.Null(LoadSingleReport(messageId).ReasonCipher);
    }

    [Fact]
    public void ReasonOverCapReturnsInvalidReason() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, new string('a', 501));

        Assert.Equal("invalidReason", root.GetProperty("status").GetString());
        Assert.Equal(0, CountReports(messageId));
    }

    [Fact]
    public void DuplicateReportReturnsAlreadyReported() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");
        Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "first report");

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "second report");

        Assert.Equal("alreadyReported", root.GetProperty("status").GetString());
        Assert.Equal(1, CountReports(messageId));
    }

    [Fact]
    public void TwoReportersCreateTwoRows() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string firstMemberAuthToken = CreateUser(testingMockProvidersContainer, "First Member");
        string secondMemberAuthToken = CreateUser(testingMockProvidersContainer, "Second Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(firstMemberAuthToken));
        AddActiveMember(groupId, ResolveUserAccountId(secondMemberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        Report(testingMockProvidersContainer, firstMemberAuthToken, groupId, messageId, "spam");
        Report(testingMockProvidersContainer, secondMemberAuthToken, groupId, messageId, "spam");

        Assert.Equal(2, CountReports(messageId));
    }

    [Fact]
    public void SnapshotSurvivesSenderDeleteOwn() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        string body = "evidence that must survive " + Guid.NewGuid();
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, body);
        Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "harassment");

        testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/deleteOwn", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, MessageId = messageId }).EnsureSuccessStatusCode();

        ChatMessageReport report = LoadSingleReport(messageId);
        Assert.Equal(body, MessageCipher.Decrypt(report.BodySnapshotCipher));
        Assert.True(LoadMessage(messageId).IsDeleted);
    }

    [Fact]
    public void ReportIsInvisibleToOtherMembersPoll() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "spam");
        JsonElement root = testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/poll", new { AuthToken = ownerAuthToken, ChatGroupId = groupId, SinceChangeSequence = 1 }).ReadContentAsJsonDocument().RootElement.Clone();

        Assert.Equal(0, root.GetProperty("changes").GetArrayLength());
        Assert.Equal(1, root.GetProperty("changeSequence").GetInt64());
    }

    // Tests - Response Shape

    [Fact]
    public void ReportResponseContainsExactlyExpectedProperties() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        string memberAuthToken = CreateUser(testingMockProvidersContainer, "Member");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "My Group", true);
        AddActiveMember(groupId, ResolveUserAccountId(memberAuthToken));
        Guid messageId = Send(testingMockProvidersContainer, ownerAuthToken, groupId, "hello");

        JsonElement root = Report(testingMockProvidersContainer, memberAuthToken, groupId, messageId, "spam");
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

    private static JsonElement Report(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId, Guid messageId, string reason) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatMessage/report", new { AuthToken = authToken, ChatGroupId = chatGroupId, MessageId = messageId, Reason = reason }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static int CountReports(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReports.Count(field => field.ChatMessageId == messageId);
    }

    private static ChatMessageReport LoadSingleReport(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessageReports.Single(field => field.ChatMessageId == messageId);
    }

    private static ChatMessage LoadMessage(Guid messageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatMessages.Single(field => field.Id == messageId);
    }
}
