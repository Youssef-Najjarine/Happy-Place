using System.Data.SqlTypes;
using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class ChatGroupManager {
    // Fields

    private static readonly int MaxHelperAvatars = 5;
    private static readonly int MaxAvailableHelpers = 50;
    private static readonly int MaxChatGroupNameLength = 100;
    private static readonly int MaxFriendsGroupMembers = 20;
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
        List<Guid> blockRelatedIds = LoadBlockRelatedIds(dbContext, userAccountId.Value);
        List<Guid> hiddenGroupIds = LoadHiddenGroupIds(dbContext, userAccountId.Value);
        HashSet<Guid> mutedGroupIds = LoadMutedGroupIds(dbContext, userAccountId.Value);
        IQueryable<ChatGroup> matchingQuery = dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Active);
        matchingQuery = ApplyFeedVisibility(matchingQuery, sortMode, activeGroupIds, blockRelatedIds, hiddenGroupIds);
        string searchPattern = BuildSearchPattern(search);
        matchingQuery = ApplySearch(matchingQuery, dbContext, userAccountId.Value, searchPattern);

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
        Dictionary<Guid, ChatGroupDirectContact> directContactsByGroup = LoadDirectContacts(dbContext, orderedGroups, userAccountId.Value);
        Dictionary<Guid, LastMessageEntry> lastMessagesByGroup = LoadLastMessages(dbContext, orderedGroups);

        List<ChatGroupSummaryResult> results = [];
        foreach (ChatGroup group in orderedGroups) {
            bool owner = group.OwnerUserAccountId == userAccountId.Value;
            bool joined = activeGroupIds.Contains(group.Id);
            bool joinRequest = pendingGroupIds.Contains(group.Id);
            bool pendingMembers = owner && groupIdsWithPendingMembers.Contains(group.Id);
            int memberCount = memberCounts.TryGetValue(group.Id, out int count) ? count : 0;
            List<ChatGroupHelperAvatar> helpers = helpersByGroup.TryGetValue(group.Id, out List<ChatGroupHelperAvatar> avatars) ? avatars : [];
            int unreadCount = unreadCounts.TryGetValue(group.Id, out int unread) ? unread : 0;
            ChatGroupDirectContact directContact = directContactsByGroup.TryGetValue(group.Id, out ChatGroupDirectContact contact) ? contact : null;
            LastMessageEntry lastMessageEntry = lastMessagesByGroup.TryGetValue(group.Id, out LastMessageEntry lastMessage) ? lastMessage : null;
            results.Add(new ChatGroupSummaryResult(group.Id.ToString(), group.Name, group.IsPublic, owner, joined, joinRequest, pendingMembers, memberCount, helpers, unreadCount, group.DirectPairLowId != null, directContact, joined ? lastMessageEntry?.Preview : null, joined ? lastMessageEntry?.CreatedAtUtc : null, mutedGroupIds.Contains(group.Id)));
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
        List<Guid> blockRelatedIds = LoadBlockRelatedIds(dbContext, userAccountId.Value);
        List<Guid> hiddenGroupIds = LoadHiddenGroupIds(dbContext, userAccountId.Value);
        IQueryable<ChatGroup> matchingQuery = dbContext.ChatGroups
            .Where(field => field.Status == ChatGroupStatus.Active);
        matchingQuery = ApplyFeedVisibility(matchingQuery, sortMode, activeGroupIds, blockRelatedIds, hiddenGroupIds);
        matchingQuery = ApplySearch(matchingQuery, dbContext, userAccountId.Value, searchPattern);

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
            .OrderBy(field => field.JoinedAtUtc)
            .ThenBy(field => field.Id)];
        List<ChatGroupMember> pendingMembers = [];
        if (chatGroup.OwnerUserAccountId == userAccountId.Value)
            pendingMembers = [.. dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending)
                .OrderBy(field => field.JoinedAtUtc)
                .ThenBy(field => field.Id)];

        List<Guid> neededUserAccountIds = [.. activeMembers.Select(field => field.UserAccountId)];
        neededUserAccountIds.AddRange(pendingMembers.Select(field => field.UserAccountId));
        Dictionary<Guid, UserAccount> usersById = dbContext.UserAccounts
            .Where(field => neededUserAccountIds.Contains(field.Id))
            .ToDictionary(field => field.Id);

        List<ChatGroupMemberEntry> memberEntries = ChatGroupMemberEntry.FromMembers(activeMembers, usersById, chatGroup.OwnerUserAccountId);
        List<ChatGroupMemberEntry> pendingEntries = ChatGroupMemberEntry.FromMembers(pendingMembers, usersById, chatGroup.OwnerUserAccountId);
        return new ChatGroupMembersResult(userAccountId.Value.ToString(), memberEntries, pendingEntries);
    }

    // Methods - Creation

    public static ChatGroupCreateWithFriendsResult CreateWithFriends(string authToken, string name, List<string> usernames) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return ChatGroupCreateWithFriendsResult.None();
        if (caller.IsAnonymous)
            return ChatGroupCreateWithFriendsResult.AccountRequired();
        string normalizedName = NormalizeName(name);
        if (normalizedName == null)
            return ChatGroupCreateWithFriendsResult.InvalidName();
        using var dbContext = HappyPlaceDbContext.Create();
        HashSet<Guid> friendUserAccountIds = [];
        foreach (string username in usernames ?? []) {
            UserAccount friendAccount = ResolveDirectPartner(dbContext, username);
            if (friendAccount == null || friendAccount.Id == caller.Id)
                return ChatGroupCreateWithFriendsResult.NotFriends();
            if (!FriendshipManager.CanDirectMessage(dbContext, caller.Id, friendAccount.Id))
                return ChatGroupCreateWithFriendsResult.NotFriends();
            friendUserAccountIds.Add(friendAccount.Id);
        }
        if (friendUserAccountIds.Count == 0 || friendUserAccountIds.Count > MaxFriendsGroupMembers)
            return ChatGroupCreateWithFriendsResult.NotFriends();
        DateTime now = DateTime.UtcNow;
        Guid chatGroupId = Guid.NewGuid();
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = normalizedName, OwnerUserAccountId = caller.Id, IsPublic = false, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = caller.Id, MemberRole = ChatGroupMemberRole.Owner, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        foreach (Guid friendUserAccountId in friendUserAccountIds)
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = friendUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.SaveChanges();
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind);
        return ChatGroupCreateWithFriendsResult.Created(chatGroupId);
    }

    // Methods - Direct Messages

    public static ChatGroupOpenDirectResult OpenDirect(string authToken, string username) {
        var caller = UserAccountResolver.Resolve(authToken);
        if (caller == null)
            return ChatGroupOpenDirectResult.None();
        if (caller.IsAnonymous)
            return ChatGroupOpenDirectResult.AccountRequired();
        using var dbContext = HappyPlaceDbContext.Create();
        UserAccount partner = ResolveDirectPartner(dbContext, username);
        if (partner == null || partner.Id == caller.Id)
            return ChatGroupOpenDirectResult.NotFriends();
        if (!FriendshipManager.CanDirectMessage(dbContext, caller.Id, partner.Id))
            return ChatGroupOpenDirectResult.NotFriends();
        (Guid pairLowId, Guid pairHighId) = ComputeDirectPair(caller.Id, partner.Id);
        ChatGroup existingGroup = dbContext.ChatGroups.SingleOrDefault(field => field.DirectPairLowId == pairLowId && field.DirectPairHighId == pairHighId);
        if (existingGroup != null)
            return OpenExistingDirectGroup(dbContext, existingGroup, caller.Id, partner.Id);
        DateTime now = DateTime.UtcNow;
        Guid chatGroupId = Guid.NewGuid();
        dbContext.ChatGroups.Add(new ChatGroup { Id = chatGroupId, Name = "", OwnerUserAccountId = null, IsPublic = false, Status = ChatGroupStatus.Active, CreatedAtUtc = now, LastSeenAtUtc = now, DirectPairLowId = pairLowId, DirectPairHighId = pairHighId });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = caller.Id, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = partner.Id, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
            return LoadDirectGroupAfterRace(pairLowId, pairHighId, caller.Id, partner.Id);
        }
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind);
        return ChatGroupOpenDirectResult.Opened(chatGroupId);
    }

    public static (Guid PairLowId, Guid PairHighId) ComputeDirectPair(Guid firstUserAccountId, Guid secondUserAccountId) {
        bool firstIsLow = new SqlGuid(firstUserAccountId).CompareTo(new SqlGuid(secondUserAccountId)) < 0;
        return firstIsLow ? (firstUserAccountId, secondUserAccountId) : (secondUserAccountId, firstUserAccountId);
    }

    private static ChatGroupOpenDirectResult OpenExistingDirectGroup(HappyPlaceDbContext dbContext, ChatGroup existingGroup, Guid callerUserAccountId, Guid partnerUserAccountId) {
        if (existingGroup.Status != ChatGroupStatus.Active)
            return ChatGroupOpenDirectResult.None();
        EnsureDirectMemberships(dbContext, existingGroup.Id, callerUserAccountId, partnerUserAccountId);
        RealtimePublisher.PublishChatGroupChanged(existingGroup.Id, RealtimePublisher.MembershipKind);
        return ChatGroupOpenDirectResult.Opened(existingGroup.Id);
    }

    private static ChatGroupOpenDirectResult LoadDirectGroupAfterRace(Guid pairLowId, Guid pairHighId, Guid callerUserAccountId, Guid partnerUserAccountId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup existingGroup = dbContext.ChatGroups.SingleOrDefault(field => field.DirectPairLowId == pairLowId && field.DirectPairHighId == pairHighId);
        if (existingGroup == null)
            return ChatGroupOpenDirectResult.None();
        return OpenExistingDirectGroup(dbContext, existingGroup, callerUserAccountId, partnerUserAccountId);
    }

    private static void EnsureDirectMemberships(HappyPlaceDbContext dbContext, Guid chatGroupId, Guid callerUserAccountId, Guid partnerUserAccountId) {
        List<Guid> existingMemberIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId)
            .Select(field => field.UserAccountId)];
        DateTime now = DateTime.UtcNow;
        if (!existingMemberIds.Contains(callerUserAccountId))
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = callerUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        if (!existingMemberIds.Contains(partnerUserAccountId))
            dbContext.ChatGroupMembers.Add(new ChatGroupMember { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, UserAccountId = partnerUserAccountId, MemberRole = ChatGroupMemberRole.Member, Status = ChatGroupMemberStatus.Active, JoinedAtUtc = now });
        dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId && field.HiddenAtUtc != null)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.HiddenAtUtc, (DateTime?)null));
        TrySaveChanges(dbContext);
    }

    private static UserAccount ResolveDirectPartner(HappyPlaceDbContext dbContext, string username) {
        string normalizedUsername = (username ?? "").Trim().ToLowerInvariant();
        if (normalizedUsername.Length == 0)
            return null;
        UserAccount partner = dbContext.UserAccounts.SingleOrDefault(field => field.Username == normalizedUsername);
        if (partner == null || partner.IsAnonymous)
            return null;
        return partner;
    }

    // Methods - Membership Preferences

    public static ChatGroupHideResult Hide(string authToken, Guid chatGroupId) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatGroupHideResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroupMember membership = dbContext.ChatGroupMembers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (membership == null)
            return ChatGroupHideResult.NotMember();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null)
            return ChatGroupHideResult.NotMember();
        if (chatGroup.DirectPairLowId == null)
            return ChatGroupHideResult.NotAllowed();
        membership.HiddenAtUtc = DateTime.UtcNow;
        dbContext.SaveChanges();
        return ChatGroupHideResult.Hidden();
    }

    public static ChatGroupMuteResult SetMuted(string authToken, Guid chatGroupId, bool isMuted) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatGroupMuteResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroupMember membership = dbContext.ChatGroupMembers.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (membership == null)
            return ChatGroupMuteResult.NotMember();
        membership.IsMuted = isMuted;
        dbContext.SaveChanges();
        return isMuted ? ChatGroupMuteResult.Muted() : ChatGroupMuteResult.Unmuted();
    }

    public static ChatGroupUnreadTotalResult UnreadTotal(string authToken) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatGroupUnreadTotalResult.None();
        using var dbContext = HappyPlaceDbContext.Create();
        int total = dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active && field.HiddenAtUtc == null)
            .Sum(field => dbContext.ChatMessages.Count(message => message.ChatGroupId == field.ChatGroupId && !message.IsDeleted && message.SenderUserAccountId != callerUserAccountId.Value && message.Sequence > field.LastReadSequence));
        return ChatGroupUnreadTotalResult.Ok(total);
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
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
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
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
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
        List<Guid> formerMemberUserAccountIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId)
            .Select(field => field.UserAccountId)];
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
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, formerMemberUserAccountIds);
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
            ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
            if (chatGroup == null)
                return ChatGroupLeaveResult.NotMember();
            if (chatGroup.DirectPairLowId != null)
                return ChatGroupLeaveResult.NotAllowed();
            if (membership.MemberRole != ChatGroupMemberRole.Owner) {
                dbContext.ChatGroupMembers.Where(field => field.Id == membership.Id).ExecuteDelete();
                ReleaseConnectedOffer(dbContext, chatGroupId, userAccountId.Value);
                NotificationDispatchManager.RemoveMessagesChannel(chatGroupId, userAccountId.Value);
                List<Guid> extraRecipientUserAccountIds = [userAccountId.Value];
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active || chatGroup.IsPublic || chatGroup.DirectPairLowId != null)
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
        List<Guid> extraRecipientUserAccountIds = [userAccountId.Value];
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
        List<Guid> extraRecipientUserAccountIds = [userAccountId.Value];
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
            RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind);
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
        List<Guid> extraRecipientUserAccountIds = [memberUserAccountId];
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
        List<Guid> extraRecipientUserAccountIds = [memberUserAccountId];
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
        foreach (Guid pendingGroupId in pendingGroupIds) {
            NotificationDispatchManager.MarkJoinRequestsDirty(pendingGroupId);
            RealtimePublisher.PublishChatGroupChanged(pendingGroupId, RealtimePublisher.MembershipKind);
        }
        List<Guid> offeredGroupIds = [.. dbContext.HelpOffers
            .Where(field => field.HelperUserAccountId == userAccountId && field.Status == HelpOfferStatus.Offered)
            .Select(field => field.ChatGroupId)];
        List<Guid> offeredGroupOwnerUserAccountIds = [.. dbContext.ChatGroups
            .Where(field => offeredGroupIds.Contains(field.Id) && field.OwnerUserAccountId != null)
            .Select(field => field.OwnerUserAccountId.Value)];
        dbContext.HelpOffers.Where(field => field.HelperUserAccountId == userAccountId).ExecuteDelete();
        foreach (Guid offeredGroupId in offeredGroupIds)
            NotificationDispatchManager.MarkOffersDirty(offeredGroupId);
        RealtimePublisher.PublishHelpChanged(offeredGroupOwnerUserAccountIds);
        dbContext.ChatMessageReactions.Where(field => field.UserAccountId == userAccountId).ExecuteDelete();
        var directGroups = dbContext.ChatGroups
            .Where(field => (field.DirectPairLowId == userAccountId || field.DirectPairHighId == userAccountId) && field.Status != ChatGroupStatus.Deleted)
            .Select(field => new { field.Id, field.DirectPairLowId, field.DirectPairHighId })
            .ToList();
        foreach (var directGroup in directGroups) {
            NotificationDispatchManager.RemoveMessagesChannels(directGroup.Id);
            dbContext.ChatGroups
                .Where(field => field.Id == directGroup.Id && field.Status != ChatGroupStatus.Deleted)
                .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
            ClearSoftDeletedGroupRows(dbContext, directGroup.Id);
            Guid directPartnerUserAccountId = directGroup.DirectPairLowId.Value == userAccountId ? directGroup.DirectPairHighId.Value : directGroup.DirectPairLowId.Value;
            List<Guid> extraRecipientUserAccountIds = [directPartnerUserAccountId];
            RealtimePublisher.PublishChatGroupChanged(directGroup.Id, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
        }
        List<Guid> ownedGroupIds = [.. dbContext.ChatGroups
            .Where(field => field.OwnerUserAccountId == userAccountId && field.Status != ChatGroupStatus.Deleted)
            .Select(field => field.Id)];
        foreach (Guid ownedGroupId in ownedGroupIds)
            UntangleOwnedGroup(ownedGroupId, userAccountId);
        List<Guid> remainingMembershipGroupIds = [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == userAccountId)
            .Select(field => field.ChatGroupId)];
        dbContext.ChatGroupMembers.Where(field => field.UserAccountId == userAccountId).ExecuteDelete();
        foreach (Guid remainingMembershipGroupId in remainingMembershipGroupIds)
            RealtimePublisher.PublishChatGroupChanged(remainingMembershipGroupId, RealtimePublisher.MembershipKind);
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
                List<Guid> extraRecipientUserAccountIds = [ownerUserAccountId];
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
                return ChatGroupLeaveResult.Transferred();
            }
            if (disposition == ChatGroupLeaveDisposition.Delete) {
                NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
                NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
                List<Guid> remainingMemberUserAccountIds = [.. dbContext.ChatGroupMembers
                    .Where(field => field.ChatGroupId == chatGroupId)
                    .Select(field => field.UserAccountId)];
                int softDeleted = dbContext.ChatGroups
                    .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                    .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
                if (softDeleted != 1) {
                    transaction.Rollback();
                    return null;
                }
                ClearSoftDeletedGroupRows(dbContext, chatGroupId);
                transaction.Commit();
                List<Guid> extraRecipientUserAccountIds = [ownerUserAccountId, .. remainingMemberUserAccountIds];
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
                List<Guid> pendingMemberUserAccountIds = [.. dbContext.ChatGroupMembers
                    .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending)
                    .Select(field => field.UserAccountId)];
                dbContext.ChatGroupMembers
                    .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Pending)
                    .ExecuteDelete();
                transaction.Commit();
                NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
                NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
                List<Guid> extraRecipientUserAccountIds = [ownerUserAccountId, .. pendingMemberUserAccountIds];
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, extraRecipientUserAccountIds);
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
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind);
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
                RealtimePublisher.PublishHelpOpenRequestsChanged();
                return true;
            }
            NotificationDispatchManager.RemoveJoinRequestsChannel(chatGroupId);
            NotificationDispatchManager.RemoveMessagesChannels(chatGroupId);
            List<Guid> remainingMemberUserAccountIds = [.. dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId)
                .Select(field => field.UserAccountId)];
            int softDeleted = dbContext.ChatGroups
                .Where(field => field.Id == chatGroupId && field.Status == ChatGroupStatus.Active && !dbContext.ChatGroupMembers.Any(member => member.ChatGroupId == chatGroupId && member.Status == ChatGroupMemberStatus.Active))
                .ExecuteUpdate(setters => setters.SetProperty(field => field.Status, ChatGroupStatus.Deleted));
            if (softDeleted != 1) {
                transaction.Rollback();
                return false;
            }
            ClearSoftDeletedGroupRows(dbContext, chatGroupId);
            transaction.Commit();
            RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MembershipKind, remainingMemberUserAccountIds);
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    // Helpers

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
        if (string.Equals(normalizedSortBy, "Direct", StringComparison.OrdinalIgnoreCase))
            return ChatGroupSortMode.DirectMessages;
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

    private sealed record LastMessageEntry(string Preview, DateTime CreatedAtUtc);

    private enum ChatGroupSortMode : byte {
        Latest = 0,
        Popular = 1,
        MostActive = 2,
        Public = 3,
        Private = 4,
        DirectMessages = 5
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
        Dictionary<Guid, ChatGroupDirectContact> directContactsByGroup = LoadDirectContacts(dbContext, pageGroups, userAccountId);
        Dictionary<Guid, LastMessageEntry> lastMessagesByGroup = LoadLastMessages(dbContext, pageGroups);
        HashSet<Guid> mutedGroupIds = LoadMutedGroupIds(dbContext, userAccountId);
        List<ChatGroupSummaryResult> results = [];
        foreach (ChatGroup group in pageGroups) {
            bool owner = group.OwnerUserAccountId == userAccountId;
            bool joined = activeGroupIds.Contains(group.Id);
            bool joinRequest = pendingGroupIds.Contains(group.Id);
            bool pendingMembers = owner && groupIdsWithPendingMembers.Contains(group.Id);
            int memberCount = memberCounts.TryGetValue(group.Id, out int count) ? count : 0;
            List<ChatGroupHelperAvatar> helpers = helpersByGroup.TryGetValue(group.Id, out List<ChatGroupHelperAvatar> avatars) ? avatars : [];
            int unreadCount = unreadCounts.TryGetValue(group.Id, out int unread) ? unread : 0;
            ChatGroupDirectContact directContact = directContactsByGroup.TryGetValue(group.Id, out ChatGroupDirectContact contact) ? contact : null;
            LastMessageEntry lastMessageEntry = lastMessagesByGroup.TryGetValue(group.Id, out LastMessageEntry lastMessage) ? lastMessage : null;
            results.Add(new ChatGroupSummaryResult(group.Id.ToString(), group.Name, group.IsPublic, owner, joined, joinRequest, pendingMembers, memberCount, helpers, unreadCount, group.DirectPairLowId != null, directContact, joined ? lastMessageEntry?.Preview : null, joined ? lastMessageEntry?.CreatedAtUtc : null, mutedGroupIds.Contains(group.Id)));
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

    private static List<Guid> LoadBlockRelatedIds(HappyPlaceDbContext dbContext, Guid callerUserAccountId) {
        return [.. dbContext.UserBlocks
            .Where(field => field.BlockerUserAccountId == callerUserAccountId || field.BlockedUserAccountId == callerUserAccountId)
            .Select(field => field.BlockerUserAccountId == callerUserAccountId ? field.BlockedUserAccountId : field.BlockerUserAccountId)];
    }

    private static IQueryable<ChatGroup> ApplyFeedVisibility(IQueryable<ChatGroup> matchingQuery, ChatGroupSortMode sortMode, HashSet<Guid> activeGroupIds, List<Guid> blockRelatedIds, List<Guid> hiddenGroupIds) {
        matchingQuery = matchingQuery.Where(field => field.DirectPairLowId == null || (activeGroupIds.Contains(field.Id) && !blockRelatedIds.Contains(field.DirectPairLowId.Value) && !blockRelatedIds.Contains(field.DirectPairHighId.Value) && !hiddenGroupIds.Contains(field.Id)));
        if (sortMode == ChatGroupSortMode.Public)
            return matchingQuery.Where(field => field.IsPublic);
        if (sortMode == ChatGroupSortMode.Private)
            return matchingQuery.Where(field => !field.IsPublic && field.DirectPairLowId == null);
        if (sortMode == ChatGroupSortMode.DirectMessages)
            return matchingQuery.Where(field => field.DirectPairLowId != null);
        return matchingQuery;
    }

    private static IQueryable<ChatGroup> ApplySearch(IQueryable<ChatGroup> matchingQuery, HappyPlaceDbContext dbContext, Guid callerUserAccountId, string searchPattern) {
        if (searchPattern == null)
            return matchingQuery;
        return matchingQuery.Where(field => (field.DirectPairLowId == null && EF.Functions.Like(field.Name, searchPattern))
            || (field.DirectPairLowId != null && dbContext.UserAccounts.Any(user => user.Id != callerUserAccountId && (user.Id == field.DirectPairLowId.Value || user.Id == field.DirectPairHighId.Value) && (EF.Functions.Like(user.DisplayName, searchPattern) || EF.Functions.Like(user.Username, searchPattern)))));
    }

    private static HashSet<Guid> LoadMutedGroupIds(HappyPlaceDbContext dbContext, Guid callerUserAccountId) {
        return [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == callerUserAccountId && field.IsMuted)
            .Select(field => field.ChatGroupId)];
    }

    private static List<Guid> LoadHiddenGroupIds(HappyPlaceDbContext dbContext, Guid callerUserAccountId) {
        return [.. dbContext.ChatGroupMembers
            .Where(field => field.UserAccountId == callerUserAccountId && field.HiddenAtUtc != null)
            .Select(field => field.ChatGroupId)];
    }

    private static Dictionary<Guid, LastMessageEntry> LoadLastMessages(HappyPlaceDbContext dbContext, List<ChatGroup> groups) {
        List<Guid> groupIds = [.. groups.Select(field => field.Id)];
        if (groupIds.Count == 0)
            return [];
        var maxSequences = dbContext.ChatMessages
            .Where(field => groupIds.Contains(field.ChatGroupId))
            .GroupBy(field => field.ChatGroupId)
            .Select(grouping => new { ChatGroupId = grouping.Key, MaxSequence = grouping.Max(message => message.Sequence) })
            .ToList();
        if (maxSequences.Count == 0)
            return [];
        Dictionary<Guid, long> maxSequenceByGroup = maxSequences.ToDictionary(field => field.ChatGroupId, field => field.MaxSequence);
        List<long> sequenceValues = [.. maxSequenceByGroup.Values.Distinct()];
        List<ChatMessage> candidateMessages = [.. dbContext.ChatMessages
            .Where(field => groupIds.Contains(field.ChatGroupId) && sequenceValues.Contains(field.Sequence))];
        Dictionary<Guid, LastMessageEntry> lastMessagesByGroup = [];
        foreach (ChatMessage candidateMessage in candidateMessages)
            if (maxSequenceByGroup.TryGetValue(candidateMessage.ChatGroupId, out long maxSequence) && candidateMessage.Sequence == maxSequence)
                lastMessagesByGroup[candidateMessage.ChatGroupId] = new LastMessageEntry(BuildMessagePreview(candidateMessage), candidateMessage.CreatedAtUtc);
        return lastMessagesByGroup;
    }

    private static string BuildMessagePreview(ChatMessage message) {
        if (message.IsDeleted)
            return "Message deleted";
        if (message.Kind != ChatMessageKind.Text) {
            byte kindValue = (byte)message.Kind;
            if (kindValue == 2)
                return "Photo";
            if (kindValue == 3)
                return "Video";
            return "Voice message";
        }
        string body = MessageCipher.Decrypt(message.BodyCipher);
        if (body == null)
            return "";
        if (body.Length <= 120)
            return body;
        return body[..120];
    }

    private static Dictionary<Guid, ChatGroupDirectContact> LoadDirectContacts(HappyPlaceDbContext dbContext, List<ChatGroup> groups, Guid callerUserAccountId) {
        Dictionary<Guid, Guid> partnerIdsByGroup = [];
        foreach (ChatGroup group in groups) {
            if (group.DirectPairLowId == null)
                continue;
            partnerIdsByGroup[group.Id] = group.DirectPairLowId.Value == callerUserAccountId ? group.DirectPairHighId.Value : group.DirectPairLowId.Value;
        }
        if (partnerIdsByGroup.Count == 0)
            return [];
        List<Guid> partnerUserAccountIds = [.. partnerIdsByGroup.Values.Distinct()];
        Dictionary<Guid, UserAccount> partnersById = dbContext.UserAccounts
            .Where(field => partnerUserAccountIds.Contains(field.Id))
            .ToDictionary(field => field.Id);
        Dictionary<Guid, ChatGroupDirectContact> directContactsByGroup = [];
        foreach (KeyValuePair<Guid, Guid> entry in partnerIdsByGroup)
            if (partnersById.TryGetValue(entry.Value, out UserAccount partnerAccount))
                directContactsByGroup[entry.Key] = ChatGroupDirectContact.FromUserAccount(partnerAccount);
        return directContactsByGroup;
    }

    private static void TrySaveChanges(HappyPlaceDbContext dbContext) {
        try { dbContext.SaveChanges(); }
        catch (DbUpdateException) { }
    }
}
