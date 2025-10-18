using HappyWorld.HappyPlace.Email;
using System.Net.Mail;

namespace HappyWorld.HappyPlace;

public class InMemoryEmailSender : EmailSender
{
    // Properties
    public IList<MailMessage> EmailMessages { get; } = [];
}
