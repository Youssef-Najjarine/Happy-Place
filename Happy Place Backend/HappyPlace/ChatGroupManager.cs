using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class ChatGroupManager {
    // Fields

    private static readonly int MaxHelperAvatars = 5;
    private static readonly int MaxAvailableHelpers = 50;
    private static readonly int MaxChatGroupNameLength = 100;
    private static readonly int MaxOwnerLeaveAttempts = 5;
    private static readonly int FeedPageSize = 50;
    private static readonly byte DefaultFeedCursorMarker = 1;
    private static readonly byte PopularFeedCursorMarker = 2;
    private static readonly byte MostActiveFeedCursorMarker = 3;

    // Methods - Reads

    public static List<ChatGroupSummaryResult> ListForUser(string authToken, string sortBy, string search) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return [];
        using var dbContext = HappyPlaceDbContext.Create();

        List<ChatGroupMember> myMemberships = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == userAccountId.Value)];
        HashSet<Guid> activeGroupIds = [.. myMemberships.Where(field => field.Status == ChatGroupMemberStatus.Active).Select(field => field.ChatGroupId)];
        HashSet<Guid> pendingGroupIds = [.. myMemberships.Where(field => field.Status == ChatGroupMemberStatus.Pending).Select(field => field.ChatGroupId)];
        Dictionary<Guid, DateTime> myJoinedAtByGroup = myMemberships.ToDictionary(field => field.ChatGroupId, field => field.JoinedAtUtc);

        ChatGroupSortMode sortMode = ParseSortMode(sortBy);
        IQueryable<ChatGroup> matchingQuery = dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Active);
        if (sortMode == ChatGroupSortMode.Public)
            matchingQuery = matchingQuery.Where(field => field.IsPublic);
        else if (sortMode == ChatGroupSortMode.Private)
            matchingQuery = matchingQuery.Where(field => !field.IsPublic);
        string searchPattern = BuildSearchPattern(search);
        if (searchPattern != null)
            matchingQuery = matchingQuery.Where(field => EF.Functions.Like(field.Name, searchPattern));

        List<ChatGroup> matchingGroups = [.. matchingQuery];
        if (matchingGroups.Count == 0)
            return [];

        List<Guid> matchingGroupIdList = [.. matchingGroups.Select(field => field.Id)];

        Dictionary<Guid, int> memberCounts = dbContext.ChatGroupMembers
            .Where(field => matchingGroupIdList.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Active)
            .GroupBy(field => field.ChatGroupId)
            .Select(group => new { ChatGroupId = group.Key, Count = group.Count() })
            .ToDictionary(row => row.ChatGroupId, row => row.Count);

        HashSet<Guid> groupIdsWithPendingMembers = [.. dbContext.ChatGroupMembers
            .Where(field => matchingGroupIdList.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Pending)
            .Select(field => field.ChatGroupId)
            .Distinct()];

        List<Guid> avatarGroupIds = [.. matchingGroups.Where(field => field.IsPublic || activeGroupIds.Contains(field.Id)).Select(field => field.Id)];
        Dictionary<Guid, List<ChatGroupHelperAvatar>> helpersByGroup = LoadHelperAvatars(dbContext, avatarGroupIds);
        Dictionary<Guid, int> unreadCounts = LoadUnreadCounts(dbContext, userAccountId.Value, matchingGroupIdList);

        List<ChatGroup> orderedGroups = OrderGroups(matchingGroups, sortMode, userAccountId.Value, activeGroupIds, pendingGroupIds, myJoinedAtByGroup, memberCounts);

        List<ChatGroupSummaryResult> results = [];
        foreach (ChatGroup group in orderedGroups) {
            bool owner = group.OwnerUserAccountId == userAccountId.Value;
            bool joined = activeGroupIds.Contains(group.Id);
            bool joinRequest = pendingGroupIds.Contains(group.Id);
            bool pendingMembers = owner && groupIdsWithPendingMembers.Contains(group.Id);
            int memberCount = memberCounts.TryGetValue(group.Id, out int count) ? count : 0;
            List<ChatGroupHelperAvatar> helpers = helpersByGroup.TryGetValue(group.Id, out List<ChatGroupHelperAvatar> avatars) ? avatars : [];
            int unreadCount = unreadCounts.TryGetValue(group.Id, out int unread) ? unread : 0;
            results.Add(new ChatGroupSummaryResult(group.Id.ToString(), group.Name, group.IsPublic, owner, joined, joinRequest, pendingMembers, memberCount, helpers, unreadCount));
        }
        return results;
    }

    public static List<AvailableHelperResult> ListAvailableHelpers(string authToken) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return [];
        using var dbContext = HappyPlaceDbContext.Create();

        Guid callerUserAccountId = userAccountId.Value;
        List<Guid> blockRelatedIds = [.. dbContext.UserBlocks
            .Where(field => field.BlockerUserAccountId == callerUserAccountId || field.BlockedUserAccountId == callerUserAccountId)
            .Select(field => field.BlockerUserAccountId == callerUserAccountId ? field.BlockedUserAccountId : field.BlockerUserAccountId)];

        List<Guid> availableHelperUserAccountIds = [.. dbContext.HelpAvailabilities
            .Where(field => field.IsAvailable && field.HelperUserAccountId != callerUserAccountId && !blockRelatedIds.Contains(field.HelperUserAccountId))
            .OrderByDescending(field => field.LastSeenAtUtc)
            .Select(field => field.HelperUserAccountId)
            .Take(MaxAvailableHelpers)];
        if (availableHelperUserAccountIds.Count == 0)
            return [];

        Dictionary<Guid, UserAccount> usersById = dbContext.UserAccounts
            .Where(field => availableHelperUserAccountIds.Contains(field.Id))
            .ToDictionary(field => field.Id);

        List<AvailableHelperResult> results = [];
        foreach (Guid helperUserAccountId in availableHelperUserAccountIds) {
            if (!usersById.TryGetValue(helperUserAccountId, out UserAccount user))
                continue;
            results.Add(new AvailableHelperResult(user.Id.ToString(), user.DisplayName, user.ProfilePhotoUrl, UserAccountRegistrar.GetAvatarColor(user.Id), user.Username, user.IsAnonymous));
        }
        return results;
    }


    // Methods - Reads (Paged)

    public static ChatGroupPageResult ListPageForUser(string authToken, string sortBy, string search, string cursor) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return new ChatGroupPageResult([], null);
        using var dbContext = HappyPlaceDbContext.Create();

        List<ChatGroupMember> myMemberships = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == userAccountId.Value)];
        HashSet<Guid> activeGroupIds = [.. myMemberships.Where(field => field.Status == ChatGroupMemberStatus.Active).Select(field => field.ChatGroupId)];
        HashSet<Guid> pendingGroupIds = [.. myMemberships.Where(field => field.Status == ChatGroupMemberStatus.Pending).Select(field => field.ChatGroupId)];
        Dictionary<Guid, DateTime> myJoinedAtByGroup = myMemberships.ToDictionary(field => field.ChatGroupId, field => field.JoinedAtUtc);

        ChatGroupSortMode sortMode = ParseSortMode(sortBy);
        string searchPattern = BuildSearchPattern(search);
        IQueryable<ChatGroup> matchingQuery = dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Active);
        if (sortMode == ChatGroupSortMode.Public)
            matchingQuery = matchingQuery.Where(field => field.IsPublic);
        else if (sortMode == ChatGroupSortMode.Private)
            matchingQuery = matchingQuery.Where(field => !field.IsPublic);
        if (searchPattern != null)
            matchingQuery = matchingQuery.Where(field => EF.Functions.Like(field.Name, searchPattern));

        List<ChatGroup> pageGroups;
        string nextCursor;
        if (sortMode == ChatGroupSortMode.Popular)
            (pageGroups, nextCursor) = LoadPopularFeedPage(dbContext, matchingQuery, cursor);
        else if (sortMode == ChatGroupSortMode.MostActive)
            (pageGroups, nextCursor) = LoadMostActiveFeedPage(matchingQuery, cursor);
        else
            (pageGroups, nextCursor) = LoadDefaultFeedPage(matchingQuery, userAccountId.Value, activeGroupIds, pendingGroupIds, myJoinedAtByGroup, cursor);

        List<ChatGroupSummaryResult> items = BuildSummariesForPage(dbContext, pageGroups, userAccountId.Value, activeGroupIds, pendingGroupIds);
        return new ChatGroupPageResult(items, nextCursor);
    }

    public static ChatGroupMembersResult ListMembers(string authToken, Guid chatGroupId) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupMembersResult.Empty();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatGroupMembersResult.Empty();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!chatGroup.IsPublic && !callerIsActiveMember)
            return ChatGroupMembersResult.Empty();

        List<ChatGroupMember> activeMembers = [.. dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active)
            .OrderBy(field => field.JoinedAtUtc)];
        List<ChatGroupMember> pendingMembers = [];
        if (chatGroup.OwnerUserAccountId == userAccountId.Value)
            pendingMembers = [.. dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending)
                .OrderBy(field => field.JoinedAtUtc)];

        List<Guid> neededUserAccountIds = [.. activeMembers.Select(field => field.UserAccountId)];
        neededUserAccountIds.AddRange(pendingMembers.Select(field => field.UserAccountId));
        Dictionary<Guid, UserAccount> usersById = dbContext.UserAccounts
            .Where(field => neededUserAccountIds.Contains(field.Id))
            .ToDictionary(field => field.Id);

        List<ChatGroupMemberEntry> memberEntries = BuildMemberEntries(activeMembers, usersById, chatGroup.OwnerUserAccountId);
        List<ChatGroupMemberEntry> pendingEntries = BuildMemberEntries(pendingMembers, usersById, chatGroup.OwnerUserAccountId);
        return new ChatGroupMembersResult(memberEntries, pendingEntries);
    }

    // Methods - Owner Controls

    public static ChatGroupRenameResult Rename(string authToken, Guid chatGroupId, string name) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupRenameResult.None();
        string normalizedName = NormalizeName(name);
        if (normalizedName == null)
            return ChatGroupRenameResult.InvalidName();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.OwnerUserAccountId != userAccountId.Value)
            return ChatGroupRenameResult.None();
        if (chatGroup.Status != ChatGroupStatus.Active && chatGroup.Status != ChatGroupStatus.Provisional)
            return ChatGroupRenameResult.None();
        chatGroup.Name = normalizedName;
        TrySaveChanges(dbContext);
        return ChatGroupRenameResult.Renamed(normalizedName);
    }

    public static ChatGroupVisibilityResult SetVisibility(string authToken, Guid chatGroupId, bool isPublic) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupVisibilityResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (!IsOwnedActiveGroup(chatGroup, userAccountId.Value))
            return ChatGroupVisibilityResult.None();
        chatGroup.IsPublic = isPublic;
        TrySaveChanges(dbContext);
        return ChatGroupVisibilityResult.Updated(isPublic);
    }

    public static ChatGroupDeleteResult Delete(string authToken, Guid chatGroupId) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupDeleteResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (!IsOwnedActiveGroup(chatGroup, userAccountId.Value))
            return ChatGroupDeleteResult.None();
        NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
        NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
        using var transaction = dbContext.Database.BeginTransaction();
        int softDeleted = dbContext.ChatGroups
            .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
        if (softDeleted != 1) {
            transaction.Rollback();
            return ChatGroupDeleteResult.None();
        }
        ClearSoftDeletedGroupRows(dbContext, chatGroupId);
        transaction.Commit();
        return ChatGroupDeleteResult.Deleted();
    }

    // Methods - Membership

    public static ChatGroupLeaveResult Leave(string authToken, Guid chatGroupId, ChatGroupLeaveDisposition disposition) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupLeaveResult.NotMember();
        for (int attempt = 0; attempt < MaxOwnerLeaveAttempts; attempt++) {
            using var dbContext = HappyPlaceDbContext.Create();
            ChatGroupMember membership = dbContext.ChatGroupMembers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
            if (membership == null)
                return ChatGroupLeaveResult.NotMember();
            if (membership.MemberRole != ChatGroupMemberRole.Owner) {
                dbContext.ChatGroupMembers.Where(field => field.Id == membership.Id).ExecuteDelete();
                ReleaseConnectedOffer(dbContext, chatGroupId, userAccountId.Value);
                NotificationDispatchManager.RemoveMessagesChannel(chatGroupId, userAccountId.Value);
                return ChatGroupLeaveResult.Left();
            }
            ChatGroupLeaveResult ownerResult = TryOwnerLeave(dbContext, chatGroupId, userAccountId.Value, membership.Id, disposition);
            if (ownerResult != null)
                return ownerResult;
        }
        return ChatGroupLeaveResult.NotMember();
    }

    public static ChatGroupJoinRequestResult RequestToJoin(string authToken, Guid chatGroupId) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupJoinRequestResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active || chatGroup.IsPublic)
            return ChatGroupJoinRequestResult.None();
        ChatGroupMember existingMembership = dbContext.ChatGroupMembers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value);
        if (existingMembership != null) {
            if (existingMembership.Status == ChatGroupMemberStatus.Active)
                return ChatGroupJoinRequestResult.AlreadyMember();
            return ChatGroupJoinRequestResult.AlreadyRequested();
        }
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = userAccountId.Value, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Pending, JoinedAtUtc = DateTime.UtcNow });
        TrySaveChanges(dbContext);
        bool groupStillActive = dbContext.ChatGroups.Any(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active);
        if (!groupStillActive) {
            dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value && field.Status == ChatGroupMemberStatus.Pending).ExecuteDelete();
            return ChatGroupJoinRequestResult.None();
        }
        NotificationDispatchManager.MarkJoinRequestsDirty(chatGroupId);
        return ChatGroupJoinRequestResult.Requested();
    }

    public static ChatGroupCancelRequestResult CancelJoinRequest(string authToken, Guid chatGroupId) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return ChatGroupCancelRequestResult.NotRequested();
        using var dbContext = HappyPlaceDbContext.Create();
        int deletedCount = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId.Value && field.Status == ChatGroupMemberStatus.Pending)
            .ExecuteDelete();
        if (deletedCount == 0)
            return ChatGroupCancelRequestResult.NotRequested();
        NotificationDispatchManager.MarkJoinRequestsDirty(chatGroupId);
        return ChatGroupCancelRequestResult.Cancelled();
    }

    // Methods - Owner Approvals

    public static ChatGroupApproveResult ApproveMember(string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        Guid? ownerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (ownerUserAccountId == null)
            return ChatGroupApproveResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (!IsOwnedActiveGroup(chatGroup, ownerUserAccountId.Value))
            return ChatGroupApproveResult.None();
        int approvedCount = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == memberUserAccountId && field.Status == ChatGroupMemberStatus.Pending)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupMemberStatus.Active));
        if (approvedCount > 0) {
            NotificationDispatchManager.MarkJoinRequestsDirty(chatGroupId);
            NotificationDispatchManager.SendJoinApprovedPush(memberUserAccountId, chatGroupId, chatGroup.Name);
            return ChatGroupApproveResult.Approved();
        }
        bool alreadyActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == memberUserAccountId && field.Status == ChatGroupMemberStatus.Active);
        if (alreadyActiveMember)
            return ChatGroupApproveResult.AlreadyMember();
        return ChatGroupApproveResult.NotPending();
    }

    public static ChatGroupRejectResult RejectMember(string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        Guid? ownerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (ownerUserAccountId == null)
            return ChatGroupRejectResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (!IsOwnedActiveGroup(chatGroup, ownerUserAccountId.Value))
            return ChatGroupRejectResult.None();
        int rejectedCount = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == memberUserAccountId && field.Status == ChatGroupMemberStatus.Pending)
            .ExecuteDelete();
        if (rejectedCount == 0)
            return ChatGroupRejectResult.NotPending();
        NotificationDispatchManager.MarkJoinRequestsDirty(chatGroupId);
        return ChatGroupRejectResult.Rejected();
    }

    public static ChatGroupRemoveResult RemoveMember(string authToken, Guid chatGroupId, Guid memberUserAccountId) {
        Guid? ownerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (ownerUserAccountId == null)
            return ChatGroupRemoveResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (!IsOwnedActiveGroup(chatGroup, ownerUserAccountId.Value))
            return ChatGroupRemoveResult.None();
        if (memberUserAccountId == ownerUserAccountId.Value)
            return ChatGroupRemoveResult.CannotRemoveOwner();
        int removedCount = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == memberUserAccountId && field.Status == ChatGroupMemberStatus.Active)
            .ExecuteDelete();
        if (removedCount == 0)
            return ChatGroupRemoveResult.NotMember();
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == memberUserAccountId && field.Status == HelpOfferStatus.Connected)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, HelpOfferStatus.Released));
        NotificationDispatchManager.RemoveMessagesChannel(chatGroupId, memberUserAccountId);
        return ChatGroupRemoveResult.Removed();
    }

    // Methods - Account Deletion

    public static void UntangleUserForAccountDeletion(Guid userAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        List<Guid> pendingGroupIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending)
            .Select(field => field.ChatGroupId)];
        dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == userAccountId && field.Status == ChatGroupMemberStatus.Pending)
            .ExecuteDelete();
        foreach (Guid pendingGroupId in pendingGroupIds)
            NotificationDispatchManager.MarkJoinRequestsDirty(pendingGroupId);
        List<Guid> offeredGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == userAccountId && field.Status == HelpOfferStatus.Offered)
            .Select(field => field.ChatGroupId)];
        dbContext.HelpOffers.Where(field => field.HelperUserAccountId == userAccountId).ExecuteDelete();
        foreach (Guid offeredGroupId in offeredGroupIds)
            NotificationDispatchManager.MarkOffersDirty(offeredGroupId);
        dbContext.ChatMessageReactions.Where(field => field.UserAccountId == userAccountId).ExecuteDelete();
        List<Guid> ownedGroupIds = [.. dbContext.ChatGroups
            .Where(field => field.OwnerUserAccountId == userAccountId && field.Status != ChatGroupStatus.Deleted)
            .Select(field => field.Id)];
        foreach (Guid ownedGroupId in ownedGroupIds)
            UntangleOwnedGroup(ownedGroupId, userAccountId);
        dbContext.ChatGroupMembers.Where(field => field.UserAccountId == userAccountId).ExecuteDelete();
        dbContext.ChatGroups
            .Where(field => field.OwnerUserAccountId == userAccountId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.OwnerUserAccountId, (Guid?)null));
    }

    // Helpers - Owner Leave

    private static ChatGroupLeaveResult TryOwnerLeave(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid ownerUserAccountId, Guid ownerMembershipId, ChatGroupLeaveDisposition disposition) {
        try {
            using var transaction = dbContext.Database.BeginTransaction();
            int removed = dbContext.ChatGroupMembers
                .Where(field => field.Id == ownerMembershipId && field.Status == ChatGroupMemberStatus.Active)
                .ExecuteDelete();
            if (removed != 1) {
                transaction.Rollback();
                return null;
            }
            ReleaseConnectedOffer(dbContext, chatGroupId, ownerUserAccountId);
            ChatGroupMember successor = dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active)
                .OrderBy(field => field.JoinedAtUtc)
                .ThenBy(field => field.Id)
                .FirstOrDefault();
            if (successor != null) {
                int promoted = dbContext.ChatGroupMembers
                    .Where(field => field.Id == successor.Id && field.Status == ChatGroupMemberStatus.Active)
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.MemberRole, ChatGroupMemberRole.Owner));
                if (promoted != 1) {
                    transaction.Rollback();
                    return null;
                }
                dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId)
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.OwnerUserAccountId, (Guid?)successor.UserAccountId));
                transaction.Commit();
                NotificationDispatchManager.SyncJoinRequestsOwner(chatGroupId);
                NotificationDispatchManager.RemoveMessagesChannel(chatGroupId, ownerUserAccountId);
                return ChatGroupLeaveResult.Transferred();
            }
            if (disposition == ChatGroupLeaveDisposition.Delete) {
                NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
                NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
                int softDeleted = dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
                if (softDeleted != 1) {
                    transaction.Rollback();
                    return null;
                }
                ClearSoftDeletedGroupRows(dbContext, chatGroupId);
                transaction.Commit();
                return ChatGroupLeaveResult.Deleted();
            }
            if (disposition == ChatGroupLeaveDisposition.MakePublic) {
                int madePublic = dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                    .ExecuteUpdate(setters => setters
                        .SetProperty(field => field.IsPublic, true)
                        .SetProperty(field => field.OwnerUserAccountId, (Guid?)null));
                if (madePublic != 1) {
                    transaction.Rollback();
                    return null;
                }
                dbContext.ChatGroupMembers
                    .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending)
                    .ExecuteDelete();
                transaction.Commit();
                NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
                NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
                return ChatGroupLeaveResult.MadePublic();
            }
            transaction.Rollback();
            return ChatGroupLeaveResult.LastOwner();
        }
        catch (Exception) {
            return null;
        }
    }

    private static void ReleaseConnectedOffer(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid userAccountId) {
        dbContext.HelpOffers
            .Where(field => field.ChatGroupId == chatGroupId && field.HelperUserAccountId == userAccountId && field.Status == HelpOfferStatus.Connected)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, HelpOfferStatus.Released));
    }

    private static void ClearSoftDeletedGroupRows(HappyPlaceDbContext dbContext, Guid chatGroupId) {
        dbContext.ChatGroupMembers.Where(field => field.ChatGroupId == chatGroupId).ExecuteDelete();
        dbContext.HelpOffers.Where(field => field.ChatGroupId == chatGroupId).ExecuteDelete();
    }

    private static void UntangleOwnedGroup(Guid chatGroupId, Guid userAccountId) {
        for (int attempt = 0; attempt < MaxOwnerLeaveAttempts; attempt++)
            if (TryOwnerDepartureForAccountDeletion(chatGroupId, userAccountId))
                return;
    }

    private static bool TryOwnerDepartureForAccountDeletion(Guid chatGroupId, Guid userAccountId) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            using var transaction = dbContext.Database.BeginTransaction();
            dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == userAccountId)
                .ExecuteDelete();
            ChatGroupMember successor = dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active)
                .OrderBy(field => field.JoinedAtUtc)
                .ThenBy(field => field.Id)
                .FirstOrDefault();
            if (successor != null) {
                int promoted = dbContext.ChatGroupMembers
                    .Where(field => field.Id == successor.Id && field.Status == ChatGroupMemberStatus.Active)
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.MemberRole, ChatGroupMemberRole.Owner));
                if (promoted != 1) {
                    transaction.Rollback();
                    return false;
                }
                int reassigned = dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId && field.OwnerUserAccountId == userAccountId)
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.OwnerUserAccountId, (Guid?)successor.UserAccountId));
                if (reassigned != 1) {
                    transaction.Rollback();
                    return true;
                }
                transaction.Commit();
                NotificationDispatchManager.SyncJoinRequestsOwner(chatGroupId);
                return true;
            }
            ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
            if (chatGroup == null || chatGroup.OwnerUserAccountId != userAccountId || chatGroup.Status == ChatGroupStatus.Deleted) {
                transaction.Commit();
                return true;
            }
            if (chatGroup.Status == ChatGroupStatus.Provisional) {
                NotificationDispatchManager.RemoveOffersChannel(chatGroupId);
                int hardDeleted = dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Provisional && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                    .ExecuteDelete();
                if (hardDeleted != 1) {
                    transaction.Rollback();
                    return false;
                }
                transaction.Commit();
                NotificationDispatchManager.MarkWaitingDirtyForAllHelpers();
                return true;
            }
            NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
            NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
            int softDeleted = dbContext.ChatGroups
                .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
            if (softDeleted != 1) {
                transaction.Rollback();
                return false;
            }
            ClearSoftDeletedGroupRows(dbContext, chatGroupId);
            transaction.Commit();
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    // Helpers

    private static List<ChatGroupMemberEntry> BuildMemberEntries(List<ChatGroupMember> members, Dictionary<Guid, UserAccount> usersById, Guid? ownerUserAccountId) {
        List<ChatGroupMemberEntry> entries = [];
        foreach (ChatGroupMember member in members) {
            if (!usersById.TryGetValue(member.UserAccountId, out UserAccount user))
                continue;
            entries.Add(new ChatGroupMemberEntry(user.Id.ToString(), user.DisplayName, user.Username, user.ProfilePhotoUrl, UserAccountRegistrar.GetAvatarColor(user.Id), user.Id == ownerUserAccountId));
        }
        return entries;
    }

    private static int FeedBucketRank(ChatGroup group, Guid userAccountId, HashSet<Guid> activeGroupIds, HashSet<Guid> pendingGroupIds) {
        if (group.OwnerUserAccountId == userAccountId)
            return 0;
        if (activeGroupIds.Contains(group.Id))
            return 1;
        if (pendingGroupIds.Contains(group.Id))
            return 2;
        return 3;
    }

    private static DateTime FeedSortTimestamp(ChatGroup group, Guid userAccountId, HashSet<Guid> activeGroupIds, HashSet<Guid> pendingGroupIds, Dictionary<Guid, DateTime> myJoinedAtByGroup) {
        if (group.OwnerUserAccountId != userAccountId && (activeGroupIds.Contains(group.Id) || pendingGroupIds.Contains(group.Id)) && myJoinedAtByGroup.TryGetValue(group.Id, out DateTime joinedAt))
            return joinedAt;
        return group.CreatedAtUtc;
    }

    private static bool IsOwnedActiveGroup(ChatGroup chatGroup, Guid userAccountId) {
        return chatGroup != null && chatGroup.Status == ChatGroupStatus.Active && chatGroup.OwnerUserAccountId == userAccountId;
    }

    private static string NormalizeName(string name) {
        string trimmedName = (name ?? "").Trim();
        if (trimmedName.Length == 0)
            return null;
        if (trimmedName.Length > MaxChatGroupNameLength)
            return trimmedName[..MaxChatGroupNameLength];
        return trimmedName;
    }

    private static Dictionary<Guid, List<ChatGroupHelperAvatar>> LoadHelperAvatars(HappyPlaceDbContext dbContext, List<Guid> groupIds) {
        Dictionary<Guid, List<ChatGroupHelperAvatar>> helpersByGroup = [];
        if (groupIds.Count == 0)
            return helpersByGroup;

        List<ChatGroupMember> activeMembers = [.. dbContext.ChatGroupMembers
            .Where(field => groupIds.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Active)
            .OrderBy(field => field.ChatGroupId)
            .ThenBy(field => field.JoinedAtUtc)];

        Dictionary<Guid, List<ChatGroupMember>> topMembersByGroup = [];
        List<Guid> neededUserAccountIds = [];
        foreach (IGrouping<Guid, ChatGroupMember> membersInGroup in activeMembers.GroupBy(field => field.ChatGroupId)) {
            List<ChatGroupMember> topMembers = [.. membersInGroup.Take(MaxHelperAvatars)];
            topMembersByGroup[membersInGroup.Key] = topMembers;
            neededUserAccountIds.AddRange(topMembers.Select(member => member.UserAccountId));
        }

        Dictionary<Guid, UserAccount> usersById = dbContext.UserAccounts
            .Where(field => neededUserAccountIds.Contains(field.Id))
            .ToDictionary(field => field.Id);

        foreach (KeyValuePair<Guid, List<ChatGroupMember>> entry in topMembersByGroup) {
            List<ChatGroupHelperAvatar> avatars = [];
            foreach (ChatGroupMember member in entry.Value) {
                if (!usersById.TryGetValue(member.UserAccountId, out UserAccount user))
                    continue;
                avatars.Add(new ChatGroupHelperAvatar(user.ProfilePhotoUrl, UserAccountRegistrar.GetAvatarColor(user.Id), BuildInitial(user.DisplayName)));
            }
            helpersByGroup[entry.Key] = avatars;
        }
        return helpersByGroup;
    }

    private static string BuildInitial(string displayName) {
        string trimmedDisplayName = (displayName ?? "").Trim();
        if (trimmedDisplayName.Length == 0)
            return "?";
        return trimmedDisplayName[..1].ToUpperInvariant();
    }

    // Helpers - Search And Sort

    private static ChatGroupSortMode ParseSortMode(string sortBy) {
        string normalizedSortBy = (sortBy ?? "").Trim();
        if (string.Equals(normalizedSortBy, "Popular", StringComparison.OrdinalIgnoreCase))
            return ChatGroupSortMode.Popular;
        if (string.Equals(normalizedSortBy, "Most Active", StringComparison.OrdinalIgnoreCase))
            return ChatGroupSortMode.MostActive;
        if (string.Equals(normalizedSortBy, "Public", StringComparison.OrdinalIgnoreCase))
            return ChatGroupSortMode.Public;
        if (string.Equals(normalizedSortBy, "Private", StringComparison.OrdinalIgnoreCase))
            return ChatGroupSortMode.Private;
        return ChatGroupSortMode.Latest;
    }

    private static string BuildSearchPattern(string search) {
        string normalizedSearch = (search ?? "").Trim();
        if (normalizedSearch.Length == 0)
            return null;
        return "%" + EscapeLikePattern(normalizedSearch) + "%";
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }

    private static List<ChatGroup> OrderGroups(List<ChatGroup> groups, ChatGroupSortMode sortMode, Guid userAccountId, HashSet<Guid> activeGroupIds, HashSet<Guid> pendingGroupIds, Dictionary<Guid, DateTime> myJoinedAtByGroup, Dictionary<Guid, int> memberCounts) {
        if (sortMode == ChatGroupSortMode.Popular)
            return [.. groups
                .OrderByDescending(group => memberCounts.TryGetValue(group.Id, out int memberCount) ? memberCount : 0)
                .ThenByDescending(group => group.CreatedAtUtc)
                .ThenBy(group => group.Id)];
        if (sortMode == ChatGroupSortMode.MostActive)
            return [.. groups
                .OrderByDescending(group => group.LastSeenAtUtc)
                .ThenByDescending(group => group.CreatedAtUtc)
                .ThenBy(group => group.Id)];
        return [.. groups
            .OrderBy(group => FeedBucketRank(group, userAccountId, activeGroupIds, pendingGroupIds))
            .ThenByDescending(group => FeedSortTimestamp(group, userAccountId, activeGroupIds, pendingGroupIds, myJoinedAtByGroup))];
    }

    private enum ChatGroupSortMode : byte {
        Latest = 0,
        Popular = 1,
        MostActive = 2,
        Public = 3,
        Private = 4
    }


    // Helpers - Feed Paging

    private static (List<ChatGroup> Groups, string NextCursor) LoadDefaultFeedPage(IQueryable<ChatGroup> matchingQuery, Guid userAccountId, HashSet<Guid> activeGroupIds, HashSet<Guid> pendingGroupIds, Dictionary<Guid, DateTime> myJoinedAtByGroup, string cursor) {
        List<ChatGroup> mineGroups = [];
        bool hasCursor = CursorCodec.TryDecodeFeedCursor(cursor, DefaultFeedCursorMarker, out long afterCreatedTicks, out _, out Guid afterId);
        if (!hasCursor) {
            mineGroups = [.. matchingQuery.Where(field => field.OwnerUserAccountId == userAccountId || activeGroupIds.Contains(field.Id) || pendingGroupIds.Contains(field.Id))];
            mineGroups = [.. mineGroups
                .OrderBy(group => FeedBucketRank(group, userAccountId, activeGroupIds, pendingGroupIds))
                .ThenByDescending(group => FeedSortTimestamp(group, userAccountId, activeGroupIds, pendingGroupIds, myJoinedAtByGroup))];
        }
        IQueryable<ChatGroup> discoveryQuery = matchingQuery
            .Where(field => (field.OwnerUserAccountId == null || field.OwnerUserAccountId != userAccountId) && !activeGroupIds.Contains(field.Id) && !pendingGroupIds.Contains(field.Id));
        if (hasCursor) {
            DateTime afterCreatedAtUtc = new(afterCreatedTicks, DateTimeKind.Utc);
            discoveryQuery = discoveryQuery.Where(field => field.CreatedAtUtc < afterCreatedAtUtc || (field.CreatedAtUtc == afterCreatedAtUtc && field.Id.CompareTo(afterId) < 0));
        }
        int discoveryWanted = Math.Max(1, FeedPageSize - mineGroups.Count);
        List<ChatGroup> discoveryGroups = [.. discoveryQuery
            .OrderByDescending(field => field.CreatedAtUtc)
            .ThenByDescending(field => field.Id)
            .Take(discoveryWanted + 1)];
        bool hasMore = discoveryGroups.Count > discoveryWanted;
        if (hasMore)
            discoveryGroups.RemoveAt(discoveryGroups.Count - 1);
        string nextCursor = hasMore ? CursorCodec.EncodeFeedCursor(DefaultFeedCursorMarker, discoveryGroups[^1].CreatedAtUtc.Ticks, 0, discoveryGroups[^1].Id) : null;
        mineGroups.AddRange(discoveryGroups);
        return (mineGroups, nextCursor);
    }

    private static (List<ChatGroup> Groups, string NextCursor) LoadMostActiveFeedPage(IQueryable<ChatGroup> matchingQuery, string cursor) {
        if (CursorCodec.TryDecodeFeedCursor(cursor, MostActiveFeedCursorMarker, out long afterLastSeenTicks, out long afterCreatedTicks, out Guid afterId)) {
            DateTime afterLastSeenAtUtc = new(afterLastSeenTicks, DateTimeKind.Utc);
            DateTime afterCreatedAtUtc = new(afterCreatedTicks, DateTimeKind.Utc);
            matchingQuery = matchingQuery.Where(field => field.LastSeenAtUtc < afterLastSeenAtUtc
                || (field.LastSeenAtUtc == afterLastSeenAtUtc && (field.CreatedAtUtc < afterCreatedAtUtc
                || (field.CreatedAtUtc == afterCreatedAtUtc && field.Id.CompareTo(afterId) < 0))));
        }
        List<ChatGroup> pageGroups = [.. matchingQuery
            .OrderByDescending(field => field.LastSeenAtUtc)
            .ThenByDescending(field => field.CreatedAtUtc)
            .ThenByDescending(field => field.Id)
            .Take(FeedPageSize + 1)];
        bool hasMore = pageGroups.Count > FeedPageSize;
        if (hasMore)
            pageGroups.RemoveAt(pageGroups.Count - 1);
        string nextCursor = hasMore ? CursorCodec.EncodeFeedCursor(MostActiveFeedCursorMarker, pageGroups[^1].LastSeenAtUtc.Ticks, pageGroups[^1].CreatedAtUtc.Ticks, pageGroups[^1].Id) : null;
        return (pageGroups, nextCursor);
    }

    private static (List<ChatGroup> Groups, string NextCursor) LoadPopularFeedPage(HappyPlaceDbContext dbContext, IQueryable<ChatGroup> matchingQuery, string cursor) {
        var rankedQuery = matchingQuery.Select(group => new {
            Group = group,
            MemberCount = dbContext.ChatGroupMembers.Count(member => member.ChatGroupId == group.Id && member.Status == ChatGroupMemberStatus.Active)
        });
        if (CursorCodec.TryDecodeFeedCursor(cursor, PopularFeedCursorMarker, out long afterMemberCount, out long afterCreatedTicks, out Guid afterId)) {
            int afterCount = (int)afterMemberCount;
            DateTime afterCreatedAtUtc = new(afterCreatedTicks, DateTimeKind.Utc);
            rankedQuery = rankedQuery.Where(entry => entry.MemberCount < afterCount
                || (entry.MemberCount == afterCount && (entry.Group.CreatedAtUtc < afterCreatedAtUtc
                || (entry.Group.CreatedAtUtc == afterCreatedAtUtc && entry.Group.Id.CompareTo(afterId) < 0))));
        }
        var rankedPage = rankedQuery
            .OrderByDescending(entry => entry.MemberCount)
            .ThenByDescending(entry => entry.Group.CreatedAtUtc)
            .ThenByDescending(entry => entry.Group.Id)
            .Take(FeedPageSize + 1)
            .ToList();
        bool hasMore = rankedPage.Count > FeedPageSize;
        if (hasMore)
            rankedPage.RemoveAt(rankedPage.Count - 1);
        string nextCursor = hasMore ? CursorCodec.EncodeFeedCursor(PopularFeedCursorMarker, rankedPage[^1].MemberCount, rankedPage[^1].Group.CreatedAtUtc.Ticks, rankedPage[^1].Group.Id) : null;
        return ([.. rankedPage.Select(entry => entry.Group)], nextCursor);
    }

    private static List<ChatGroupSummaryResult> BuildSummariesForPage(HappyPlaceDbContext dbContext, List<ChatGroup> pageGroups, Guid userAccountId, HashSet<Guid> activeGroupIds, HashSet<Guid> pendingGroupIds) {
        if (pageGroups.Count == 0)
            return [];
        List<Guid> pageGroupIdList = [.. pageGroups.Select(field => field.Id)];
        Dictionary<Guid, int> memberCounts = dbContext.ChatGroupMembers
            .Where(field => pageGroupIdList.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Active)
            .GroupBy(field => field.ChatGroupId)
            .Select(group => new { ChatGroupId = group.Key, Count = group.Count() })
            .ToDictionary(row => row.ChatGroupId, row => row.Count);
        HashSet<Guid> groupIdsWithPendingMembers = [.. dbContext.ChatGroupMembers
            .Where(field => pageGroupIdList.Contains(field.ChatGroupId) && field.Status == ChatGroupMemberStatus.Pending)
            .Select(field => field.ChatGroupId)
            .Distinct()];
        List<Guid> avatarGroupIds = [.. pageGroups.Where(field => field.IsPublic || activeGroupIds.Contains(field.Id)).Select(field => field.Id)];
        Dictionary<Guid, List<ChatGroupHelperAvatar>> helpersByGroup = LoadHelperAvatars(dbContext, avatarGroupIds);
        Dictionary<Guid, int> unreadCounts = LoadUnreadCounts(dbContext, userAccountId, pageGroupIdList);
        List<ChatGroupSummaryResult> results = [];
        foreach (ChatGroup group in pageGroups) {
            bool owner = group.OwnerUserAccountId == userAccountId;
            bool joined = activeGroupIds.Contains(group.Id);
            bool joinRequest = pendingGroupIds.Contains(group.Id);
            bool pendingMembers = owner && groupIdsWithPendingMembers.Contains(group.Id);
            int memberCount = memberCounts.TryGetValue(group.Id, out int count) ? count : 0;
            List<ChatGroupHelperAvatar> helpers = helpersByGroup.TryGetValue(group.Id, out List<ChatGroupHelperAvatar> avatars) ? avatars : [];
            int unreadCount = unreadCounts.TryGetValue(group.Id, out int unread) ? unread : 0;
            results.Add(new ChatGroupSummaryResult(group.Id.ToString(), group.Name, group.IsPublic, owner, joined, joinRequest, pendingMembers, memberCount, helpers, unreadCount));
        }
        return results;
    }

    private static Dictionary<Guid, int> LoadUnreadCounts(HappyPlaceDbContext dbContext, Guid callerUserAccountId, List<Guid> displayGroupIds) {
        if (displayGroupIds.Count == 0)
            return [];
        return dbContext.ChatGroupMembers
            .Where(member => member.UserAccountId == callerUserAccountId && member.Status == ChatGroupMemberStatus.Active && displayGroupIds.Contains(member.ChatGroupId))
            .Select(member => new {
                member.ChatGroupId,
                Count = dbContext.ChatMessages.Count(message => message.ChatGroupId == member.ChatGroupId && message.Sequence > member.LastReadSequence && !message.IsDeleted && message.SenderUserAccountId != callerUserAccountId)
            })
            .ToDictionary(row => row.ChatGroupId, row => row.Count);
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try { dbContext.SaveChanges(); }
        catch (DbUpdateException) { }
    }
}
