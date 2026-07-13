using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public class FriendshipManager {
    // Fields

    private static readonly int MaxFriendRequestsPerHour = 20;
    private static readonly int MaxFriendRequestsPerDayToSamePerson = 3;
    private static readonly int FriendsPageSize = 30;
    private static readonly int FriendListSearchLimit = 50;
    private static readonly int RequestListLimit = 200;
    private static readonly int UserSearchResultLimit = 20;
    private static readonly byte FriendsFeedCursorMarker = 50;

    // Methods - Requests

    public static FriendRequestSendResult SendRequest(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return FriendRequestSendResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null || target.Id == caller.Id)
            return FriendRequestSendResult.None();
        if (IsBlockedEitherDirection(dbContext, caller.Id, target.Id))
            return FriendRequestSendResult.None();
        var existingFriendship = FindFriendshipBetween(dbContext, caller.Id, target.Id);
        if (existingFriendship != null)
            return ResolveSendAgainstExistingFriendship(dbContext, existingFriendship, caller);
        if (IsRateLimited(dbContext, caller.Id, target.Id))
            return FriendRequestSendResult.RateLimited();
        DateTime requestedAtUtc = DateTime.UtcNow;
        dbContext.Friendships.Add(new() { Id = Guid.NewGuid(), RequesterUserAccountId = caller.Id, AddresseeUserAccountId = target.Id, Status = FriendshipStatus.Pending, CreatedAtUtc = requestedAtUtc });
        dbContext.FriendRequestAudits.Add(new() { Id = Guid.NewGuid(), RequesterUserAccountId = caller.Id, AddresseeUserAccountId = target.Id, RequestedAtUtc = requestedAtUtc });
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
            return ResolveConcurrentSend(caller.Id, target.Id);
        }
        NotificationDispatchManager.MarkFriendRequestsDirty(target.Id);
        return FriendRequestSendResult.Requested();
    }

    public static FriendRequestCancelResult CancelRequest(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return FriendRequestCancelResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null)
            return FriendRequestCancelResult.None();
        int deletedCount = dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == caller.Id && field.AddresseeUserAccountId == target.Id && field.Status == FriendshipStatus.Pending)
            .ExecuteDelete();
        if (deletedCount == 0)
            return FriendRequestCancelResult.None();
        NotificationDispatchManager.MarkFriendRequestsDirty(target.Id);
        return FriendRequestCancelResult.Canceled();
    }

    // Methods - Responses

    public static FriendRequestAcceptResult AcceptRequest(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return FriendRequestAcceptResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null)
            return FriendRequestAcceptResult.None();
        DateTime respondedAtUtc = DateTime.UtcNow;
        int acceptedCount = dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == target.Id && field.AddresseeUserAccountId == caller.Id && field.Status == FriendshipStatus.Pending)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, FriendshipStatus.Accepted).SetProperty(field => field.RespondedAtUtc, respondedAtUtc));
        if (acceptedCount > 0) {
            NotificationDispatchManager.MarkFriendRequestsDirty(caller.Id);
            NotificationDispatchManager.SendFriendRequestAcceptedPush(target.Id, caller.Id, caller.DisplayName, caller.Username);
            return FriendRequestAcceptResult.Accepted();
        }
        var existingFriendship = FindFriendshipBetween(dbContext, caller.Id, target.Id);
        if (existingFriendship != null && existingFriendship.Status == FriendshipStatus.Accepted)
            return FriendRequestAcceptResult.AlreadyFriends();
        return FriendRequestAcceptResult.None();
    }

    public static FriendRequestDeclineResult DeclineRequest(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return FriendRequestDeclineResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null)
            return FriendRequestDeclineResult.None();
        int deletedCount = dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == target.Id && field.AddresseeUserAccountId == caller.Id && field.Status == FriendshipStatus.Pending)
            .ExecuteDelete();
        if (deletedCount == 0)
            return FriendRequestDeclineResult.None();
        NotificationDispatchManager.MarkFriendRequestsDirty(caller.Id);
        return FriendRequestDeclineResult.Declined();
    }

    // Methods - Unfriend

    public static UnfriendResult Unfriend(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return UnfriendResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null)
            return UnfriendResult.None();
        int deletedCount = dbContext.Friendships
            .Where(field => field.Status == FriendshipStatus.Accepted && ((field.RequesterUserAccountId == caller.Id && field.AddresseeUserAccountId == target.Id) || (field.RequesterUserAccountId == target.Id && field.AddresseeUserAccountId == caller.Id)))
            .ExecuteDelete();
        return deletedCount > 0 ? UnfriendResult.Unfriended() : UnfriendResult.None();
    }

    // Methods - Blocking

    public static BlockUserResult Block(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return BlockUserResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null || target.Id == caller.Id)
            return BlockUserResult.None();
        var existingFriendship = FindFriendshipBetween(dbContext, caller.Id, target.Id);
        if (existingFriendship != null)
            dbContext.Friendships.Where(field => field.Id == existingFriendship.Id).ExecuteDelete();
        bool blockAlreadyExists = dbContext.UserBlocks.Any(field => field.BlockerUserAccountId == caller.Id && field.BlockedUserAccountId == target.Id);
        if (!blockAlreadyExists) {
            dbContext.UserBlocks.Add(new() { Id = Guid.NewGuid(), BlockerUserAccountId = caller.Id, BlockedUserAccountId = target.Id, CreatedAtUtc = DateTime.UtcNow });
            try {
                dbContext.SaveChanges();
            }
            catch (DbUpdateException) {
            }
        }
        if (existingFriendship != null && existingFriendship.Status == FriendshipStatus.Pending)
            NotificationDispatchManager.MarkFriendRequestsDirty(existingFriendship.AddresseeUserAccountId);
        return BlockUserResult.Blocked();
    }

    public static UnblockUserResult Unblock(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        if (caller.IsAnonymous)
            return UnblockUserResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        var target = ResolveTargetUserAccount(dbContext, username);
        if (target == null)
            return UnblockUserResult.None();
        int deletedCount = dbContext.UserBlocks
            .Where(field => field.BlockerUserAccountId == caller.Id && field.BlockedUserAccountId == target.Id)
            .ExecuteDelete();
        return deletedCount > 0 ? UnblockUserResult.Unblocked() : UnblockUserResult.None();
    }

    public static UserBlockListResult ListBlocked(string authToken) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        List<UserAccount> blockedUserAccounts = [.. dbContext.UserBlocks
            .Where(field => field.BlockerUserAccountId == caller.Id)
            .Join(dbContext.UserAccounts, userBlock => userBlock.BlockedUserAccountId, userAccount => userAccount.Id, (userBlock, userAccount) => new { userBlock.CreatedAtUtc, UserAccount = userAccount })
            .OrderByDescending(field => field.CreatedAtUtc)
            .Select(field => field.UserAccount)];
        return new UserBlockListResult([.. blockedUserAccounts.Select(UserProfileSummaryResult.FromUserAccount)]);
    }

    public static bool IsBlockedEitherDirection(Guid firstUserAccountId, Guid secondUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return IsBlockedEitherDirection(dbContext, firstUserAccountId, secondUserAccountId);
    }

    // Methods - Lists

    public static FriendListPageResult ListFriends(string authToken, string username, string search, string cursor) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount owner;
        if (string.IsNullOrWhiteSpace(username)) {
            owner = caller;
        }
        else {
            owner = ResolveTargetUserAccount(dbContext, username);
            if (owner == null)
                return FriendListPageResult.NotFound();
            if (owner.Id != caller.Id && IsBlockedEitherDirection(dbContext, caller.Id, owner.Id))
                return FriendListPageResult.NotFound();
        }
        Guid ownerUserAccountId = owner.Id;
        var acceptedFriendships = dbContext.Friendships
            .Where(field => field.Status == FriendshipStatus.Accepted && (field.RequesterUserAccountId == ownerUserAccountId || field.AddresseeUserAccountId == ownerUserAccountId));
        int totalCount = acceptedFriendships.Count();
        var friendRows = acceptedFriendships
            .Select(friendship => new { FriendshipId = friendship.Id, friendship.RespondedAtUtc, FriendUserAccountId = friendship.RequesterUserAccountId == ownerUserAccountId ? friendship.AddresseeUserAccountId : friendship.RequesterUserAccountId })
            .Join(dbContext.UserAccounts, entry => entry.FriendUserAccountId, userAccount => userAccount.Id, (entry, userAccount) => new { entry.FriendshipId, entry.RespondedAtUtc, UserAccount = userAccount });
        if (owner.Id != caller.Id) {
            Guid callerUserAccountId = caller.Id;
            var callerBlockRelatedIds = dbContext.UserBlocks
                .Where(field => field.BlockerUserAccountId == callerUserAccountId || field.BlockedUserAccountId == callerUserAccountId)
                .Select(field => field.BlockerUserAccountId == callerUserAccountId ? field.BlockedUserAccountId : field.BlockerUserAccountId);
            friendRows = friendRows.Where(entry => !callerBlockRelatedIds.Contains(entry.UserAccount.Id));
        }
        string trimmedSearch = (search ?? "").Trim();
        if (trimmedSearch.Length > 0) {
            string loweredSearch = trimmedSearch.ToLowerInvariant();
            List<FriendPageRow> searchRows = [.. friendRows
                .Where(entry => entry.UserAccount.Username.StartsWith(loweredSearch) || entry.UserAccount.DisplayName.Contains(trimmedSearch))
                .OrderByDescending(entry => entry.RespondedAtUtc)
                .ThenByDescending(entry => entry.FriendshipId)
                .Take(FriendListSearchLimit)
                .Select(entry => new FriendPageRow(entry.FriendshipId, (DateTime)entry.RespondedAtUtc, entry.UserAccount))];
            return FriendListPageResult.Ok(totalCount, BuildEntriesWithStatuses(dbContext, caller.Id, [.. searchRows.Select(row => row.UserAccount)]), null);
        }
        if (!string.IsNullOrWhiteSpace(cursor)) {
            if (!CursorCodec.TryDecodeFeedCursor(cursor, FriendsFeedCursorMarker, out long friendedTicks, out _, out Guid anchorFriendshipId))
                return FriendListPageResult.Ok(totalCount, [], null);
            DateTime friendedBefore = new(friendedTicks, DateTimeKind.Utc);
            friendRows = friendRows.Where(entry => entry.RespondedAtUtc < friendedBefore || (entry.RespondedAtUtc == friendedBefore && entry.FriendshipId < anchorFriendshipId));
        }
        List<FriendPageRow> pageRows = [.. friendRows
            .OrderByDescending(entry => entry.RespondedAtUtc)
            .ThenByDescending(entry => entry.FriendshipId)
            .Take(FriendsPageSize + 1)
            .Select(entry => new FriendPageRow(entry.FriendshipId, (DateTime)entry.RespondedAtUtc, entry.UserAccount))];
        string nextCursor = null;
        if (pageRows.Count > FriendsPageSize) {
            pageRows.RemoveAt(FriendsPageSize);
            FriendPageRow lastRow = pageRows[FriendsPageSize - 1];
            nextCursor = CursorCodec.EncodeFeedCursor(FriendsFeedCursorMarker, lastRow.FriendedAtUtc.Ticks, 0, lastRow.FriendshipId);
        }
        return FriendListPageResult.Ok(totalCount, BuildEntriesWithStatuses(dbContext, caller.Id, [.. pageRows.Select(row => row.UserAccount)]), nextCursor);
    }

    public static FriendRequestListResult ListIncomingRequests(string authToken) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        List<RequestRow> requestRows = [.. dbContext.Friendships
            .Where(field => field.AddresseeUserAccountId == caller.Id && field.Status == FriendshipStatus.Pending)
            .Join(dbContext.UserAccounts, friendship => friendship.RequesterUserAccountId, userAccount => userAccount.Id, (friendship, userAccount) => new { friendship.Id, friendship.CreatedAtUtc, UserAccount = userAccount })
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Take(RequestListLimit)
            .Select(entry => new RequestRow(entry.CreatedAtUtc, entry.UserAccount))];
        return new FriendRequestListResult([.. requestRows.Select(row => FriendRequestEntry.FromUserAccount(row.UserAccount, row.RequestedAtUtc))]);
    }

    public static FriendRequestListResult ListOutgoingRequests(string authToken) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        List<RequestRow> requestRows = [.. dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == caller.Id && field.Status == FriendshipStatus.Pending)
            .Join(dbContext.UserAccounts, friendship => friendship.AddresseeUserAccountId, userAccount => userAccount.Id, (friendship, userAccount) => new { friendship.Id, friendship.CreatedAtUtc, UserAccount = userAccount })
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Take(RequestListLimit)
            .Select(entry => new RequestRow(entry.CreatedAtUtc, entry.UserAccount))];
        return new FriendRequestListResult([.. requestRows.Select(row => FriendRequestEntry.FromUserAccount(row.UserAccount, row.RequestedAtUtc))]);
    }

    // Methods - Search

    public static UserSearchResult SearchUsers(string authToken, string query) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return null;
        using var dbContext = HappyPlaceDbContext.Create();
        Guid callerUserAccountId = caller.Id;
        List<Guid> blockRelatedIds = [.. dbContext.UserBlocks
            .Where(field => field.BlockerUserAccountId == callerUserAccountId || field.BlockedUserAccountId == callerUserAccountId)
            .Select(field => field.BlockerUserAccountId == callerUserAccountId ? field.BlockedUserAccountId : field.BlockerUserAccountId)];
        string trimmedQuery = (query ?? "").Trim();
        if (trimmedQuery.Length == 0)
            return BuildSuggestions(dbContext, caller, blockRelatedIds);
        string loweredQuery = trimmedQuery.ToLowerInvariant();
        List<UserAccount> matchedAccounts = [.. dbContext.UserAccounts
            .Where(field => !field.IsAnonymous && field.Username != null && field.Id != callerUserAccountId && !blockRelatedIds.Contains(field.Id) && (field.Username.StartsWith(loweredQuery) || field.DisplayName.Contains(trimmedQuery)))
            .OrderByDescending(field => field.Username.StartsWith(loweredQuery))
            .ThenBy(field => field.Username)
            .Take(UserSearchResultLimit)];
        return new UserSearchResult(BuildEntriesWithStatuses(dbContext, caller.Id, matchedAccounts));
    }

    // Methods - Status

    public static string ComputeFriendshipStatus(Guid callerUserAccountId, Guid targetUserAccountId) {
        if (callerUserAccountId == targetUserAccountId)
            return "self";
        using var dbContext = HappyPlaceDbContext.Create();
        return ResolveFriendshipStatusFromFriendship(callerUserAccountId, FindFriendshipBetween(dbContext, callerUserAccountId, targetUserAccountId));
    }

    public static int CountFriends(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        return dbContext.Friendships.Count(field => field.Status == FriendshipStatus.Accepted && (field.RequesterUserAccountId == userAccountId || field.AddresseeUserAccountId == userAccountId));
    }

    // Methods - Account Deletion

    public static void UntangleUserForAccountDeletion(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> pendingAddresseeUserAccountIds = [.. dbContext.Friendships.Where(field => field.RequesterUserAccountId == userAccountId && field.Status == FriendshipStatus.Pending).Select(field => field.AddresseeUserAccountId)];
        dbContext.Friendships.Where(field => field.RequesterUserAccountId == userAccountId || field.AddresseeUserAccountId == userAccountId).ExecuteDelete();
        dbContext.UserBlocks.Where(field => field.BlockerUserAccountId == userAccountId || field.BlockedUserAccountId == userAccountId).ExecuteDelete();
        dbContext.FriendRequestAudits.Where(field => field.RequesterUserAccountId == userAccountId || field.AddresseeUserAccountId == userAccountId).ExecuteDelete();
        NotificationDispatchManager.RemoveFriendRequestsChannel(userAccountId);
        foreach (Guid pendingAddresseeUserAccountId in pendingAddresseeUserAccountIds)
            NotificationDispatchManager.MarkFriendRequestsDirty(pendingAddresseeUserAccountId);
    }

    // Methods - Private

    private static UserAccount ResolveTargetUserAccount(HappyPlaceDbContext dbContext, string username) {
        string normalizedUsername = (username ?? "").Trim().ToLowerInvariant();
        if (normalizedUsername.Length == 0)
            return null;
        var target = dbContext.UserAccounts.SingleOrDefault(field => field.Username == normalizedUsername);
        if (target == null || target.IsAnonymous)
            return null;
        return target;
    }

    private static Friendship FindFriendshipBetween(HappyPlaceDbContext dbContext, Guid firstUserAccountId, Guid secondUserAccountId) {
        return dbContext.Friendships.SingleOrDefault(field => (field.RequesterUserAccountId == firstUserAccountId && field.AddresseeUserAccountId == secondUserAccountId) || (field.RequesterUserAccountId == secondUserAccountId && field.AddresseeUserAccountId == firstUserAccountId));
    }

    private static bool IsBlockedEitherDirection(HappyPlaceDbContext dbContext, Guid firstUserAccountId, Guid secondUserAccountId) {
        return dbContext.UserBlocks.Any(field => (field.BlockerUserAccountId == firstUserAccountId && field.BlockedUserAccountId == secondUserAccountId) || (field.BlockerUserAccountId == secondUserAccountId && field.BlockedUserAccountId == firstUserAccountId));
    }

    private static string ResolveFriendshipStatusFromFriendship(Guid callerUserAccountId, Friendship friendship) {
        if (friendship == null)
            return "none";
        if (friendship.Status == FriendshipStatus.Accepted)
            return "friends";
        return friendship.RequesterUserAccountId == callerUserAccountId ? "requestSent" : "requestReceived";
    }

    private static List<FriendListEntry> BuildEntriesWithStatuses(HappyPlaceDbContext dbContext, Guid callerUserAccountId, List<UserAccount> userAccounts) {
        List<Guid> userAccountIds = [.. userAccounts.Select(field => field.Id)];
        List<Friendship> callerFriendships = [.. dbContext.Friendships
            .Where(field => (field.RequesterUserAccountId == callerUserAccountId && userAccountIds.Contains(field.AddresseeUserAccountId)) || (field.AddresseeUserAccountId == callerUserAccountId && userAccountIds.Contains(field.RequesterUserAccountId)))];
        return [.. userAccounts.Select(field => FriendListEntry.FromUserAccount(field, ResolveStatusForLoadedUser(callerUserAccountId, field.Id, callerFriendships)))];
    }

    private static string ResolveStatusForLoadedUser(Guid callerUserAccountId, Guid targetUserAccountId, List<Friendship> callerFriendships) {
        if (callerUserAccountId == targetUserAccountId)
            return "self";
        Friendship friendship = callerFriendships.SingleOrDefault(field => field.RequesterUserAccountId == targetUserAccountId || field.AddresseeUserAccountId == targetUserAccountId);
        return ResolveFriendshipStatusFromFriendship(callerUserAccountId, friendship);
    }

    private static UserSearchResult BuildSuggestions(HappyPlaceDbContext dbContext, UserAccount caller, List<Guid> blockRelatedIds) {
        Guid callerUserAccountId = caller.Id;
        List<Guid> callerActiveGroupIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == callerUserAccountId && field.Status == ChatGroupMemberStatus.Active)
            .Join(dbContext.ChatGroups.Where(chatGroup => chatGroup.Status == ChatGroupStatus.Active), member => member.ChatGroupId, chatGroup => chatGroup.Id, (member, chatGroup) => chatGroup.Id)];
        var coMemberIds = dbContext.ChatGroupMembers
            .Where(field => callerActiveGroupIds.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Active && field.UserAccountId != callerUserAccountId)
            .Select(field => field.UserAccountId)
            .Distinct();
        var relatedIds = dbContext.Friendships
            .Where(field => field.RequesterUserAccountId == callerUserAccountId || field.AddresseeUserAccountId == callerUserAccountId)
            .Select(field => field.RequesterUserAccountId == callerUserAccountId ? field.AddresseeUserAccountId : field.RequesterUserAccountId);
        List<UserAccount> suggestionAccounts = [.. dbContext.UserAccounts
            .Where(field => coMemberIds.Contains(field.Id) && !field.IsAnonymous && field.Username != null && !blockRelatedIds.Contains(field.Id) && !relatedIds.Contains(field.Id))
            .OrderBy(field => field.DisplayName)
            .ThenBy(field => field.Id)
            .Take(UserSearchResultLimit)];
        return new UserSearchResult(BuildEntriesWithStatuses(dbContext, callerUserAccountId, suggestionAccounts));
    }

    private static FriendRequestSendResult ResolveSendAgainstExistingFriendship(HappyPlaceDbContext dbContext, Friendship existingFriendship, UserAccount caller) {
        if (existingFriendship.Status == FriendshipStatus.Accepted)
            return FriendRequestSendResult.AlreadyFriends();
        if (existingFriendship.RequesterUserAccountId == caller.Id)
            return FriendRequestSendResult.AlreadyRequested();
        DateTime respondedAtUtc = DateTime.UtcNow;
        int acceptedCount = dbContext.Friendships
            .Where(field => field.Id == existingFriendship.Id && field.Status == FriendshipStatus.Pending)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, FriendshipStatus.Accepted).SetProperty(field => field.RespondedAtUtc, respondedAtUtc));
        if (acceptedCount > 0) {
            NotificationDispatchManager.MarkFriendRequestsDirty(caller.Id);
            NotificationDispatchManager.SendFriendRequestAcceptedPush(existingFriendship.RequesterUserAccountId, caller.Id, caller.DisplayName, caller.Username);
            return FriendRequestSendResult.Accepted();
        }
        var refreshedFriendship = dbContext.Friendships.SingleOrDefault(field => field.Id == existingFriendship.Id);
        if (refreshedFriendship != null && refreshedFriendship.Status == FriendshipStatus.Accepted)
            return FriendRequestSendResult.AlreadyFriends();
        return FriendRequestSendResult.None();
    }

    private static FriendRequestSendResult ResolveConcurrentSend(Guid callerUserAccountId, Guid targetUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        var caller = dbContext.UserAccounts.SingleOrDefault(field => field.Id == callerUserAccountId);
        if (caller == null)
            return FriendRequestSendResult.None();
        var existingFriendship = FindFriendshipBetween(dbContext, callerUserAccountId, targetUserAccountId);
        if (existingFriendship == null)
            return FriendRequestSendResult.None();
        return ResolveSendAgainstExistingFriendship(dbContext, existingFriendship, caller);
    }

    private static bool IsRateLimited(HappyPlaceDbContext dbContext, Guid callerUserAccountId, Guid targetUserAccountId) {
        DateTime oneHourAgo = DateTime.UtcNow.AddHours(-1);
        int recentSendCount = dbContext.FriendRequestAudits.Count(field => field.RequesterUserAccountId == callerUserAccountId && field.RequestedAtUtc > oneHourAgo);
        if (recentSendCount >= MaxFriendRequestsPerHour)
            return true;
        DateTime oneDayAgo = DateTime.UtcNow.AddDays(-1);
        int recentSendsToTarget = dbContext.FriendRequestAudits.Count(field => field.RequesterUserAccountId == callerUserAccountId && field.AddresseeUserAccountId == targetUserAccountId && field.RequestedAtUtc > oneDayAgo);
        return recentSendsToTarget >= MaxFriendRequestsPerDayToSamePerson;
    }

    // Records

    private record FriendPageRow(Guid FriendshipId, DateTime FriendedAtUtc, UserAccount UserAccount);

    private record RequestRow(DateTime RequestedAtUtc, UserAccount UserAccount);
}
