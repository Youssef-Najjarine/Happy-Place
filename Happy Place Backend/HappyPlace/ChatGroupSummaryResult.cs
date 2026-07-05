namespace HappyWorld.HappyPlace;

public record ChatGroupSummaryResult(string Id, string Title, bool IsPublic, bool Owner, bool Joined, bool JoinRequest, bool PendingMembers, int MemberCount, List<ChatGroupHelperAvatar> Helpers);
