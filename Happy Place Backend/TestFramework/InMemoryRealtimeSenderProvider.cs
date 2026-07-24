using HappyWorld.HappyPlace.Realtime;

namespace HappyWorld.HappyPlace;

public class InMemoryRealtimeSenderProvider : IDisposable {
    // Fields
    private readonly InMemoryRealtimeSender _realtimeSender = new();

    // Constructors
    public InMemoryRealtimeSenderProvider() {
        RealtimeSender.SetInitializer(() => this._realtimeSender);
    }

    // Properties
    public IEnumerable<RealtimeSentEvent> SentEvents => this._realtimeSender.SentEvents;

    // Methods
    public void FailNextSend() {
        this._realtimeSender.FailNextSend();
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        RealtimeSender.ResetInitializer();
    }
}
