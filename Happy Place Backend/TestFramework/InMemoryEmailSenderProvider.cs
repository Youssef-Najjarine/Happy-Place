using HappyWorld.HappyPlace.Email;


namespace HappyWorld.HappyPlace;

public class InMemoryEmailSenderProvider : IDisposable {
    // Fields
    private readonly InMemoryEmailSender _emailSender = new();

    // Constructors
    public InMemoryEmailSenderProvider() {
        EmailSender.SetInitializer(() => this._emailSender);
    }

    // Properties
    public IEnumerable<MailMessage> EmailMessages => this._emailSender.EmailMessages;

    // Methods

    public void Dispose() {
        GC.SuppressFinalize(this);
        EmailSender.ResetInitializer();
    }
}
