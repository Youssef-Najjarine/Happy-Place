using System.Net;
using System.Text.Json;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

[Collection("Integration")]
public class ListChatGroupsPageTest {
    // Fields

    private static readonly int PageSize = 50;

    // Tests - Authentication Failures

    [Fact]
    public void ListPageEmptyTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/chatGroup/listPage", new { AuthToken = "" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ListPageInvalidTokenReturnsUnauthorized() {
        using var container = new TestingMockProvidersContainer();

        HttpResponseMessage response = container.WebClient.PostJson("api/chatGroup/listPage", new { AuthToken = "not-a-real-token-at-all" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Tests - Page Shape And Size

    [Fact]
    public void ResponseContainsExactlyItemsAndNextCursor() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(3);

        JsonElement root = ListPage(container, callerAuthToken, null);
        List<string> actualProperties = [.. root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)];
        List<string> expectedProperties = ["items", "nextCursor"];

        Assert.Equal(expectedProperties, actualProperties);
    }

    [Fact]
    public void FeedSmallerThanPageReturnsAllWithNullCursor() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(5);

        JsonElement root = ListPage(container, callerAuthToken, null);

        Assert.Equal(5, root.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public void FirstPageReturnsAtMostPageSizeItemsWithACursor() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(PageSize + 10);

        JsonElement root = ListPage(container, callerAuthToken, null);

        Assert.Equal(PageSize, root.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.String, root.GetProperty("nextCursor").ValueKind);
    }

    [Fact]
    public void LastPageReturnsNullCursor() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(PageSize + 10);
        JsonElement firstPage = ListPage(container, callerAuthToken, null);
        string cursor = firstPage.GetProperty("nextCursor").GetString();

        JsonElement secondPage = ListPage(container, callerAuthToken, cursor);

