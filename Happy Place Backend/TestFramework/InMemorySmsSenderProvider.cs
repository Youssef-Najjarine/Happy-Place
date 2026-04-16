using HappyWorld.HappyPlace.Sms;

namespace HappyWorld.HappyPlace;

public class InMemorySmsSenderProvider : IDisposable {
    // Fields
    private InMemorySmsSender _smsSender = new();

    // Constructors
    public InMemorySmsSenderProvider() {
        SmsSender.SetInitializer(() => this._smsSender);
    }

    // Properties
    public IEnumerable<SmsMessage> SentMessages => this._smsSender.SentMessages;

    // Methods
    public void Dispose() {
        SmsSender.ResetInitializer();
    }
}