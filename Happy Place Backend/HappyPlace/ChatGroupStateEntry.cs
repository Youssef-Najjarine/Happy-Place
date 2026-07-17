using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record ChatGroupStateEntry(string Title, bool IsPublic, List<ChatGroupMemberEntry> Members) {
    // Methods

    public static ChatGroupStateEntry FromGroup(ChatGroup chatGroup, List<ChatGroupMemberEntry> members) {
        return new ChatGroupStateEntry(chatGroup.Name, chatGroup.IsPublic, members);
    }
}
