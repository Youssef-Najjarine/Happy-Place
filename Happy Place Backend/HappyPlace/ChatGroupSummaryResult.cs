namespace HappyWorld.HappyPlace;

public record ChatGroupSummaryResult(string Id, string Title, bool IsPublic, bool Owner, bool Joined, bool JoinRequest, bool PendingMembers, int MemberCount, List<ChatGroupHelperAvatar> Helpers, int UnreadCount, bool IsDirect, ChatGroupDirectContact DirectContact, string LastMessagePreview, DateTime? LastMessageAtUtc, bool IsMuted);
