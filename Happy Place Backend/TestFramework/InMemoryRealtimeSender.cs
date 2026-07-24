using HappyWorld.HappyPlace.Realtime;

namespace HappyWorld.HappyPlace;

public class InMemoryRealtimeSender : RealtimeSender {
    // Fields
    private bool _failNextSend;

    // Properties
    public IList<RealtimeSentEvent> SentEvents { get; } = [];

    // Methods
    public void FailNextSend() {
        this._failNextSend = true;
    }

    public override void SendToGroup(string groupName, string eventName, Dictionary<string, string> payload) {
        this.ThrowIfFailing();
        this.SentEvents.Add(new RealtimeSentEvent(groupName, eventName, payload));
    }

    public override void SendToGroups(List<string> groupNames, string eventName, Dictionary<string, string> payload) {
        this.ThrowIfFailing();
        foreach (string groupName in groupNames)
            this.SentEvents.Add(new RealtimeSentEvent(groupName, eventName, payload));
    }

    private void ThrowIfFailing() {
        if (!this._failNextSend)
            return;
        this._failNextSend = false;
        throw new Exception("Simulated realtime send failure.");
    }
}
