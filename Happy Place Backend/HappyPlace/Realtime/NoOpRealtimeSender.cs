namespace HappyWorld.HappyPlace.Realtime;

public class NoOpRealtimeSender : RealtimeSender {
    // Methods
    public override void SendToGroup(string groupName, string eventName, Dictionary<string, string> payload) {
    }

    public override void SendToGroups(List<string> groupNames, string eventName, Dictionary<string, string> payload) {
    }
}
