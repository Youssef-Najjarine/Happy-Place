namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupOpenDirectModel(string AuthToken, string Username) {
    // Methods
    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }
    public ChatGroupOpenDirectResult OpenDirect() {
        return ChatGroupManager.OpenDirect(this.AuthToken, this.Username);
    }
}
