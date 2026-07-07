using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ClaimOwnershipOnJoinTest {
    // Tests - First Joiner Claims Ownership

    [Fact]
    public void FirstJoinerOfOwnerlessGroupBecomesOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
        Guid groupId = CreateOwnerlessPublicGroup("Ownerless Group");

        Join(testingMockProvidersContainer, joinerAuthToken, groupId);

        Assert.Equal(joinerUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, joinerUserAccountId));
    }

    [Fact]
    public void JoiningOwnerlessGroupReturnsJoined() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid groupId = CreateOwnerlessPublicGroup("Ownerless Group");

        JsonElement root = Join(testingMockProvidersContainer, joinerAuthToken, groupId);

        Assert.Equal("joined", root.GetProperty("status").GetString());
    }

    [Fact]
    public void ClaimSetsBothGroupOwnerAndMemberRole() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
        Guid groupId = CreateOwnerlessPublicGroup("Ownerless Group");

        Join(testingMockProvidersContainer, joinerAuthToken, groupId);

        Assert.Equal(joinerUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.Equal(ChatGroupMemberRole.Owner, GetMemberRole(groupId, joinerUserAccountId));
        Assert.Equal(1, CountActiveMembers(groupId));
    }

    // Tests - Later Joiners Stay Members

    [Fact]
    public void SecondJoinerOfClaimedGroupStaysMember() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string firstJoinerAuthToken = CreateUser(testingMockProvidersContainer, "First");
        Guid firstJoinerUserAccountId = ResolveUserAccountId(firstJoinerAuthToken);
        string secondJoinerAuthToken = CreateUser(testingMockProvidersContainer, "Second");
        Guid secondJoinerUserAccountId = ResolveUserAccountId(secondJoinerAuthToken);
        Guid groupId = CreateOwnerlessPublicGroup("Ownerless Group");
        Join(testingMockProvidersContainer, firstJoinerAuthToken, groupId);

        Join(testingMockProvidersContainer, secondJoinerAuthToken, groupId);

        Assert.Equal(firstJoinerUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, firstJoinerUserAccountId));
        Assert.Equal(ChatGroupMemberRole.Member, GetMemberRole(groupId, secondJoinerUserAccountId));
    }

    [Fact]
    public void JoinerOfOwnedGroupDoesNotBecomeOwner() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(testingMockProvidersContainer, "Owner");
        Guid ownerUserAccountId = ResolveUserAccountId(ownerAuthToken);
        string joinerAuthToken = CreateUser(testingMockProvidersContainer, "Joiner");
        Guid joinerUserAccountId = ResolveUserAccountId(joinerAuthToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Owned Group", true);

        Join(testingMockProvidersContainer, joinerAuthToken, groupId);

        Assert.Equal(ownerUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, ownerUserAccountId));
        Assert.Equal(ChatGroupMemberRole.Member, GetMemberRole(groupId, joinerUserAccountId));
    }

    // Tests - Concurrency

    [Fact]
    public void ConcurrentJoinsOnOwnerlessGroupProduceExactlyOneOwner() {
        for (int trial = 0; trial < 5; trial++) {
            using var testingMockProvidersContainer = new TestingMockProvidersContainer();
            Guid groupId = CreateOwnerlessPublicGroup("Ownerless Group");
            List<string> joinerAuthTokens = [
                CreateUser(testingMockProvidersContainer, "Joiner A"),
                CreateUser(testingMockProvidersContainer, "Joiner B"),
                CreateUser(testingMockProvidersContainer, "Joiner C"),
                CreateUser(testingMockProvidersContainer, "Joiner D"),
                CreateUser(testingMockProvidersContainer, "Joiner E")
            ];

            ConcurrentBag<Exception> errors = [];
            List<Thread> threads = [.. joinerAuthTokens.Select(joinerAuthToken => new Thread(() => {
                try { Join(testingMockProvidersContainer, joinerAuthToken, groupId); }
                catch (Exception error) { errors.Add(error); }
            }))];
            foreach (Thread thread in threads)
                thread.Start();
            foreach (Thread thread in threads)
                thread.Join();

            Assert.Empty(errors);
            Assert.Equal(joinerAuthTokens.Count, CountActiveMembers(groupId));
            Assert.Equal(1, CountActiveOwners(groupId));
            Guid? ownerUserAccountId = GetOwnerUserAccountId(groupId);
            Assert.NotNull(ownerUserAccountId);
            Assert.True(IsActiveOwner(groupId, ownerUserAccountId.Value));
        }
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement Join(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, Guid chatGroupId) {
        return testingMockProvidersContainer.WebClient.PostJson("api/helpOffer/join", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
    }

    // Helpers - Seeding

    private static Guid ResolveUserAccountId(string authToken) {
        return Guid.Parse(UserAuthenticationToken.ValidateToken(authToken).Identifier);
    }

    private static Guid CreateOwnerlessPublicGroup(string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = null, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
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

    // Helpers - Reading

    private static Guid? GetOwnerUserAccountId(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).OwnerUserAccountId;
    }

    private static bool IsActiveOwner(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active && field.MemberRole == ChatGroupMemberRole.Owner);
    }

    private static ChatGroupMemberRole GetMemberRole(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Single(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId).MemberRole;
    }

    private static int CountActiveMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.Status == ChatGroupMemberStatus.Active);
    }

    private static int CountActiveOwners(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.Status == ChatGroupMemberStatus.Active && field.MemberRole == ChatGroupMemberRole.Owner);
    }
}
