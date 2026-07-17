namespace HappyWorld.HappyPlace;

public record ChatGroupMembersResult(string CallerUserAccountId, List<ChatGroupMemberEntry> Members, List<ChatGroupMemberEntry> PendingMembers) {
    // Methods

    public static ChatGroupMembersResult Empty() {
        return new ChatGroupMembersResult(null, [], []);
    }
}
