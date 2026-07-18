namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupCreateWithFriendsModel(string AuthToken, string Name, List<string> Usernames) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupCreateWithFriendsResult CreateWithFriends() {
        return ChatGroupManager.CreateWithFriends(this.AuthToken, this.Name, this.Usernames);
    }
}
