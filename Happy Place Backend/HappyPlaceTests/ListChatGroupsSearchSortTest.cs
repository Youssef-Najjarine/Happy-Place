using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListChatGroupsSearchSortTest {
    // Tests - Search

    [Fact]
    public void SearchEmptyStringReturnsAllActiveGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid firstGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);
        Guid secondGroupId = CreateActiveGroup(ownerUserAccountId, "Grief Circle", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "");

        Assert.Equal(2, root.GetArrayLength());
        Assert.True(ContainsGroup(root, firstGroupId));
        Assert.True(ContainsGroup(root, secondGroupId));
    }

    [Fact]
    public void SearchNullReturnsAllActiveGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, null);

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchWhitespaceOnlyReturnsAllActiveGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "   ");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchMatchesTitleSubstring() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "xiety");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchMatchesCaseInsensitively() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "anxiety");

        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchExcludesNonMatchingGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid matchingGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);
        Guid otherGroupId = CreateActiveGroup(ownerUserAccountId, "Depression Circle", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Anxiety");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, matchingGroupId));
        Assert.False(ContainsGroup(root, otherGroupId));
    }

    [Fact]
    public void SearchWithNoMatchReturnsEmptyArray() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "nonexistent-topic");

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public void SearchMatchesMultipleGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid firstMatchId = CreateActiveGroup(ownerUserAccountId, "Anxiety Support", true);
        Guid secondMatchId = CreateActiveGroup(ownerUserAccountId, "Anxiety Circle", true);
        Guid nonMatchId = CreateActiveGroup(ownerUserAccountId, "Depression Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Anxiety");

        Assert.Equal(2, root.GetArrayLength());
        Assert.True(ContainsGroup(root, firstMatchId));
        Assert.True(ContainsGroup(root, secondMatchId));
        Assert.False(ContainsGroup(root, nonMatchId));
    }

    [Fact]
    public void SearchMatchesExactFullTitle() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(ownerUserAccountId, "Grief", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Grief");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchTreatsPercentSignAsLiteral() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid literalGroupId = CreateActiveGroup(ownerUserAccountId, "50% Discount", true);
        Guid wildcardTrapGroupId = CreateActiveGroup(ownerUserAccountId, "50 Dollars", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "50%");

        Assert.True(ContainsGroup(root, literalGroupId));
        Assert.False(ContainsGroup(root, wildcardTrapGroupId));
    }

    [Fact]
    public void SearchTreatsUnderscoreAsLiteral() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid literalGroupId = CreateActiveGroup(ownerUserAccountId, "Team_A Chat", true);
        Guid wildcardTrapGroupId = CreateActiveGroup(ownerUserAccountId, "TeamXA Chat", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Team_A");

        Assert.True(ContainsGroup(root, literalGroupId));
        Assert.False(ContainsGroup(root, wildcardTrapGroupId));
    }

    [Fact]
    public void SearchIncludesOtherUsersPrivateGroupsInDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid otherOwnerUserAccountId = SeedUser("Other Owner", null);
        Guid privateGroupId = CreateActiveGroup(otherOwnerUserAccountId, "Secret Anxiety Room", false);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Anxiety");

        Assert.True(ContainsGroup(root, privateGroupId));
        Assert.False(GetGroup(root, privateGroupId).GetProperty("joined").GetBoolean());
    }

    // Tests - Popular Sort

    [Fact]
    public void PopularOrdersByActiveMemberCountDescending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid largeGroupId = CreateActiveGroup(SeedUser("Owner Large", null), "Large", true);
        AddActiveMembers(largeGroupId, 2);
        Guid mediumGroupId = CreateActiveGroup(SeedUser("Owner Medium", null), "Medium", true);
        AddActiveMembers(mediumGroupId, 1);
        Guid smallGroupId = CreateActiveGroup(SeedUser("Owner Small", null), "Small", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.True(IndexOfGroup(root, largeGroupId) < IndexOfGroup(root, mediumGroupId));
        Assert.True(IndexOfGroup(root, mediumGroupId) < IndexOfGroup(root, smallGroupId));
    }

    [Fact]
    public void PopularBreaksTiesByMostRecentlyCreated() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid olderGroupId = CreateActiveGroup(SeedUser("Owner Older", null), "Older Equal", true);
        Guid newerGroupId = CreateActiveGroup(SeedUser("Owner Newer", null), "Newer Equal", true);
        SetGroupCreatedAt(olderGroupId, DateTime.UtcNow.AddHours(-2));
        SetGroupCreatedAt(newerGroupId, DateTime.UtcNow.AddHours(-1));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.True(IndexOfGroup(root, newerGroupId) < IndexOfGroup(root, olderGroupId));
    }

    [Fact]
    public void PopularRanksLargerDiscoveryGroupAboveSmallerOwnedGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "My Owned", true);
        Guid discoveryGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Popular Discovery", true);
        AddActiveMembers(discoveryGroupId, 3);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.True(IndexOfGroup(root, discoveryGroupId) < IndexOfGroup(root, ownedGroupId));
    }

    [Fact]
    public void PopularCountsActiveMembersNotPendingMembers() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid pendingHeavyGroupId = CreateActiveGroup(SeedUser("Owner Pending", null), "Pending Heavy", true);
        AddPendingMembers(pendingHeavyGroupId, 3);
        Guid activeHeavyGroupId = CreateActiveGroup(SeedUser("Owner Active", null), "Active Heavy", true);
        AddActiveMembers(activeHeavyGroupId, 1);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.Equal(1, GetGroup(root, pendingHeavyGroupId).GetProperty("memberCount").GetInt32());
        Assert.Equal(2, GetGroup(root, activeHeavyGroupId).GetProperty("memberCount").GetInt32());
        Assert.True(IndexOfGroup(root, activeHeavyGroupId) < IndexOfGroup(root, pendingHeavyGroupId));
    }

    // Tests - Most Active Sort

    [Fact]
    public void MostActiveOrdersByLastSeenDescending() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid staleGroupId = CreateActiveGroup(SeedUser("Owner Stale", null), "Stale", true);
        Guid recentGroupId = CreateActiveGroup(SeedUser("Owner Recent", null), "Recent", true);
        Guid middleGroupId = CreateActiveGroup(SeedUser("Owner Middle", null), "Middle", true);
        SetGroupLastSeenAt(staleGroupId, DateTime.UtcNow.AddHours(-3));
        SetGroupLastSeenAt(middleGroupId, DateTime.UtcNow.AddHours(-2));
        SetGroupLastSeenAt(recentGroupId, DateTime.UtcNow.AddHours(-1));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Most Active", null);

        Assert.True(IndexOfGroup(root, recentGroupId) < IndexOfGroup(root, middleGroupId));
        Assert.True(IndexOfGroup(root, middleGroupId) < IndexOfGroup(root, staleGroupId));
    }

    [Fact]
    public void MostActiveBreaksTiesByMostRecentlyCreated() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid olderGroupId = CreateActiveGroup(SeedUser("Owner Older", null), "Older Equal", true);
        Guid newerGroupId = CreateActiveGroup(SeedUser("Owner Newer", null), "Newer Equal", true);
        DateTime sharedLastSeen = DateTime.UtcNow.AddHours(-1);
        SetGroupLastSeenAt(olderGroupId, sharedLastSeen);
        SetGroupLastSeenAt(newerGroupId, sharedLastSeen);
        SetGroupCreatedAt(olderGroupId, DateTime.UtcNow.AddHours(-5));
        SetGroupCreatedAt(newerGroupId, DateTime.UtcNow.AddHours(-4));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Most Active", null);

        Assert.True(IndexOfGroup(root, newerGroupId) < IndexOfGroup(root, olderGroupId));
    }

    [Fact]
    public void MostActiveRanksMoreRecentlyActiveGroupFirstRegardlessOfOwnership() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedStaleGroupId = CreateActiveGroup(requesterUserAccountId, "My Stale Group", true);
        Guid discoveryRecentGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Discovery Recent", true);
        SetGroupLastSeenAt(ownedStaleGroupId, DateTime.UtcNow.AddHours(-3));
        SetGroupLastSeenAt(discoveryRecentGroupId, DateTime.UtcNow.AddHours(-1));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Most Active", null);

        Assert.True(IndexOfGroup(root, discoveryRecentGroupId) < IndexOfGroup(root, ownedStaleGroupId));
    }

    // Tests - Public Filter

    [Fact]
    public void PublicReturnsOnlyPublicGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid publicGroupId = CreateActiveGroup(requesterUserAccountId, "Open Group", true);
        Guid ownedPrivateGroupId = CreateActiveGroup(requesterUserAccountId, "My Private Group", false);
        Guid discoveryPrivateGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Their Private Group", false);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Public", null);

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, publicGroupId));
        Assert.False(ContainsGroup(root, ownedPrivateGroupId));
        Assert.False(ContainsGroup(root, discoveryPrivateGroupId));
    }

    [Fact]
    public void PublicOrdersOwnedPublicBeforeDiscoveryPublic() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid discoveryPublicGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Discovery Public", true);
        Guid ownedPublicGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Public", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Public", null);

        Assert.True(IndexOfGroup(root, ownedPublicGroupId) < IndexOfGroup(root, discoveryPublicGroupId));
    }

    // Tests - Private Filter

    [Fact]
    public void PrivateReturnsOnlyPrivateGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedPrivateGroupId = CreateActiveGroup(requesterUserAccountId, "My Private Group", false);
        Guid discoveryPrivateGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Their Private Group", false);
        Guid publicGroupId = CreateActiveGroup(requesterUserAccountId, "Open Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Private", null);

        Assert.Equal(2, root.GetArrayLength());
        Assert.True(ContainsGroup(root, ownedPrivateGroupId));
        Assert.True(ContainsGroup(root, discoveryPrivateGroupId));
        Assert.False(ContainsGroup(root, publicGroupId));
    }

    [Fact]
    public void PrivateOrdersOwnedPrivateBeforeDiscoveryPrivate() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid discoveryPrivateGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Discovery Private", false);
        Guid ownedPrivateGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Private", false);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Private", null);

        Assert.True(IndexOfGroup(root, ownedPrivateGroupId) < IndexOfGroup(root, discoveryPrivateGroupId));
    }

    // Tests - Latest And Fallback

    [Fact]
    public void LatestSortMatchesDefaultRelationshipOrder() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Owner Joined", null), "Joined Group", true);
        AddActiveMember(joinedGroupId, requesterUserAccountId);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner Pending", null), "Pending Group", false);
        AddPendingMember(pendingGroupId, requesterUserAccountId);
        Guid discoveryGroupId = CreateActiveGroup(SeedUser("Owner Discovery", null), "Discovery Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Latest", null);

        Assert.True(IndexOfGroup(root, ownedGroupId) < IndexOfGroup(root, joinedGroupId));
        Assert.True(IndexOfGroup(root, joinedGroupId) < IndexOfGroup(root, pendingGroupId));
        Assert.True(IndexOfGroup(root, pendingGroupId) < IndexOfGroup(root, discoveryGroupId));
    }

    [Fact]
    public void UnknownSortModeFallsBackToLatest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        Guid discoveryGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Discovery Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "A - Z", null);

        Assert.True(IndexOfGroup(root, ownedGroupId) < IndexOfGroup(root, discoveryGroupId));
    }

    [Fact]
    public void EmptySortModeFallsBackToLatest() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        Guid discoveryGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Discovery Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "", null);

        Assert.True(IndexOfGroup(root, ownedGroupId) < IndexOfGroup(root, discoveryGroupId));
    }

    // Tests - Combined Search And Sort

    [Fact]
    public void SearchCombinedWithPopularOrdersMatchesByMemberCount() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid anxietyLargeGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Anxiety Large", true);
        AddActiveMembers(anxietyLargeGroupId, 2);
        Guid anxietySmallGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Anxiety Small", true);
        Guid depressionLargeGroupId = CreateActiveGroup(SeedUser("Owner C", null), "Depression Large", true);
        AddActiveMembers(depressionLargeGroupId, 4);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", "Anxiety");

        Assert.Equal(2, root.GetArrayLength());
        Assert.False(ContainsGroup(root, depressionLargeGroupId));
        Assert.True(IndexOfGroup(root, anxietyLargeGroupId) < IndexOfGroup(root, anxietySmallGroupId));
    }

    [Fact]
    public void SearchCombinedWithPublicFiltersToMatchingPublicGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid anxietyPublicGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Public", true);
        Guid anxietyPrivateGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Private", false);
        Guid depressionPublicGroupId = CreateActiveGroup(ownerUserAccountId, "Depression Public", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Public", "Anxiety");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, anxietyPublicGroupId));
        Assert.False(ContainsGroup(root, anxietyPrivateGroupId));
        Assert.False(ContainsGroup(root, depressionPublicGroupId));
    }

    [Fact]
    public void SearchCombinedWithPrivateFiltersToMatchingPrivateGroups() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid ownerUserAccountId = ResolveUserAccountId(authToken);
        Guid anxietyPrivateGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Private", false);
        Guid anxietyPublicGroupId = CreateActiveGroup(ownerUserAccountId, "Anxiety Public", true);
        Guid depressionPrivateGroupId = CreateActiveGroup(ownerUserAccountId, "Depression Private", false);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Private", "Anxiety");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, anxietyPrivateGroupId));
        Assert.False(ContainsGroup(root, anxietyPublicGroupId));
        Assert.False(ContainsGroup(root, depressionPrivateGroupId));
    }

    [Fact]
    public void SearchCombinedWithMostActiveOrdersMatchesByActivity() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid anxietyStaleGroupId = CreateActiveGroup(SeedUser("Owner A", null), "Anxiety Stale", true);
        Guid anxietyRecentGroupId = CreateActiveGroup(SeedUser("Owner B", null), "Anxiety Recent", true);
        Guid depressionRecentGroupId = CreateActiveGroup(SeedUser("Owner C", null), "Depression Recent", true);
        SetGroupLastSeenAt(anxietyStaleGroupId, DateTime.UtcNow.AddHours(-3));
        SetGroupLastSeenAt(anxietyRecentGroupId, DateTime.UtcNow.AddHours(-1));
        SetGroupLastSeenAt(depressionRecentGroupId, DateTime.UtcNow.AddMinutes(-10));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Most Active", "Anxiety");

        Assert.Equal(2, root.GetArrayLength());
        Assert.False(ContainsGroup(root, depressionRecentGroupId));
        Assert.True(IndexOfGroup(root, anxietyRecentGroupId) < IndexOfGroup(root, anxietyStaleGroupId));
    }

    // Tests - Edge Cases

    [Fact]
    public void PopularWithNoGroupsReturnsEmptyArray() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public void PublicWithNoPublicGroupsReturnsEmptyArray() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        CreateActiveGroup(requesterUserAccountId, "Only Private Group", false);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Public", null);

        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public void ProvisionalGroupExcludedFromSearch() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid provisionalGroupId = CreateProvisionalGroup(requesterUserAccountId, "Anxiety Provisional", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Anxiety");

        Assert.Equal(0, root.GetArrayLength());
        Assert.False(ContainsGroup(root, provisionalGroupId));
    }

    [Fact]
    public void ProvisionalGroupExcludedFromPopular() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid provisionalGroupId = CreateProvisionalGroup(requesterUserAccountId, "Provisional Group", true);
        Guid activeGroupId = CreateActiveGroup(requesterUserAccountId, "Active Group", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        Assert.True(ContainsGroup(root, activeGroupId));
        Assert.False(ContainsGroup(root, provisionalGroupId));
    }

    [Fact]
    public void OwnerlessPublicGroupAppearsAndSortsUnderMostActive() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownerlessGroupId = CreateOwnerlessPublicGroup("Ownerless Public Group");
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        SetGroupLastSeenAt(ownerlessGroupId, DateTime.UtcNow.AddHours(-1));
        SetGroupLastSeenAt(ownedGroupId, DateTime.UtcNow.AddHours(-2));

        JsonElement root = List(testingMockProvidersContainer, authToken, "Most Active", null);

        Assert.True(ContainsGroup(root, ownerlessGroupId));
        Assert.False(GetGroup(root, ownerlessGroupId).GetProperty("owner").GetBoolean());
        Assert.Equal(0, GetGroup(root, ownerlessGroupId).GetProperty("memberCount").GetInt32());
        Assert.True(IndexOfGroup(root, ownerlessGroupId) < IndexOfGroup(root, ownedGroupId));
    }

    [Fact]
    public void SearchWithInvalidTokenReturnsUnauthorized() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();

        HttpResponseMessage response = testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = "not-a-real-token-at-all", SortBy = "Popular", Search = "Anxiety" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Additional Coverage

    [Fact]
    public void AvatarsDoNotLeakAcrossGroupsWhenLoadedTogether() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid firstGroupId = CreateActiveGroup(SeedUser("Owner One", null), "First Group", true);
        AddActiveMember(firstGroupId, SeedUser("Member One", "https://photos.example/one.png"));
        Guid secondGroupId = CreateActiveGroup(SeedUser("Owner Two", null), "Second Group", true);
        AddActiveMember(secondGroupId, SeedUser("Member Two", "https://photos.example/two.png"));

        JsonElement root = List(testingMockProvidersContainer, authToken, null, null);

        List<string> firstGroupPhotoUrls = HelperPhotoUrls(GetGroup(root, firstGroupId));
        List<string> secondGroupPhotoUrls = HelperPhotoUrls(GetGroup(root, secondGroupId));
        Assert.Contains("https://photos.example/one.png", firstGroupPhotoUrls);
        Assert.DoesNotContain("https://photos.example/two.png", firstGroupPhotoUrls);
        Assert.Contains("https://photos.example/two.png", secondGroupPhotoUrls);
        Assert.DoesNotContain("https://photos.example/one.png", secondGroupPhotoUrls);
    }

    [Fact]
    public void RelationshipFlagsCorrectUnderPopularSort() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedGroupId = CreateActiveGroup(requesterUserAccountId, "Owned Group", true);
        AddPendingMember(ownedGroupId, SeedUser("Pending Applicant", null));
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Owner Joined", null), "Joined Group", true);
        AddActiveMember(joinedGroupId, requesterUserAccountId);
        Guid pendingGroupId = CreateActiveGroup(SeedUser("Owner Pending", null), "Pending Group", false);
        AddPendingMember(pendingGroupId, requesterUserAccountId);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Popular", null);

        JsonElement ownedGroup = GetGroup(root, ownedGroupId);
        Assert.True(ownedGroup.GetProperty("owner").GetBoolean());
        Assert.True(ownedGroup.GetProperty("joined").GetBoolean());
        Assert.False(ownedGroup.GetProperty("joinRequest").GetBoolean());
        Assert.True(ownedGroup.GetProperty("pendingMembers").GetBoolean());
        JsonElement joinedGroup = GetGroup(root, joinedGroupId);
        Assert.False(joinedGroup.GetProperty("owner").GetBoolean());
        Assert.True(joinedGroup.GetProperty("joined").GetBoolean());
        Assert.False(joinedGroup.GetProperty("joinRequest").GetBoolean());
        JsonElement pendingGroup = GetGroup(root, pendingGroupId);
        Assert.False(pendingGroup.GetProperty("owner").GetBoolean());
        Assert.False(pendingGroup.GetProperty("joined").GetBoolean());
        Assert.True(pendingGroup.GetProperty("joinRequest").GetBoolean());
    }

    [Fact]
    public void PrivateFilterIncludesPendingRequestPrivateGroup() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid privateGroupId = CreateActiveGroup(SeedUser("Owner", null), "Private Pending Group", false);
        AddPendingMember(privateGroupId, requesterUserAccountId);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Private", null);

        Assert.True(ContainsGroup(root, privateGroupId));
        JsonElement privateGroup = GetGroup(root, privateGroupId);
        Assert.True(privateGroup.GetProperty("joinRequest").GetBoolean());
        Assert.False(privateGroup.GetProperty("joined").GetBoolean());
    }

    [Fact]
    public void SearchTrimsSurroundingWhitespaceBeforeMatching() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid groupId = CreateActiveGroup(requesterUserAccountId, "Anxiety Support", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "   Anxiety   ");

        Assert.Equal(1, root.GetArrayLength());
        Assert.True(ContainsGroup(root, groupId));
    }

    [Fact]
    public void SearchTreatsOpenBracketAsLiteral() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid literalGroupId = CreateActiveGroup(requesterUserAccountId, "Group [A]", true);
        Guid wildcardTrapGroupId = CreateActiveGroup(requesterUserAccountId, "Group A", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "[A]");

        Assert.True(ContainsGroup(root, literalGroupId));
        Assert.False(ContainsGroup(root, wildcardTrapGroupId));
    }

    [Fact]
    public void SearchPreservesRelationshipOrderUnderDefaultSort() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid ownedMatchId = CreateActiveGroup(requesterUserAccountId, "Anxiety Owned", true);
        Guid discoveryMatchId = CreateActiveGroup(SeedUser("Other Owner", null), "Anxiety Discovery", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, null, "Anxiety");

        Assert.Equal(2, root.GetArrayLength());
        Assert.True(IndexOfGroup(root, ownedMatchId) < IndexOfGroup(root, discoveryMatchId));
    }

    [Fact]
    public void PublicOrdersJoinedGroupBeforeDiscovery() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid requesterUserAccountId = ResolveUserAccountId(authToken);
        Guid joinedPublicGroupId = CreateActiveGroup(SeedUser("Owner Joined", null), "Joined Public", true);
        AddActiveMember(joinedPublicGroupId, requesterUserAccountId);
        Guid discoveryPublicGroupId = CreateActiveGroup(SeedUser("Owner Discovery", null), "Discovery Public", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "Public", null);

        JsonElement joinedGroup = GetGroup(root, joinedPublicGroupId);
        Assert.False(joinedGroup.GetProperty("owner").GetBoolean());
        Assert.True(joinedGroup.GetProperty("joined").GetBoolean());
        Assert.True(IndexOfGroup(root, joinedPublicGroupId) < IndexOfGroup(root, discoveryPublicGroupId));
    }

    [Fact]
    public void SortModeIsCaseInsensitiveAndTrimmed() {
        using var testingMockProvidersContainer = new TestingMockProvidersContainer();
        string authToken = CreateUser(testingMockProvidersContainer, "Requester");
        Guid largeGroupId = CreateActiveGroup(SeedUser("Owner Large", null), "Large", true);
        AddActiveMembers(largeGroupId, 2);
        Guid smallGroupId = CreateActiveGroup(SeedUser("Owner Small", null), "Small", true);

        JsonElement root = List(testingMockProvidersContainer, authToken, "  pOpUlAr  ", null);

        Assert.True(IndexOfGroup(root, largeGroupId) < IndexOfGroup(root, smallGroupId));
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer testingMockProvidersContainer, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(testingMockProvidersContainer, name + " " + Guid.NewGuid());
    }

    private static JsonElement List(TestingMockProvidersContainer testingMockProvidersContainer, string authToken, string sortBy, string search) {
        return testingMockProvidersContainer.WebClient.PostJson("api/chatGroup/list", new { AuthToken = authToken, SortBy = sortBy, Search = search }).ReadContentAsJsonDocument().RootElement.Clone();
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

    private static Guid CreateOwnerlessPublicGroup(string name) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = null, IsPublic = true, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
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

    private static void AddActiveMembers(Guid groupId, int count) {
        for (int index = 0; index < count; index++)
            AddActiveMember(groupId, SeedUser("Member " + Guid.NewGuid(), null));
    }

    private static void AddPendingMembers(Guid groupId, int count) {
        for (int index = 0; index < count; index++)
            AddPendingMember(groupId, SeedUser("Pending " + Guid.NewGuid(), null));
    }

    private static void SetGroupCreatedAt(Guid groupId, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == groupId);
        chatGroup.CreatedAtUtc = createdAtUtc;
        dbContext.SaveChanges();
    }

    private static void SetGroupLastSeenAt(Guid groupId, DateTime lastSeenAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.Single(field => field.Id == groupId);
        chatGroup.LastSeenAtUtc = lastSeenAtUtc;
        dbContext.SaveChanges();
    }

    // Helpers - Reading

    private static bool ContainsGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return true;
        return false;
    }

    private static JsonElement GetGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        foreach (JsonElement element in root.EnumerateArray())
            if (element.GetProperty("id").GetString() == target)
                return element;
        throw new InvalidOperationException("Chat group was not present in the response.");
    }

    private static int IndexOfGroup(JsonElement root, Guid groupId) {
        string target = groupId.ToString();
        int index = 0;
        foreach (JsonElement element in root.EnumerateArray()) {
            if (element.GetProperty("id").GetString() == target)
                return index;
            index++;
        }
        throw new InvalidOperationException("Chat group was not present in the response.");
    }

    private static List<string> HelperPhotoUrls(JsonElement group) {
        List<string> photoUrls = [];
        foreach (JsonElement helper in group.GetProperty("helpers").EnumerateArray())
            photoUrls.Add(helper.GetProperty("profilePhotoUrl").GetString());
        return photoUrls;
    }
}
