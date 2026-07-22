using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

public class InMemoryPushSender : PushSender {
    // Fields
    private readonly HashSet<string> _invalidTokens = [];
    private readonly HashSet<string> _failingTokens = [];

    // Properties
    public IList<PushMessage> SentMessages { get; } = [];

    // Methods
    public void InvalidateToken(string token) {
        this._invalidTokens.Add(token);
    }

    public void FailToken(string token) {
        this._failingTokens.Add(token);
    }

    public void UnfailToken(string token) {
        this._failingTokens.Remove(token);
    }

    public override void Send(PushMessage message) {
        if (this._invalidTokens.Contains(message.Token))
            throw new PushTokenInvalidException(message.Token);
        if (this._failingTokens.Contains(message.Token))
            throw new Exception("Simulated transient push failure.");
        this.SentMessages.Add(message);
    }
}
