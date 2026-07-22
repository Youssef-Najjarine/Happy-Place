using HappyWorld.HappyPlace.PushNotifications;

namespace HappyWorld.HappyPlace;

public class InMemoryPushSenderProvider : IDisposable {
    // Fields
    private readonly InMemoryPushSender _pushSender = new();

    // Constructors
    public InMemoryPushSenderProvider() {
        PushSender.SetInitializer(() => this._pushSender);
    }

    // Properties
    public IEnumerable<PushMessage> SentMessages => this._pushSender.SentMessages;

    // Methods
    public void InvalidateToken(string token) {
        this._pushSender.InvalidateToken(token);
    }

    public void FailToken(string token) {
        this._pushSender.FailToken(token);
    }

    public void UnfailToken(string token) {
        this._pushSender.UnfailToken(token);
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        PushSender.ResetInitializer();
    }
}
