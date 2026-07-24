using HappyWorld.HappyPlace.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyWorld.HappyPlace;

public static class ChatMessageManager {
    // Fields

    private static readonly int MaxBodyLength = 4096;
    private static readonly int MessageHistoryPageSize = 40;
    private static readonly int PollChangeCap = 200;
    private static readonly byte MessageHistoryCursorMarker = 4;
    private static readonly int TypingWindowSeconds = 5;
    private static readonly int MaxReportReasonLength = 500;
    private static readonly int ReplyPreviewMaxLength = 140;
    public static readonly int GuestMessageCap = 10;

    // Methods

    public static ChatMessageSendResult Send(string authToken, Guid chatGroupId, Guid clientMessageId, string body, Guid mediaId, Guid replyToMessageId) {
        Guid? senderUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (senderUserAccountId == null)
            return ChatMessageSendResult.NotMember();
        if (clientMessageId == Guid.Empty)
            return ChatMessageSendResult.InvalidBody();
        string trimmedBody = (body ?? "").Trim();
        bool isMediaSend = mediaId != Guid.Empty;
        if (isMediaSend && trimmedBody.Length > 0)
            return ChatMessageSendResult.InvalidBody();
        if (!isMediaSend && (trimmedBody.Length == 0 || trimmedBody.Length > MaxBodyLength))
            return ChatMessageSendResult.InvalidBody();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageSendResult.GroupGone();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == senderUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!callerIsActiveMember)
            return ChatMessageSendResult.NotMember();
        if (DirectSendIsBlocked(dbContext, chatGroup, senderUserAccountId.Value))
            return ChatMessageSendResult.NotFriends();
        ChatMediaAsset mediaAsset = null;
        if (isMediaSend) {
            mediaAsset = dbContext.ChatMediaAssets.SingleOrDefault(field => field.Id == mediaId);
            bool assetIsAttachable = mediaAsset != null && mediaAsset.ChatGroupId == chatGroupId && mediaAsset.UploaderUserAccountId == senderUserAccountId.Value && mediaAsset.AttachedMessageId == null;
            if (!assetIsAttachable) {
                ChatMessage retriedMessage = dbContext.ChatMessages.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.ClientMessageId == clientMessageId);
                if (retriedMessage != null)
                    return ChatMessageSendResult.Duplicate(BuildEntries(dbContext, [retriedMessage])[0], GuestMessagesRemaining(dbContext, senderUserAccountId));
                return ChatMessageSendResult.InvalidMedia();
            }
        }
        ChatMessage replyParentMessage = null;
        if (replyToMessageId != Guid.Empty) {
            replyParentMessage = dbContext.ChatMessages.SingleOrDefault(field => field.Id == replyToMessageId && field.ChatGroupId == chatGroupId);
            if (replyParentMessage == null)
                return ChatMessageSendResult.InvalidReply();
        }
        bool senderIsAnonymous = dbContext.UserAccounts.Where(field => field.Id == senderUserAccountId.Value).Select(field => field.IsAnonymous).SingleOrDefault();
        byte[] bodyCipher = isMediaSend ? null : MessageCipher.Encrypt(trimmedBody);
        ChatMessageKind messageKind = isMediaSend ? mediaAsset.Kind : ChatMessageKind.Text;
        int? guestMessagesRemaining = null;
        using var transaction = dbContext.Database.BeginTransaction();
        if (senderIsAnonymous) {
            int guestSendCounted = dbContext.Database.ExecuteSql($"UPDATE [dbo].[UserAccount] SET [GuestMessageCount] = [GuestMessageCount] + 1 WHERE [Id] = {senderUserAccountId.Value} AND [IsAnonymous] = 1 AND [GuestMessageCount] < {GuestMessageCap}");
            if (guestSendCounted == 1) {
                int guestMessageCount = dbContext.UserAccounts.Where(field => field.Id == senderUserAccountId.Value).Select(field => field.GuestMessageCount).Single();
                guestMessagesRemaining = Math.Max(0, GuestMessageCap - guestMessageCount);
            }
            else {
                bool stillAnonymous = dbContext.UserAccounts.Where(field => field.Id == senderUserAccountId.Value).Select(field => field.IsAnonymous).SingleOrDefault();
                if (stillAnonymous) {
                    transaction.Rollback();
                    return LoadGuestLimitedSend(chatGroupId, clientMessageId);
                }
            }
        }
        int sequenceClaimed = dbContext.Database.ExecuteSql($"UPDATE [dbo].[ChatGroup] SET [LastMessageSequence] = [LastMessageSequence] + 1, [LastChangeSequence] = [LastChangeSequence] + 1, [LastSeenAtUtc] = sysutcdatetime() WHERE [Id] = {chatGroupId} AND [Status] = {(byte)ChatGroupStatus.Active}");
        if (sequenceClaimed != 1) {
            transaction.Rollback();
            return ChatMessageSendResult.GroupGone();
        }
        var claimedCounters = dbContext.ChatGroups.Where(field => field.Id == chatGroupId).Select(field => new { field.LastMessageSequence, field.LastChangeSequence }).Single();
        ChatMessage message = new() { Id = Guid.NewGuid(), ChatGroupId = chatGroupId, SenderUserAccountId = senderUserAccountId.Value, ClientMessageId = clientMessageId, ReplyToChatMessageId = replyParentMessage?.Id, Kind = messageKind, BodyCipher = bodyCipher, CipherVersion = MessageCipher.CurrentVersion, Sequence = claimedCounters.LastMessageSequence, ChangeSequence = claimedCounters.LastChangeSequence, IsDeleted = false, CreatedAtUtc = DateTime.UtcNow };
        dbContext.ChatMessages.Add(message);
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
            transaction.Rollback();
            return LoadExistingSend(chatGroupId, clientMessageId);
        }
        if (isMediaSend) {
            int attached = dbContext.ChatMediaAssets
                .Where(field => field.Id == mediaId && field.AttachedMessageId == null)
                .ExecuteUpdate(setters => setters.SetProperty(field => field.AttachedMessageId, (Guid?)message.Id));
            if (attached != 1) {
                transaction.Rollback();
                return ChatMessageSendResult.InvalidMedia();
            }
        }
        transaction.Commit();
        if (chatGroup.DirectPairLowId != null)
            dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.HiddenAtUtc != null)
                .ExecuteUpdate(setters => setters.SetProperty(field => field.HiddenAtUtc, (DateTime?)null));
        NotificationDispatchManager.MarkMessagesDirty(chatGroupId, senderUserAccountId.Value);
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        return ChatMessageSendResult.Sent(BuildEntry(message, [], mediaAsset, BuildSingleReplyContext(dbContext, replyParentMessage)), guestMessagesRemaining);
    }

    public static ChatMessageListPageResult ListPage(string authToken, Guid chatGroupId, string cursor) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageListPageResult.NotMember();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageListPageResult.GroupGone();
        List<ChatGroupMember> activeMembers = LoadActiveMembers(dbContext, chatGroupId);
        if (!activeMembers.Any(field => field.UserAccountId == callerUserAccountId.Value))
            return ChatMessageListPageResult.NotMember();
        IQueryable<ChatMessage> historyQuery = dbContext.ChatMessages.Where(field => field.ChatGroupId == chatGroupId);
        bool hasCursor = CursorCodec.TryDecodeFeedCursor(cursor, MessageHistoryCursorMarker, out long beforeSequence, out _, out Guid anchorId) && anchorId == chatGroupId;
        if (hasCursor)
            historyQuery = historyQuery.Where(field => field.Sequence < beforeSequence);
        List<ChatMessage> pageMessages = [.. historyQuery
            .OrderByDescending(field => field.Sequence)
            .Take(MessageHistoryPageSize + 1)];
        bool hasOlder = pageMessages.Count > MessageHistoryPageSize;
        if (hasOlder)
            pageMessages.RemoveAt(pageMessages.Count - 1);
        string nextCursor = hasOlder ? CursorCodec.EncodeFeedCursor(MessageHistoryCursorMarker, pageMessages[^1].Sequence, 0, chatGroupId) : null;
        return ChatMessageListPageResult.Ok(callerUserAccountId.Value.ToString(), BuildEntries(dbContext, pageMessages), BuildSenderEntries(dbContext, pageMessages), BuildReadPointerEntries(activeMembers), BuildTypingUserIds(activeMembers, callerUserAccountId.Value), nextCursor, chatGroup.LastChangeSequence, GuestMessagesRemaining(dbContext, callerUserAccountId.Value));
    }

    public static ChatMessagePollResult Poll(string authToken, Guid chatGroupId, long sinceChangeSequence) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessagePollResult.NotMember();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessagePollResult.GroupGone();
        List<ChatGroupMember> activeMembers = LoadActiveMembers(dbContext, chatGroupId);
        if (!activeMembers.Any(field => field.UserAccountId == callerUserAccountId.Value))
            return ChatMessagePollResult.NotMember();
        List<ChatMessage> changedMessages = [.. dbContext.ChatMessages
            .Where(field => field.ChatGroupId == chatGroupId && field.ChangeSequence > sinceChangeSequence)
            .OrderBy(field => field.ChangeSequence)
            .Take(PollChangeCap)];
        long changeSequence = changedMessages.Count == 0 ? sinceChangeSequence : changedMessages[^1].ChangeSequence;
        return ChatMessagePollResult.Ok(BuildGroupState(dbContext, chatGroup, activeMembers, callerUserAccountId.Value), BuildEntries(dbContext, changedMessages), BuildSenderEntries(dbContext, changedMessages), BuildReadPointerEntries(activeMembers), BuildTypingUserIds(activeMembers, callerUserAccountId.Value), changeSequence);
    }

    public static ChatMessageMarkReadResult MarkRead(string authToken, Guid chatGroupId, long upToSequence) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageMarkReadResult.NotMember();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageMarkReadResult.GroupGone();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!callerIsActiveMember)
            return ChatMessageMarkReadResult.NotMember();
        long effectiveSequence = Math.Min(upToSequence, chatGroup.LastMessageSequence);
        if (effectiveSequence > 0) {
            int advanced = dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active && field.LastReadSequence < effectiveSequence)
                .ExecuteUpdate(setters => setters.SetProperty(field => field.LastReadSequence, effectiveSequence));
            if (advanced > 0) {
                NotificationDispatchManager.MarkMessagesReadDirty(chatGroupId, callerUserAccountId.Value);
                RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
            }
        }
        long lastReadSequence = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value)
            .Select(field => field.LastReadSequence)
            .SingleOrDefault();
        return ChatMessageMarkReadResult.Ok(lastReadSequence);
    }

    public static ChatMessageTypingResult Typing(string authToken, Guid chatGroupId) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageTypingResult.NotMember();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageTypingResult.GroupGone();
        if (DirectSendIsBlocked(dbContext, chatGroup, callerUserAccountId.Value))
            return ChatMessageTypingResult.NotFriends();
        int stamped = dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.LastTypingAtUtc, (DateTime?)DateTime.UtcNow));
        if (stamped != 1)
            return ChatMessageTypingResult.NotMember();
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        return ChatMessageTypingResult.Ok();
    }

    public static ChatMessageReactResult React(string authToken, Guid chatGroupId, Guid messageId, string emoji) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageReactResult.NotMember();
        string trimmedEmoji = emoji == null ? "" : emoji.Trim();
        if (trimmedEmoji.Length > 20 || !IsValidReactionEmoji(trimmedEmoji))
            return ChatMessageReactResult.InvalidEmoji();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageReactResult.GroupGone();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!callerIsActiveMember)
            return ChatMessageReactResult.NotMember();
        if (DirectSendIsBlocked(dbContext, chatGroup, callerUserAccountId.Value))
            return ChatMessageReactResult.NotFriends();
        ChatMessage message = dbContext.ChatMessages.SingleOrDefault(field => field.Id == messageId && field.ChatGroupId == chatGroupId);
        if (message == null || message.IsDeleted)
            return ChatMessageReactResult.MessageGone();
        using var transaction = dbContext.Database.BeginTransaction();
        int changeClaimed = dbContext.Database.ExecuteSql($"UPDATE [dbo].[ChatGroup] SET [LastChangeSequence] = [LastChangeSequence] + 1, [LastSeenAtUtc] = sysutcdatetime() WHERE [Id] = {chatGroupId} AND [Status] = {(byte)ChatGroupStatus.Active}");
        if (changeClaimed != 1) {
            transaction.Rollback();
            return ChatMessageReactResult.GroupGone();
        }
        long changeSequence = dbContext.ChatGroups.Where(field => field.Id == chatGroupId).Select(field => field.LastChangeSequence).Single();
        if (trimmedEmoji.Length == 0) {
            dbContext.ChatMessageReactions
                .Where(field => field.ChatMessageId == messageId && field.UserAccountId == callerUserAccountId.Value)
                .ExecuteDelete();
            StampMessageChange(dbContext, messageId, changeSequence);
            transaction.Commit();
            RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
            return ChatMessageReactResult.Removed();
        }
        ChatMessageReaction existingReaction = dbContext.ChatMessageReactions.SingleOrDefault(field => field.ChatMessageId == messageId && field.UserAccountId == callerUserAccountId.Value);
        if (existingReaction == null) {
            dbContext.ChatMessageReactions.Add(new ChatMessageReaction { Id = Guid.NewGuid(), ChatMessageId = messageId, UserAccountId = callerUserAccountId.Value, Emoji = trimmedEmoji, CreatedAtUtc = DateTime.UtcNow });
            try {
                dbContext.SaveChanges();
            }
            catch (DbUpdateException) {
                transaction.Rollback();
                return ChatMessageReactResult.Reacted();
            }
        }
        else {
            existingReaction.Emoji = trimmedEmoji;
            existingReaction.CreatedAtUtc = DateTime.UtcNow;
            dbContext.SaveChanges();
        }
        StampMessageChange(dbContext, messageId, changeSequence);
        transaction.Commit();
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        return ChatMessageReactResult.Reacted();
    }

    public static ChatMessageDeleteOwnResult DeleteOwn(string authToken, Guid chatGroupId, Guid messageId) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageDeleteOwnResult.NotMember();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageDeleteOwnResult.GroupGone();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!callerIsActiveMember)
            return ChatMessageDeleteOwnResult.NotMember();
        ChatMessage message = dbContext.ChatMessages.SingleOrDefault(field => field.Id == messageId && field.ChatGroupId == chatGroupId);
        if (message == null)
            return ChatMessageDeleteOwnResult.MessageGone();
        if (message.SenderUserAccountId != callerUserAccountId.Value)
            return ChatMessageDeleteOwnResult.NotYours();
        if (message.IsDeleted)
            return ChatMessageDeleteOwnResult.Deleted();
        using var transaction = dbContext.Database.BeginTransaction();
        int changeClaimed = dbContext.Database.ExecuteSql($"UPDATE [dbo].[ChatGroup] SET [LastChangeSequence] = [LastChangeSequence] + 1, [LastSeenAtUtc] = sysutcdatetime() WHERE [Id] = {chatGroupId} AND [Status] = {(byte)ChatGroupStatus.Active}");
        if (changeClaimed != 1) {
            transaction.Rollback();
            return ChatMessageDeleteOwnResult.GroupGone();
        }
        long changeSequence = dbContext.ChatGroups.Where(field => field.Id == chatGroupId).Select(field => field.LastChangeSequence).Single();
        int flipped = dbContext.ChatMessages
            .Where(field => field.Id == messageId && !field.IsDeleted)
            .ExecuteUpdate(setters => setters
                .SetProperty(field => field.IsDeleted, true)
                .SetProperty(field => field.ChangeSequence, changeSequence));
        if (flipped != 1) {
            transaction.Rollback();
            return ChatMessageDeleteOwnResult.Deleted();
        }
        dbContext.ChatMessageReactions.Where(field => field.ChatMessageId == messageId).ExecuteDelete();
        transaction.Commit();
        RealtimePublisher.PublishChatGroupChanged(chatGroupId, RealtimePublisher.MessagesKind);
        return ChatMessageDeleteOwnResult.Deleted();
    }

    public static ChatMessageReportResult Report(string authToken, Guid chatGroupId, Guid messageId, string reason) {
        Guid? callerUserAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (callerUserAccountId == null)
            return ChatMessageReportResult.NotMember();
        string trimmedReason = (reason ?? "").Trim();
        if (trimmedReason.Length > MaxReportReasonLength)
            return ChatMessageReportResult.InvalidReason();
        using var dbContext = HappyPlaceDbContext.Create();
        ChatGroup chatGroup = dbContext.ChatGroups.SingleOrDefault(field => field.Id == chatGroupId);
        if (chatGroup == null || chatGroup.Status != ChatGroupStatus.Active)
            return ChatMessageReportResult.GroupGone();
        bool callerIsActiveMember = dbContext.ChatGroupMembers.Any(field => field.ChatGroupId == chatGroupId && field.UserAccountId == callerUserAccountId.Value && field.Status == ChatGroupMemberStatus.Active);
        if (!callerIsActiveMember)
            return ChatMessageReportResult.NotMember();
        ChatMessage message = dbContext.ChatMessages.SingleOrDefault(field => field.Id == messageId && field.ChatGroupId == chatGroupId);
        if (message == null || message.IsDeleted)
            return ChatMessageReportResult.MessageGone();
        if (message.SenderUserAccountId == callerUserAccountId.Value)
            return ChatMessageReportResult.CannotReportOwn();
        byte[] reasonCipher = trimmedReason.Length == 0 ? null : MessageCipher.Encrypt(trimmedReason);
        dbContext.ChatMessageReports.Add(new ChatMessageReport { Id = Guid.NewGuid(), ChatMessageId = messageId, ReporterUserAccountId = callerUserAccountId.Value, ReportedUserAccountId = message.SenderUserAccountId, Kind = message.Kind, BodySnapshotCipher = message.BodyCipher, ReasonCipher = reasonCipher, Status = ChatMessageReportStatus.Open, CreatedAtUtc = DateTime.UtcNow });
        try {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException) {
            return ChatMessageReportResult.AlreadyReported();
        }
        return ChatMessageReportResult.Reported();
    }

    // Helpers

    private static void StampMessageChange(HappyPlaceDbContext dbContext, Guid messageId, long changeSequence) {
        dbContext.ChatMessages
            .Where(field => field.Id == messageId)
            .ExecuteUpdate(setters => setters.SetProperty(field => field.ChangeSequence, changeSequence));
    }

    private static List<ChatGroupMember> LoadActiveMembers(HappyPlaceDbContext dbContext, Guid chatGroupId) {
        return [.. dbContext.ChatGroupMembers
            .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active)];
    }

    private static bool DirectSendIsBlocked(HappyPlaceDbContext dbContext, ChatGroup chatGroup, Guid callerUserAccountId) {
        if (chatGroup.DirectPairLowId == null)
            return false;
        bool callerIsInPair = chatGroup.DirectPairLowId.Value == callerUserAccountId || chatGroup.DirectPairHighId.Value == callerUserAccountId;
        if (!callerIsInPair)
            return false;
        Guid partnerUserAccountId = chatGroup.DirectPairLowId.Value == callerUserAccountId ? chatGroup.DirectPairHighId.Value : chatGroup.DirectPairLowId.Value;
        return !FriendshipManager.CanDirectMessage(dbContext, callerUserAccountId, partnerUserAccountId);
    }

    private static ChatGroupStateEntry BuildGroupState(HappyPlaceDbContext dbContext, ChatGroup chatGroup, List<ChatGroupMember> activeMembers, Guid callerUserAccountId) {
        List<ChatGroupMember> orderedMembers = [.. activeMembers.OrderBy(field => field.JoinedAtUtc).ThenBy(field => field.Id)];
        List<Guid> memberUserAccountIds = [.. orderedMembers.Select(field => field.UserAccountId)];
        Dictionary<Guid, UserAccount> usersById = dbContext.UserAccounts.Where(field => memberUserAccountIds.Contains(field.Id)).ToDictionary(field => field.Id);
        ChatGroupDirectContact directContact = null;
        if (chatGroup.DirectPairLowId != null) {
            Guid partnerUserAccountId = chatGroup.DirectPairLowId.Value == callerUserAccountId ? chatGroup.DirectPairHighId.Value : chatGroup.DirectPairLowId.Value;
            if (usersById.TryGetValue(partnerUserAccountId, out UserAccount partnerAccount))
                directContact = ChatGroupDirectContact.FromUserAccount(partnerAccount);
        }
        return ChatGroupStateEntry.FromGroup(chatGroup, ChatGroupMemberEntry.FromMembers(orderedMembers, usersById, chatGroup.OwnerUserAccountId), directContact);
    }

    private static List<ChatMessageReadPointerEntry> BuildReadPointerEntries(List<ChatGroupMember> activeMembers) {
        return [.. activeMembers
            .OrderBy(field => field.UserAccountId)
            .Select(field => new ChatMessageReadPointerEntry(field.UserAccountId.ToString(), field.LastReadSequence))];
    }

    private static List<string> BuildTypingUserIds(List<ChatGroupMember> activeMembers, Guid callerUserAccountId) {
        DateTime typingCutoffUtc = DateTime.UtcNow.AddSeconds(-TypingWindowSeconds);
        return [.. activeMembers
            .Where(field => field.UserAccountId != callerUserAccountId && field.LastTypingAtUtc != null && field.LastTypingAtUtc > typingCutoffUtc)
            .OrderBy(field => field.UserAccountId)
            .Select(field => field.UserAccountId.ToString())];
    }

    private static ChatMessageSendResult LoadExistingSend(Guid chatGroupId, Guid clientMessageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatMessage existingMessage = dbContext.ChatMessages.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.ClientMessageId == clientMessageId);
        if (existingMessage == null)
            return ChatMessageSendResult.GroupGone();
        return ChatMessageSendResult.Duplicate(BuildEntries(dbContext, [existingMessage])[0], GuestMessagesRemaining(dbContext, existingMessage.SenderUserAccountId));
    }

    private static ChatMessageSendResult LoadGuestLimitedSend(Guid chatGroupId, Guid clientMessageId) {
        using var dbContext = HappyPlaceDbContext.Create();
        ChatMessage existingMessage = dbContext.ChatMessages.SingleOrDefault(field => field.ChatGroupId == chatGroupId && field.ClientMessageId == clientMessageId);
        if (existingMessage == null)
            return ChatMessageSendResult.GuestLimitReached();
        return ChatMessageSendResult.Duplicate(BuildEntries(dbContext, [existingMessage])[0], GuestMessagesRemaining(dbContext, existingMessage.SenderUserAccountId));
    }

    private static int? GuestMessagesRemaining(HappyPlaceDbContext dbContext, Guid? userAccountId) {
        if (userAccountId == null)
            return null;
        var guestCounters = dbContext.UserAccounts.Where(field => field.Id == userAccountId.Value).Select(field => new { field.IsAnonymous, field.GuestMessageCount }).SingleOrDefault();
        if (guestCounters == null || !guestCounters.IsAnonymous)
            return null;
        return Math.Max(0, GuestMessageCap - guestCounters.GuestMessageCount);
    }

    private static bool IsValidReactionEmoji(string emoji) {
        foreach (char character in emoji) {
            if (character < '\u00A0' && !"0123456789#*".Contains(character))
                return false;
        }
        return true;
    }

    private static List<ChatMessageEntry> BuildEntries(HappyPlaceDbContext dbContext, List<ChatMessage> messages) {
        if (messages.Count == 0)
            return [];
        List<Guid> messageIds = [.. messages.Select(field => field.Id)];
        List<ChatMessageReaction> reactions = [.. dbContext.ChatMessageReactions.Where(field => messageIds.Contains(field.ChatMessageId))];
        Dictionary<Guid, List<ChatMessageReactionEntry>> reactionsByMessageId = reactions
            .GroupBy(field => field.ChatMessageId)
            .ToDictionary(reactionGroup => reactionGroup.Key, reactionGroup => reactionGroup.OrderBy(field => field.UserAccountId).Select(field => new ChatMessageReactionEntry(field.UserAccountId.ToString(), field.Emoji)).ToList());
        Dictionary<Guid, ChatMediaAsset> assetsByMessageId = dbContext.ChatMediaAssets
            .Where(field => field.AttachedMessageId != null && messageIds.Contains(field.AttachedMessageId.Value))
            .ToDictionary(field => field.AttachedMessageId.Value);
        Dictionary<Guid, ChatMessage> replyParentsById = LoadReplyParents(dbContext, messages);
        Dictionary<Guid, string> replyParentSenderNamesById = LoadSenderDisplayNames(dbContext, [.. replyParentsById.Values]);
        return [.. messages.Select(field => BuildEntry(field, reactionsByMessageId.TryGetValue(field.Id, out List<ChatMessageReactionEntry> messageReactions) ? messageReactions : [], assetsByMessageId.TryGetValue(field.Id, out ChatMediaAsset messageAsset) ? messageAsset : null, BuildReplyContext(field.ReplyToChatMessageId, replyParentsById, replyParentSenderNamesById)))];
    }

    private static ChatMessageEntry BuildEntry(ChatMessage message, List<ChatMessageReactionEntry> reactions, ChatMediaAsset mediaAsset, ChatMessageReplyContextEntry replyTo) {
        string senderUserAccountId = message.SenderUserAccountId?.ToString();
        string body = message.IsDeleted ? null : MessageCipher.Decrypt(message.BodyCipher);
        string mediaUrl = null;
        int? mediaWidth = null;
        int? mediaHeight = null;
        int? mediaDurationSeconds = null;
        if (mediaAsset != null && !message.IsDeleted) {
            mediaUrl = ChatMediaManager.BuildMediaUrl(mediaAsset.Id);
            mediaWidth = mediaAsset.Width;
            mediaHeight = mediaAsset.Height;
            mediaDurationSeconds = mediaAsset.DurationSeconds;
        }
        return new ChatMessageEntry(message.Id.ToString(), message.ClientMessageId.ToString(), message.Sequence, senderUserAccountId, (byte)message.Kind, body, message.IsDeleted, reactions, mediaUrl, mediaWidth, mediaHeight, mediaDurationSeconds, replyTo, message.CreatedAtUtc);
    }

    private static ChatMessageReplyContextEntry BuildSingleReplyContext(HappyPlaceDbContext dbContext, ChatMessage replyParentMessage) {
        if (replyParentMessage == null)
            return null;
        Dictionary<Guid, ChatMessage> replyParentsById = new() { [replyParentMessage.Id] = replyParentMessage };
        return BuildReplyContext(replyParentMessage.Id, replyParentsById, LoadSenderDisplayNames(dbContext, [replyParentMessage]));
    }

    private static ChatMessageReplyContextEntry BuildReplyContext(Guid? replyToChatMessageId, Dictionary<Guid, ChatMessage> replyParentsById, Dictionary<Guid, string> replyParentSenderNamesById) {
        if (replyToChatMessageId == null)
            return null;
        if (!replyParentsById.TryGetValue(replyToChatMessageId.Value, out ChatMessage replyParentMessage))
            return new ChatMessageReplyContextEntry(replyToChatMessageId.Value.ToString(), null, null, (byte)ChatMessageKind.Text, null, true);
        string senderUserAccountId = replyParentMessage.SenderUserAccountId?.ToString();
        string senderDisplayName = replyParentMessage.SenderUserAccountId != null && replyParentSenderNamesById.TryGetValue(replyParentMessage.SenderUserAccountId.Value, out string parentSenderName) ? parentSenderName : null;
        string preview = null;
        if (!replyParentMessage.IsDeleted && replyParentMessage.Kind == ChatMessageKind.Text) {
            string parentBody = MessageCipher.Decrypt(replyParentMessage.BodyCipher);
            preview = parentBody != null && parentBody.Length > ReplyPreviewMaxLength ? parentBody[..ReplyPreviewMaxLength] : parentBody;
        }
        return new ChatMessageReplyContextEntry(replyParentMessage.Id.ToString(), senderUserAccountId, senderDisplayName, (byte)replyParentMessage.Kind, preview, replyParentMessage.IsDeleted);
    }

    private static Dictionary<Guid, ChatMessage> LoadReplyParents(HappyPlaceDbContext dbContext, List<ChatMessage> messages) {
        List<Guid> replyParentIds = [.. messages.Where(field => field.ReplyToChatMessageId != null).Select(field => field.ReplyToChatMessageId.Value).Distinct()];
        Dictionary<Guid, ChatMessage> replyParentsById = [];
        if (replyParentIds.Count == 0)
            return replyParentsById;
        return dbContext.ChatMessages.Where(field => replyParentIds.Contains(field.Id)).ToDictionary(field => field.Id);
    }

    private static Dictionary<Guid, string> LoadSenderDisplayNames(HappyPlaceDbContext dbContext, List<ChatMessage> messages) {
        List<Guid> parentSenderIds = [.. messages.Where(field => field.SenderUserAccountId != null).Select(field => field.SenderUserAccountId.Value).Distinct()];
        Dictionary<Guid, string> senderNamesById = [];
        if (parentSenderIds.Count == 0)
            return senderNamesById;
        return dbContext.UserAccounts.Where(field => parentSenderIds.Contains(field.Id)).ToDictionary(field => field.Id, field => field.DisplayName);
    }

    private static List<ChatMessageSenderEntry> BuildSenderEntries(HappyPlaceDbContext dbContext, List<ChatMessage> messages) {
        List<Guid> senderIds = [.. messages
            .Where(field => field.SenderUserAccountId != null)
            .Select(field => field.SenderUserAccountId.Value)
            .Distinct()];
        if (senderIds.Count == 0)
            return [];
        List<UserAccount> senders = [.. dbContext.UserAccounts
            .Where(field => senderIds.Contains(field.Id))
            .OrderBy(field => field.Id)];
        return [.. senders.Select(field => new ChatMessageSenderEntry(field.Id.ToString(), field.DisplayName, field.ProfilePhotoUrl))];
    }
}
