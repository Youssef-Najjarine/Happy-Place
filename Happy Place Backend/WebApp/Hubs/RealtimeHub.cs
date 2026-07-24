using HappyWorld.HappyPlace;
using Microsoft.AspNetCore.SignalR;

namespace HappyWorld.HappyPlace.Web.Hubs;

public class RealtimeHub : Hub {
    // Fields

    private static readonly string UserAccountIdItemKey = "userAccountId";

    // Methods

    public string Authenticate(string authToken) {
        Guid? userAccountId = HelpParticipant.ResolveUserAccountId(authToken);
        if (userAccountId == null)
            return "unauthorized";
        if (Context.Items.TryGetValue(UserAccountIdItemKey, out object previousValue) && previousValue is Guid previousUserAccountId && previousUserAccountId != userAccountId.Value)
            Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimePublisher.BuildUserGroupName(previousUserAccountId)).GetAwaiter().GetResult();
        Context.Items[UserAccountIdItemKey] = userAccountId.Value;
        Groups.AddToGroupAsync(Context.ConnectionId, RealtimePublisher.BuildUserGroupName(userAccountId.Value)).GetAwaiter().GetResult();
        return "authenticated";
    }

    public string SetListening(bool isListening) {
        if (!Context.Items.ContainsKey(UserAccountIdItemKey))
            return "unauthorized";
        if (isListening)
            Groups.AddToGroupAsync(Context.ConnectionId, RealtimePublisher.HelpersListeningGroupName).GetAwaiter().GetResult();
        else
            Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimePublisher.HelpersListeningGroupName).GetAwaiter().GetResult();
        return "ok";
    }
}
