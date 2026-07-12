namespace HappyWorld.HappyPlace.Web.Models.ChatMessage;

public record ChatMessageReportModel(string AuthToken, Guid ChatGroupId, Guid MessageId, string Reason) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatMessageReportResult Report() {
        return ChatMessageManager.Report(this.AuthToken, this.ChatGroupId, this.MessageId, this.Reason);
    }
}
