using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class OwnershipClaimOnRejoinTest {
    // Tests - Rejoin Heals An Ownerless Group

    [Fact]
    public void RejoiningAnOwnerlessGroupClaimsOwnership() {
        using var container = new TestingMockProvidersContainer();
        string memberAuthToken = CreateUser(container, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateOwnerlessGroup("Orphaned Group");
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = Join(container, memberAuthToken, groupId);

        Assert.Equal("joined", root.GetProperty("status").GetString());
        Assert.Equal(memberUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.True(IsActiveOwner(groupId, memberUserAccountId));
    }

    // Tests - Rejoin Never Disturbs An Owned Group

    [Fact]
    public void RejoiningAnOwnedGroupDoesNotChangeOwnership() {
        using var container = new TestingMockProvidersContainer();
        Guid ownerUserAccountId = SeedUser("Owner", null);
        string memberAuthToken = CreateUser(container, "Member");
        Guid memberUserAccountId = ResolveUserAccountId(memberAuthToken);
        Guid groupId = CreateOwnedGroup(ownerUserAccountId, "Owned Group");
        AddActiveMember(groupId, memberUserAccountId);

        JsonElement root = Join(container, memberAuthToken, groupId);

        Assert.Equal("joined", root.GetProperty("status").GetString());
        Assert.Equal(ownerUserAccountId, GetOwnerUserAccountId(groupId));
        Assert.False(IsActiveOwner(groupId, memberUserAccountId));
        Assert.True(IsActiveOwner(groupId, ownerUserAccountId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static JsonElement Join(TestingMockProvidersContainer container, string authToken, Guid chatGroupId) {
        return container.WebClient.PostJson("api/helpOffer/join", new { AuthToken = authToken, ChatGroupId = chatGroupId }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static Guid CreateOwnerlessGroup(string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = null, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.SaveChanges();
        return groupId;
    }

    private static Guid CreateOwnedGroup(Guid ownerUserAccountId, string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
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

    private static Guid? GetOwnerUserAccountId(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == groupId).OwnerUserAccountId;
    }

    private static bool IsActiveOwner(Guid groupId, Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == groupId && field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Active && field.MemberRole == ChatGroupMemberRole.Owner);
    }
}
