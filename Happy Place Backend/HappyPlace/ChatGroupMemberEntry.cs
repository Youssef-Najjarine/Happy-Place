using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace;

public record ChatGroupMemberEntry(string UserAccountId, string Name, string Username, string ProfilePhotoUrl, string AvatarColor, bool IsOwner) {
    // Methods

    public static List<ChatGroupMemberEntry> FromMembers(List<ChatGroupMember> members, Dictionary<Guid, UserAccount> usersById, Guid? ownerUserAccountId) {
        List<ChatGroupMemberEntry> entries = [];
        foreach (ChatGroupMember member in members) {
            if (!usersById.TryGetValue(member.UserAccountId, out UserAccount user))
                continue;
            entries.Add(new ChatGroupMemberEntry(user.Id.ToString(), user.DisplayName, user.Username, user.ProfilePhotoUrl, UserAccountRegistrar.GetAvatarColor(user.Id), user.Id == ownerUserAccountId));
        }
        return entries;
    }
}
