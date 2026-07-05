namespace HappyWorld.HappyPlace;

public record ChatGroupMembersResult(List<ChatGroupMemberEntry> Members, List<ChatGroupMemberEntry> PendingMembers) {
    // Methods

    public static ChatGroupMembersResult Empty() {
        return new ChatGroupMembersResult([], []);
    }
}