        Assert.Equal(10, secondPage.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, secondPage.GetProperty("nextCursor").ValueKind);
    }

    // Tests - Walking The Cursor

    [Fact]
    public void WalkingTheCursorCoversEveryGroupExactlyOnce() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        HashSet<string> seededIds = SeedDiscoveryGroups(PageSize * 2 + 20);

        List<string> walkedIds = WalkAllPages(container, callerAuthToken, null, null);

        Assert.Equal(seededIds.Count, walkedIds.Count);
        Assert.Equal(seededIds, [.. walkedIds]);
    }

    [Fact]
    public void OrderIsStableAcrossPageBoundaries() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(PageSize + 20);

        JsonElement firstPage = ListPage(container, callerAuthToken, null);
        string cursor = firstPage.GetProperty("nextCursor").GetString();
        JsonElement secondPage = ListPage(container, callerAuthToken, cursor);
        DateTime lastOfFirstPage = GetCreatedAtUtc(LastItemId(firstPage));
        DateTime firstOfSecondPage = GetCreatedAtUtc(FirstItemId(secondPage));

        Assert.True(firstOfSecondPage <= lastOfFirstPage);
    }

    [Fact]
    public void DeletingTheCursorAnchorStillContinuesTheWalk() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        HashSet<string> seededIds = SeedDiscoveryGroups(PageSize + 10);
        JsonElement firstPage = ListPage(container, callerAuthToken, null);
        string cursor = firstPage.GetProperty("nextCursor").GetString();
        string anchorGroupId = LastItemId(firstPage);
        DeleteGroupRow(Guid.Parse(anchorGroupId));

        JsonElement secondPage = ListPage(container, callerAuthToken, cursor);

        List<string> secondPageIds = [.. secondPage.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetString())];
        Assert.Equal(10, secondPageIds.Count);
        Assert.DoesNotContain(anchorGroupId, secondPageIds);
        List<string> firstPageIds = [.. firstPage.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetString())];
        Assert.Empty(firstPageIds.Intersect(secondPageIds));
    }

    [Fact]
    public void GarbageCursorFallsBackToTheFirstPage() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        SeedDiscoveryGroups(PageSize + 10);
        JsonElement firstPage = ListPage(container, callerAuthToken, null);

        JsonElement garbagePage = ListPage(container, callerAuthToken, "definitely-not-a-real-cursor");

        Assert.Equal(FirstItemId(firstPage), FirstItemId(garbagePage));
        Assert.Equal(PageSize, garbagePage.GetProperty("items").GetArrayLength());
    }

    // Tests - The Callers Groups Lead The Feed

    [Fact]
    public void CallersOwnJoinedAndRequestedGroupsAppearOnTheFirstPage() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        Guid callerUserAccountId = ResolveUserAccountId(callerAuthToken);
        SeedDiscoveryGroups(PageSize + 30);
        Guid ownedGroupId = CreateActiveGroup(callerUserAccountId, "My Own Group", false, DateTime.UtcNow.AddDays(-30));
        Guid joinedGroupId = CreateActiveGroup(SeedUser("Other Owner", null), "Joined Group", true, DateTime.UtcNow.AddDays(-31));
        AddActiveMember(joinedGroupId, callerUserAccountId);
        Guid requestedGroupId = CreateActiveGroup(SeedUser("Private Owner", null), "Requested Group", false, DateTime.UtcNow.AddDays(-32));
        AddPendingMember(requestedGroupId, callerUserAccountId);

        JsonElement firstPage = ListPage(container, callerAuthToken, null);

        List<string> firstPageIds = [.. firstPage.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetString())];
        Assert.Contains(ownedGroupId.ToString(), firstPageIds);
        Assert.Contains(joinedGroupId.ToString(), firstPageIds);
        Assert.Contains(requestedGroupId.ToString(), firstPageIds);
    }

    [Fact]
    public void PageItemsCarryTheSameFlagsAsTheUnpagedList() {
        using var container = new TestingMockProvidersContainer();
        string ownerAuthToken = CreateUser(container, "Owner");
        Guid groupId = CreateActiveGroup(ResolveUserAccountId(ownerAuthToken), "Flagged Group", false, DateTime.UtcNow);
        string requesterAuthToken = CreateUser(container, "Requester");
        container.WebClient.PostJson("api/chatGroup/requestToJoin", new { AuthToken = requesterAuthToken, ChatGroupId = groupId }).EnsureSuccessStatusCode();

        JsonElement ownerItem = FindItem(ListPage(container, ownerAuthToken, null), groupId);
        JsonElement requesterItem = FindItem(ListPage(container, requesterAuthToken, null), groupId);

        Assert.True(ownerItem.GetProperty("owner").GetBoolean());
        Assert.True(ownerItem.GetProperty("pendingMembers").GetBoolean());
        Assert.True(requesterItem.GetProperty("joinRequest").GetBoolean());
        Assert.False(requesterItem.GetProperty("joined").GetBoolean());
    }

    // Tests - Search And Sort Across Pages

    [Fact]
    public void SearchAppliesAcrossEveryPage() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        HashSet<string> matchingIds = SeedDiscoveryGroups(PageSize + 10, "Anxiety Circle");
        SeedDiscoveryGroups(PageSize, "Totally Unrelated");

        List<string> walkedIds = WalkAllPages(container, callerAuthToken, null, "Anxiety");

        Assert.Equal(matchingIds, [.. walkedIds]);
    }

    [Fact]
    public void PopularSortIsNonIncreasingAcrossTheWholeWalk() {
        using var container = new TestingMockProvidersContainer();
        string callerAuthToken = CreateUser(container, "Caller");
        List<Guid> groupIds = [.. SeedDiscoveryGroups(PageSize + 10).Select(Guid.Parse)];
        AddActiveMember(groupIds[3], SeedUser("Member A", null));
        AddActiveMember(groupIds[3], SeedUser("Member B", null));
        AddActiveMember(groupIds[7], SeedUser("Member C", null));

        List<string> walkedIds = WalkAllPages(container, callerAuthToken, "Popular", null);

        List<int> memberCounts = [.. walkedIds.Select(id => CountActiveMembers(Guid.Parse(id)))];
        for (int index = 1; index < memberCounts.Count; index++)
            Assert.True(memberCounts[index] <= memberCounts[index - 1]);
    }

    // Helpers - Acting

    private static string CreateUser(TestingMockProvidersContainer container, string name) {
        return TestUserFactory.CreateVerifiedEmailUser(container, name + " " + Guid.NewGuid());
    }

    private static JsonElement ListPage(TestingMockProvidersContainer container, string authToken, string cursor, string sortBy = null, string search = null) {
        var body = new Dictionary<string, object> { ["AuthToken"] = authToken };
        if (cursor != null) body["Cursor"] = cursor;
        if (sortBy != null) body["SortBy"] = sortBy;
        if (search != null) body["Search"] = search;
        return container.WebClient.PostJson("api/chatGroup/listPage", body).ReadContentAsJsonDocument().RootElement.Clone();
    }

    private static List<string> WalkAllPages(TestingMockProvidersContainer container, string authToken, string sortBy, string search) {
        List<string> walkedIds = [];
        string cursor = null;
        for (int pageCount = 0; pageCount < 10; pageCount++) {
            JsonElement page = ListPage(container, authToken, cursor, sortBy, search);
            foreach (JsonElement item in page.GetProperty("items").EnumerateArray())
                walkedIds.Add(item.GetProperty("id").GetString());
            if (page.GetProperty("nextCursor").ValueKind == JsonValueKind.Null)
                return walkedIds;
            cursor = page.GetProperty("nextCursor").GetString();
        }
        throw new InvalidOperationException("Cursor walk did not terminate.");
    }

    private static string FirstItemId(JsonElement page) {
        return page.GetProperty("items")[0].GetProperty("id").GetString();
    }

    private static string LastItemId(JsonElement page) {
        JsonElement items = page.GetProperty("items");
        return items[items.GetArrayLength() - 1].GetProperty("id").GetString();
    }

    private static JsonElement FindItem(JsonElement page, Guid chatGroupId) {
        string target = chatGroupId.ToString();
        foreach (JsonElement item in page.GetProperty("items").EnumerateArray())
            if (item.GetProperty("id").GetString() == target)
                return item;
        throw new InvalidOperationException("Chat group was not present on the page.");
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

    private static HashSet<string> SeedDiscoveryGroups(int count, string namePrefix = "Discovery Group") {
        HashSet<string> seededIds = [];
        Guid ownerUserAccountId = SeedUser("Discovery Owner", null);
        DateTime baseCreatedAtUtc = DateTime.UtcNow.AddDays(-1);
        for (int index = 0; index < count; index++)
            seededIds.Add(CreateActiveGroup(ownerUserAccountId, $"{namePrefix} {index} {Guid.NewGuid()}", true, baseCreatedAtUtc.AddSeconds(index)).ToString());
        return seededIds;
    }

    private static Guid CreateActiveGroup(Guid ownerUserAccountId, string name, bool isPublic, DateTime createdAtUtc) {
        using var dbContext = HappyPlaceDbContext.Create();
        Guid groupId = Guid.NewGuid();
        dbContext.ChatGroups.Add(new ChatGroup { Id = groupId, Name = name, OwnerUserAccountId = ownerUserAccountId, IsPublic = isPublic, Status = ChatGroupStatus.Active, CreatedAtUtc = createdAtUtc, LastSeenAtUtc = createdAtUtc });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = groupId, UserAccountId = ownerUserAccountId, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = createdAtUtc });
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

    private static void DeleteGroupRow(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        dbContext.ChatGroups.Where(field => field.Id == groupId).ExecuteDelete();
    }

    // Helpers - Reading

    private static DateTime GetCreatedAtUtc(string groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroups.Single(field => field.Id == Guid.Parse(groupId)).CreatedAtUtc;
    }

    private static int CountActiveMembers(Guid groupId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.ChatGroupMembers.Count(field => field.ChatGroupId == groupId && field.Status == ChatGroupMemberStatus.Active);
    }
}
