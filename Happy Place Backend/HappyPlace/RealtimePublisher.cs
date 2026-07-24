using HappyWorld.HappyPlace.Data;
using HappyWorld.HappyPlace.Realtime;

namespace HappyWorld.HappyPlace;

public static class RealtimePublisher {
    // Fields

    public static readonly string ChatGroupChangedEventName = "chatGroupChanged";
    public static readonly string FriendsChangedEventName = "friendsChanged";
    public static readonly string HelpChangedEventName = "helpChanged";
    public static readonly string MessagesKind = "messages";
    public static readonly string MembershipKind = "membership";
    public static readonly string HelpersListeningGroupName = "helpers-listening";

    // Methods - Group Names

    public static string BuildUserGroupName(Guid userAccountId) {
        return $"user-{userAccountId}";
    }

    // Methods - Publishing

    public static void PublishChatGroupChanged(Guid chatGroupId, string kind) {
        List<Guid> extraRecipientUserAccountIds = [];
        PublishChatGroupChanged(chatGroupId, kind, extraRecipientUserAccountIds);
    }

    public static void PublishChatGroupChanged(Guid chatGroupId, string kind, List<Guid> extraRecipientUserAccountIds) {
        try {
            using var dbContext = HappyPlaceDbContext.Create();
            List<Guid> memberUserAccountIds = [.. dbContext.ChatGroupMembers
                .Where(field => field.ChatGroupId == chatGroupId && field.Status == ChatGroupMemberStatus.Active)
                .Select(field => field.UserAccountId)];
            HashSet<Guid> recipientUserAccountIds = [.. memberUserAccountIds];
            foreach (Guid extraRecipientUserAccountId in extraRecipientUserAccountIds ?? [])
                recipientUserAccountIds.Add(extraRecipientUserAccountId);
            if (recipientUserAccountIds.Count == 0)
                return;
            List<string> groupNames = [.. recipientUserAccountIds.Select(BuildUserGroupName)];
            Dictionary<string, string> payload = new() { ["chatGroupId"] = chatGroupId.ToString(), ["kind"] = kind };
            RealtimeSender.Create().SendToGroups(groupNames, ChatGroupChangedEventName, payload);
        }
        catch (Exception) {
        }
    }

    public static void PublishFriendsChanged(Guid userAccountId) {
        try {
            Dictionary<string, string> payload = [];
            RealtimeSender.Create().SendToGroup(BuildUserGroupName(userAccountId), FriendsChangedEventName, payload);
        }
        catch (Exception) {
        }
    }

    public static void PublishHelpChanged(Guid userAccountId) {
        try {
            Dictionary<string, string> payload = [];
            RealtimeSender.Create().SendToGroup(BuildUserGroupName(userAccountId), HelpChangedEventName, payload);
        }
        catch (Exception) {
        }
    }

    public static void PublishHelpChanged(List<Guid> userAccountIds) {
        try {
            List<Guid> recipientUserAccountIds = [.. (userAccountIds ?? []).Distinct()];
            if (recipientUserAccountIds.Count == 0)
                return;
            List<string> groupNames = [.. recipientUserAccountIds.Select(BuildUserGroupName)];
            Dictionary<string, string> payload = [];
            RealtimeSender.Create().SendToGroups(groupNames, HelpChangedEventName, payload);
        }
        catch (Exception) {
        }
    }

    public static void PublishHelpOpenRequestsChanged() {
        try {
            Dictionary<string, string> payload = [];
            RealtimeSender.Create().SendToGroup(HelpersListeningGroupName, HelpChangedEventName, payload);
        }
        catch (Exception) {
        }
    }
}
