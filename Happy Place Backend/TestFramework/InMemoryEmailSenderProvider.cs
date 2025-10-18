using HappyWorld.HappyPlace.Email;
using System.Net.Mail;

namespace HappyWorld.HappyPlace;
public class InMemoryEmailSenderProvider : IDisposable
{
    // Fields
    private InMemoryEmailSender _emailSender = new();

    // Constructors
    public InMemoryEmailSenderProvider()
    {
        EmailSender.SetInitializer(() => this._emailSender);
    }

    // Properties
    public IEnumerable<MailMessage> EmailMessages => this._emailSender.EmailMessages;

    // Methods

    public void Dispose()
    {
        EmailSender.ResetInitializer();
    }
}
