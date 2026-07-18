using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record ChatGroupStateEntry(string Title, bool IsPublic, List<ChatGroupMemberEntry> Members, bool IsDirect, ChatGroupDirectContact DirectContact) {
    // Methods

    public static ChatGroupStateEntry FromGroup(ChatGroup chatGroup, List<ChatGroupMemberEntry> members, ChatGroupDirectContact directContact) {
        return new ChatGroupStateEntry(chatGroup.Name, chatGroup.IsPublic, members, chatGroup.DirectPairLowId != null, directContact);
    }
}
