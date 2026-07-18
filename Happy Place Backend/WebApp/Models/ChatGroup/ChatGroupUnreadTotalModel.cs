namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupUnreadTotalModel(string AuthToken) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupUnreadTotalResult UnreadTotal() {
        return ChatGroupManager.UnreadTotal(this.AuthToken);
    }
}
