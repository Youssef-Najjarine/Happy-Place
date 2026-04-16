using HappyWorld.HappyPlace.Email;

namespace HappyWorld.HappyPlace;

public class InMemoryEmailSender : EmailSender {
    // Properties
    public IList<MailMessage> EmailMessages { get; } = [];

    public override MailMessage NewMailMessage() {
        return new InMemoryMailMessage();
    }

    public override void Send(MailMessage message) {
        this.EmailMessages.Add(message);
    }
}
