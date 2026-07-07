using HappyWorld.HappyPlace.Data;

namespace HappyWorld.HappyPlace.Web.Models.ChatGroup;

public record ChatGroupLeaveModel(string AuthToken, Guid ChatGroupId, string Disposition = null) {
    // Methods

    public bool IsAuthenticated() {
        return UserProfileManager.IsAuthenticated(this.AuthToken);
    }

    public ChatGroupLeaveResult Leave() {
        return ChatGroupManager.Leave(this.AuthToken, this.ChatGroupId, ParseDisposition(this.Disposition));
    }

    // Helpers

    private static ChatGroupLeaveDisposition ParseDisposition(string disposition) {
        if (disposition == "delete")
            return ChatGroupLeaveDisposition.Delete;
        if (disposition == "makePublic")
            return ChatGroupLeaveDisposition.MakePublic;
        return ChatGroupLeaveDisposition.Unspecified;
    }
}
