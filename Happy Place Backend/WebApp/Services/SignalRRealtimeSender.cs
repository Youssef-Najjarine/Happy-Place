using HappyWorld.HappyPlace.Realtime;
using HappyWorld.HappyPlace.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HappyWorld.HappyPlace.Web.Services;

public class SignalRRealtimeSender(IHubContext<RealtimeHub> hubContext) : RealtimeSender {
    // Fields
    private readonly IHubContext<RealtimeHub> _hubContext = hubContext;

    // Methods
    public override void SendToGroup(string groupName, string eventName, Dictionary<string, string> payload) {
        object[] eventArguments = [payload];
        this._hubContext.Clients.Group(groupName).SendCoreAsync(eventName, eventArguments).GetAwaiter().GetResult();
    }

    public override void SendToGroups(List<string> groupNames, string eventName, Dictionary<string, string> payload) {
        object[] eventArguments = [payload];
        this._hubContext.Clients.Groups(groupNames).SendCoreAsync(eventName, eventArguments).GetAwaiter().GetResult();
    }
}
